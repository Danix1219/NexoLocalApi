using System.Security.Cryptography;
using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Requests;

public sealed class RequestService(IRequestStore store, IProviderStore profiles, IProviderAccounts accounts, IProviderSafety safety,
    IDirectoryProfiles directory, IRequestClients clients, IRequestInterests interests, TimeProvider clock)
{
    public async Task<Result<ServiceRequest>> PublishAsync(Guid owner, string? key, JsonElement json, CancellationToken ct)
    {
        if (!Guid.TryParse(key, out var parsedKey) || parsedKey == Guid.Empty) return Fail<ServiceRequest>(NexoCodes.RequestIdempotencyKeyMissing);
        PublishRequest? body;
        try { body = json.Deserialize<PublishRequest>(ProviderService.Json); }
        catch (JsonException) { return Fail<ServiceRequest>(NexoCodes.ValidationFailed); }
        if (RequestRules.Validate(body) is { } error) return Fail<ServiceRequest>(error);
        var account = await accounts.ReadAsync(owner, ct);
        if (account?.Active != true) return Fail<ServiceRequest>(NexoCodes.AccountPendingDeletion);
        if (body!.Contact == "chatCall" && !account.EmailVerified) return Fail<ServiceRequest>(NexoCodes.AuthEmailNotVerified);
        if (body.Contact == "chatCall" && !account.HasPhone) return Fail<ServiceRequest>(NexoCodes.PhoneInvalid);
        var input = body with { Title = body.Title.Trim(), Description = body.Description.Trim() };
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(input, ProviderService.Json)));
        if (await store.ReceiptAsync(owner, parsedKey, ct) is { } receipt)
        {
            if (receipt.Hash != hash) return Fail<ServiceRequest>(NexoCodes.RequestIdempotencyKeyReused);
            var previous = await store.OwnAsync(owner, receipt.RequestId, ct);
            return previous == null ? Fail<ServiceRequest>(NexoCodes.RequestNotFound) : Result<ServiceRequest>.Success(previous);
        }
        var moderation = await safety.ModerateAsync(input.Title + "\n" + input.Description, ct);
        if (moderation.IsFailure) return Result<ServiceRequest>.Failure(moderation.Error!);
        if (!moderation.Value) return Fail<ServiceRequest>(NexoCodes.ContentInappropriate);
        var imageUrls = new List<string>();
        foreach (var image in input.ImageUploadIds ?? (input.ImageUploadId is { } legacy ? [legacy] : []))
        {
            var approved = await safety.ImageAsync(owner, image, "request", ct);
            if (approved.IsFailure) return Result<ServiceRequest>.Failure(approved.Error!);
            if (!Uri.TryCreate(approved.Value!.Url, UriKind.Absolute, out var media) || !media.AbsolutePath.StartsWith("/v1/discovery/media/", StringComparison.Ordinal)) return Fail<ServiceRequest>(NexoCodes.ServerDependencyUnavailable);
            imageUrls.Add(approved.Value!.Url);
        }
        var reachable = (await directory.CandidatesAsync(input.Location, input.Category, ct)).Where(p => p.Published && p.AccountId != owner && DirectoryService.Serves(p)).ToArray();
        var states = await accounts.ReadManyAsync(reachable.Select(p => p.AccountId).Distinct().ToArray(), ct);
        var count = reachable.Count(p => states.TryGetValue(p.AccountId, out var state) && state.Active);
        var now = clock.GetUtcNow().UtcDateTime;
        var request = new ServiceRequest(Guid.CreateVersion7(), input.Category, input.Title, input.Description, input.Urgency, input.Contact,
            input.Location.Blur(), "Zona aproximada", imageUrls.FirstOrDefault(), "open", 0, count, now, now, ImageUrls: imageUrls.ToArray());
        return await store.CreateAsync(owner, parsedKey, hash, request, input.Location, ct);
    }
    public async Task<Result<ServiceRequest>> EditAsync(Guid owner, Guid id, JsonElement json, CancellationToken ct)
    {
        EditRequest? body;
        try { body = json.Deserialize<EditRequest>(ProviderService.Json); }
        catch (JsonException) { return Fail<ServiceRequest>(NexoCodes.ValidationFailed); }
        if (RequestRules.ValidateEdit(body) is { } error) return Fail<ServiceRequest>(error);
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return Fail<ServiceRequest>(NexoCodes.AccountPendingDeletion);
        var current = await store.OwnAsync(owner, id, ct);
        if (current == null) return Fail<ServiceRequest>(NexoCodes.RequestNotFound);
        if (current.Status != "open") return Fail<ServiceRequest>(NexoCodes.RequestAlreadyClosed);
        if (body!.Contact == "chatCall")
        {
            var account = await accounts.ReadAsync(owner, ct);
            if (account?.EmailVerified != true) return Fail<ServiceRequest>(NexoCodes.AuthEmailNotVerified);
            if (!account.HasPhone) return Fail<ServiceRequest>(NexoCodes.PhoneInvalid);
        }
        var title = body.Title.Trim(); var description = body.Description.Trim();
        var moderation = await safety.ModerateAsync(title + "\n" + description, ct);
        if (moderation.IsFailure) return Result<ServiceRequest>.Failure(moderation.Error!);
        if (!moderation.Value) return Fail<ServiceRequest>(NexoCodes.ContentInappropriate);
        var urls = new List<string>();
        foreach (var upload in body.ImageUploadIds ?? [])
        {
            var approved = await safety.ImageAsync(owner, upload, "request", ct);
            if (approved.IsFailure) return Result<ServiceRequest>.Failure(approved.Error!);
            if (!Uri.TryCreate(approved.Value!.Url, UriKind.Absolute, out var media) || !media.AbsolutePath.StartsWith("/v1/discovery/media/", StringComparison.Ordinal)) return Fail<ServiceRequest>(NexoCodes.ServerDependencyUnavailable);
            urls.Add(approved.Value!.Url);
        }
        var updated = current with
        {
            Title = title,
            Description = description,
            Urgency = body.Urgency,
            Contact = body.Contact,
            ImageUrl = urls.FirstOrDefault(),
            ImageUrls = urls.ToArray(),
            CheckedAt = clock.GetUtcNow().UtcDateTime
        };
        return await store.UpdateAsync(owner, id, updated, ct);
    }
    public async Task<Result<object>> MineAsync(Guid owner, string status, string? cursor, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 50) return Fail<object>(NexoCodes.RequestLimitExceeded);
        if (status is not ("open" or "assigned" or "closed" or "all")) return Fail<object>(NexoCodes.ValidationFailed);
        var hash = RequestRules.Hash(new { kind = "mine", owner, status });
        if (!RequestRules.TryCursor(cursor, hash, out var after)) return Fail<object>(NexoCodes.RequestCursorInvalid);
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return Fail<object>(NexoCodes.AccountPendingDeletion);
        var values = await store.MineAsync(owner, status, after, limit + 1, ct);
        var selected = values.Take(limit).ToArray();
        var summary = await interests.ReadAsync(selected.Select(r => r.RequestId).ToArray(), null, ct);
        var ids = summary.Items.Values.SelectMany(s => s.Providers).Concat(selected.Where(r => r.AssignedProviderId != null).Select(r => r.AssignedProviderId!.Value)).Distinct().ToArray();
        var candidates = ids.Length == 0 ? [] : (await profiles.PublicAsync(ids, null, ct)).ToArray();
        var states = await accounts.ReadManyAsync(candidates.Select(p => p.AccountId).Distinct().ToArray(), ct);
        var visible = candidates.Where(p => p.Published && states.TryGetValue(p.AccountId, out var state) && state.Active).ToDictionary(p => p.Profile.ProviderId);
        var items = selected.Select(r =>
        {
            summary.Items.TryGetValue(r.RequestId, out var s);
            var count = summary.Available && s != null ? s.Count : r.InterestCount;
            var preview = (s?.Providers ?? []).Where(visible.ContainsKey).Take(4).Select(id => new { providerId = id, name = visible[id].Profile.Name, photoUrl = visible[id].Profile.PhotoUrl }).ToArray();
            var picked = r.AssignedProviderId is { } assigned && visible.TryGetValue(assigned, out var chosen) ? new { providerId = assigned, name = chosen.Profile.Name, photoUrl = chosen.Profile.PhotoUrl } : null;
            return new
            {
                r.RequestId,
                r.Category,
                r.Title,
                description = r.Description,
                descriptionExcerpt = r.Description[..Math.Min(200, r.Description.Length)],
                r.Urgency,
                r.UrgencyText,
                r.ZoneLabel,
                r.ImageUrl,
                imageUrls = r.Photos,
                r.Contact,
                r.Status,
                interestCount = count,
                interestedPreview = preview,
                interestedMoreCount = Math.Max(0, count - preview.Length),
                summaryText = r.Status == "closed" ? "Solicitud cerrada" : picked != null ? "Elegiste a " + picked.name : r.Status == "assigned" ? "Ya elegiste un prestador" : count == 0 ? "Aún sin interesados" : $"{count} interesados · Compara y elige a quién pedirle",
                pickedProvider = picked,
                ago = Ago(r.CreatedAt),
                r.CreatedAt
            };
        }).ToArray();
        return Result<object>.Success(new
        {
            items,
            nextCursor = values.Length > limit ? RequestRules.Cursor(selected[^1].RequestId, hash) : null,
            hasMore = values.Length > limit,
            interestSummaryAvailable = summary.Available
        });
    }
    public async Task<Result<object>> OpportunitiesAsync(Guid owner, OpportunityQuery query, CancellationToken ct)
    {
        if (query.Error is { } error) return Fail<object>(error);
        var profile = await ProviderAsync(owner, ct);
        if (profile == null) return Fail<object>(NexoCodes.ProviderProfileRequired);
        if (query.Cat != null && query.MyServiceOnly && !profile.Profile.Categories.Contains(query.Cat)) return Fail<object>(NexoCodes.ValidationFailed);
        var p = profile.Profile;
        var hash = RequestRules.Hash(new
        {
            kind = "opportunities",
            owner,
            p.ProviderId,
            profile.Revision,
            p.Location,
            p.RadiusKm,
            cats = p.Categories.Order().ToArray(),
            query.Cat,
            query.Today,
            query.InRadius,
            query.MyServiceOnly,
            q = DirectoryCatalog.Normalize(query.Q)
        });
        if (!RequestRules.TryCursor(query.Cursor, hash, out var after, out var rank)) return Fail<object>(NexoCodes.RequestCursorInvalid);
        var cats = query.Cat != null ? new[] { query.Cat } : query.MyServiceOnly ? p.Categories : null;
        var radius = p.RadiusKm == 0 ? 10 : p.RadiusKm;
        var values = await ActiveCandidatesAsync(owner, p.Location!, query.InRadius ? radius : null, cats, query.Q, query.Today, ct);
        var ordered = values.OrderBy(v => RequestRules.Rank(v.Request.Urgency)).ThenByDescending(v => v.Request.RequestId.ToString("D"), StringComparer.Ordinal).ToArray();
        var rest = ordered.Where(v => after == null || RequestRules.Rank(v.Request.Urgency) > rank ||
            RequestRules.Rank(v.Request.Urgency) == rank && string.CompareOrdinal(v.Request.RequestId.ToString("D"), after.Value.ToString("D")) < 0).Take(query.Limit + 1).ToArray();
        var selected = rest.Take(query.Limit).ToArray();
        var labels = await clients.ReadAsync(selected.Select(v => v.ClientId).Distinct().ToArray(), ct);
        var summary = await interests.ReadAsync(selected.Select(v => v.Request.RequestId).ToArray(), p.ProviderId, ct);
        var items = selected.Select(v =>
        {
            var r = v.Request; summary.Items.TryGetValue(r.RequestId, out var s);
            return new
            {
                r.RequestId,
                r.Category,
                r.Title,
                description = r.Description,
                descriptionExcerpt = r.Description[..Math.Min(200, r.Description.Length)],
                r.Urgency,
                r.UrgencyText,
                r.ZoneLabel,
                r.PublicPoint,
                distanceText = RequestRules.DistanceText(v.DistanceKm),
                r.ImageUrl,
                imageUrls = r.Photos,
                interestCount = summary.Available && s != null ? s.Count : r.InterestCount,
                ago = Ago(r.CreatedAt),
                iAmInterested = summary.Available && s != null ? (bool?)s.Interested : null,
                inMyRadius = v.DistanceKm <= radius,
                matchesMyService = p.Categories.Contains(r.Category),
                client = labels.TryGetValue(v.ClientId, out var label) ? new { displayName = label.DisplayName, photoUrl = label.PhotoUrl } : new { displayName = "Persona", photoUrl = (string?)null }
            };
        }).ToArray();
        var filters = new[] { query.InRadius ? "inRadius" : null, query.MyServiceOnly ? "myServiceOnly" : null, query.Today ? "today" : null,
            query.Cat != null ? "cat:" + query.Cat : null, query.Q.Length > 0 ? "q" : null }.OfType<string>().ToArray();
        return Result<object>.Success(new
        {
            items,
            nextCursor = rest.Length > query.Limit ? RequestRules.Cursor(selected[^1].Request.RequestId, hash, RequestRules.Rank(selected[^1].Request.Urgency)) : null,
            hasMore = rest.Length > query.Limit,
            total = values.Count,
            appliedFilters = filters,
            interestSummaryAvailable = summary.Available
        });
    }
    public async Task<Result<object>> MapAsync(Guid owner, ProviderPoint center, int radius, string? cat, int limit, CancellationToken ct)
    {
        var provider = await ProviderAsync(owner, ct);
        if (provider == null) return Fail<object>(NexoCodes.ProviderProfileRequired);
        if (cat != null && !provider.Profile.Categories.Contains(cat)) return Fail<object>(NexoCodes.ValidationFailed);
        var effective = Math.Min(radius, provider.Profile.RadiusKm == 0 ? 10 : provider.Profile.RadiusKm);
        var values = await ActiveCandidatesAsync(owner, center, effective, cat == null ? provider.Profile.Categories : [cat], "", false, ct);
        var points = values.OrderBy(v => v.DistanceKm).ThenBy(v => v.Request.RequestId).Take(limit).Select(v => new
        {
            id = v.Request.RequestId,
            point = v.Request.PublicPoint,
            state = v.Request.Urgency == "today" ? "today" : "other",
            title = v.Request.Title[..Math.Min(80, v.Request.Title.Length)],
            subtitle = v.Request.UrgencyText,
            distanceKm = DirectoryService.PublicDistance(v.DistanceKm),
            exact = false
        }).ToArray();
        return Result<object>.Success(new
        {
            kind = "requests",
            center,
            radiusKm = effective,
            count = values.Count,
            summary = $"{values.Count} solicitudes de tu rubro dentro de {effective} km · Toca un punto para verla",
            points
        });
    }
    public async Task<Result<RequestClosure>> CloseAsync(Guid owner, Guid id, JsonElement? json, CancellationToken ct)
    {
        CloseRequest body;
        try { body = json == null || json.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? new() : json.Value.Deserialize<CloseRequest>(ProviderService.Json)!; }
        catch (JsonException) { return Fail<RequestClosure>(NexoCodes.ValidationFailed); }
        if (body == null || !RequestRules.Valid(body)) return Fail<RequestClosure>(NexoCodes.ValidationFailed);
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return Fail<RequestClosure>(NexoCodes.AccountPendingDeletion);
        return await store.CloseAsync(owner, id, body.Outcome, body.PickedProviderId, clock.GetUtcNow().UtcDateTime, ct);
    }
    private async Task<StoredProvider?> ProviderAsync(Guid owner, CancellationToken ct)
    {
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return null;
        var p = await profiles.OwnAsync(owner, ct);
        return p?.Published == true && p.Profile.Location?.IsValid == true ? p : null;
    }
    private async Task<List<RequestCandidate>> ActiveCandidatesAsync(Guid owner, ProviderPoint point, double? radius, string[]? cats, string q, bool today, CancellationToken ct)
    {
        var values = await store.CandidatesAsync(owner, point, radius, cats, q, today, ct);
        var labels = await clients.ReadAsync(values.Select(v => v.ClientId).Distinct().ToArray(), ct);
        return values.Where(v => labels.TryGetValue(v.ClientId, out var client) && client.Active).ToList();
    }
    private string Ago(DateTime created)
    {
        var elapsed = clock.GetUtcNow().UtcDateTime - created;
        return elapsed.TotalHours < 1 ? "hace un momento" : elapsed.TotalDays < 1 ? $"hace {(int)elapsed.TotalHours} h" : $"hace {(int)elapsed.TotalDays} días";
    }
    public static object Created(ServiceRequest r) => new
    {
        r.RequestId,
        r.Category,
        r.Title,
        r.Urgency,
        r.UrgencyText,
        r.ZoneLabel,
        r.PublicPoint,
        r.Status,
        r.InterestCount,
        r.ReachableProviders,
        r.CreatedAt,
        moderation = new { status = "approved", checkedAt = r.CheckedAt }
    };
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se puede completar esta operación sobre la solicitud.");
}
