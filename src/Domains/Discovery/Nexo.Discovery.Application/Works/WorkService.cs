using System.Security.Cryptography;
using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Works;

public sealed class WorkService(IWorkStore store, IProviderStore profiles, IProviderAccounts accounts, IProviderSafety safety, TimeProvider clock)
{
    public async Task<Result<Work>> PublishAsync(Guid owner, string? key, JsonElement json, CancellationToken ct)
    {
        if (!Guid.TryParse(key, out var parsedKey) || parsedKey == Guid.Empty) return Fail<Work>(NexoCodes.RequestIdempotencyKeyMissing);
        PublishWork? body;
        try { body = json.Deserialize<PublishWork>(ProviderService.Json); }
        catch (JsonException) { return Fail<Work>(NexoCodes.ValidationFailed); }
        var provider = await profiles.OwnAsync(owner, ct);
        if (provider?.Published != true) return Fail<Work>(NexoCodes.ProviderProfileRequired);
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return Fail<Work>(NexoCodes.AccountPendingDeletion);
        if (WorkRules.Validate(body, provider.Profile.Categories) is { } error) return Fail<Work>(error);
        var input = body! with { Title = body!.Title.Trim(), Text = body.Text.Trim(), ImageUploadIds = body.ImageUploadIds ?? [] };
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(input, ProviderService.Json)));
        if (await store.ReceiptAsync(owner, parsedKey, ct) is { } receipt)
        {
            if (receipt.Hash != hash) return Fail<Work>(NexoCodes.RequestIdempotencyKeyReused);
            var previous = await store.FindAsync(receipt.WorkId, ct);
            return previous == null ? Fail<Work>(NexoCodes.WorkNotFound) : Result<Work>.Success(previous);
        }
        var moderated = await safety.ModerateAsync(string.Join("\n", input.Title, input.Text, input.Problem, input.Did, input.Time, input.Tag), ct);
        if (moderated.IsFailure) return Result<Work>.Failure(moderated.Error!);
        if (!moderated.Value) return Fail<Work>(NexoCodes.ContentInappropriate);
        var images = new List<ProviderImage>();
        foreach (var image in input.ImageUploadIds!)
        {
            var resolved = await safety.ImageAsync(owner, image, "work", ct);
            if (resolved.IsFailure) return Result<Work>.Failure(resolved.Error!);
            if (!Uri.TryCreate(resolved.Value!.Url, UriKind.Absolute, out var media) || !media.AbsolutePath.StartsWith("/v1/discovery/media/", StringComparison.Ordinal)) return Fail<Work>(NexoCodes.ServerDependencyUnavailable);
            images.Add(resolved.Value!);
        }
        // Only owned, approved managed media can be attached to a publication.

        var now = clock.GetUtcNow().UtcDateTime;
        var zone = provider.Profile.Location == input.Location ? provider.Profile.PublicZone?.Label : null;
        var work = new Work(Guid.CreateVersion7(), provider.Profile.ProviderId, input.Category, input.Title, input.Text,
            input.Problem, input.Did, input.Time, input.Tag, input.Location.Blur(), string.IsNullOrWhiteSpace(zone) ? "Zona aproximada" : zone, images.ToArray(), 0, now, now);
        return await store.CreateAsync(owner, parsedKey, hash, work, ct);
    }
    public async Task<Result<object>> ListAsync(Guid providerId, Guid? viewer, string? cursor, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 50) return Fail<object>(NexoCodes.RequestLimitExceeded);
        if (!WorkRules.TryCursor(cursor, providerId, out var after)) return Fail<object>(NexoCodes.RequestCursorInvalid);
        var provider = await PublicProviderAsync(providerId, ct);
        if (provider == null) return Fail<object>(NexoCodes.ProviderNotFound);
        var page = await store.ListAsync(providerId, after, limit, viewer, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var items = page.Items.Select(w =>
        {
            var days = Math.Max(0, (int)(now - w.CreatedAt).TotalDays);
            var item = new Dictionary<string, object?>
            {
                ["workId"] = w.WorkId,
                ["category"] = w.Category,
                ["title"] = w.Title,
                ["text"] = w.Text,
                ["textExcerpt"] = w.Text[..Math.Min(300, w.Text.Length)],
                ["images"] = w.Images,
                ["zoneLabel"] = w.ZoneLabel,
                ["recs"] = w.Recs,
                ["recsText"] = w.Recs == 1 ? "1 recomendación" : $"{w.Recs} recomendaciones",
                ["tag"] = w.Tag,
                ["ago"] = days == 0 ? "hoy" : $"hace {days} " + (days == 1 ? "día" : "días")
            };
            if (viewer != null) { item["saved"] = page.Saved.Contains(w.WorkId); item["recommended"] = page.Recommended.Contains(w.WorkId); }
            return item;
        }).ToArray();
        return Result<object>.Success(new { items, nextCursor = page.HasMore ? WorkRules.Cursor(page.Items[^1].WorkId, providerId) : null, page.HasMore, total = provider.Profile.Jobs });
    }
    public async Task<Result<Work>> EditAsync(Guid owner, Guid id, JsonElement json, CancellationToken ct)
    {
        EditWork? body;
        try { body = json.Deserialize<EditWork>(ProviderService.Json); }
        catch (JsonException) { return Fail<Work>(NexoCodes.ValidationFailed); }
        if (WorkRules.ValidateEdit(body) is { } error) return Fail<Work>(error);
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return Fail<Work>(NexoCodes.AccountPendingDeletion);
        var provider = await profiles.OwnAsync(owner, ct);
        var current = await store.FindAsync(id, ct);
        if (provider?.Published != true || current == null || current.ProviderId != provider.Profile.ProviderId) return Fail<Work>(NexoCodes.WorkNotFound);
        var title = body!.Title.Trim(); var text = body.Text.Trim();
        var moderation = await safety.ModerateAsync(string.Join("\n", title, text, current.Problem, current.Did, current.Time, body.Tag), ct);
        if (moderation.IsFailure) return Result<Work>.Failure(moderation.Error!);
        if (!moderation.Value) return Fail<Work>(NexoCodes.ContentInappropriate);
        var images = new List<ProviderImage>();
        foreach (var upload in body.ImageUploadIds ?? [])
        {
            var resolved = await safety.ImageAsync(owner, upload, "work", ct);
            if (resolved.IsFailure) return Result<Work>.Failure(resolved.Error!);
            if (!Uri.TryCreate(resolved.Value!.Url, UriKind.Absolute, out var media) || !media.AbsolutePath.StartsWith("/v1/discovery/media/", StringComparison.Ordinal)) return Fail<Work>(NexoCodes.ServerDependencyUnavailable);
            images.Add(resolved.Value!);
        }
        return await store.UpdateAsync(owner, id, current with
        {
            Title = title,
            Text = text,
            Tag = body.Tag,
            Images = images.ToArray(),
            CheckedAt = clock.GetUtcNow().UtcDateTime
        }, ct);
    }
    public async Task<Result<bool>> DeleteAsync(Guid owner, Guid id, CancellationToken ct)
    {
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return Fail<bool>(NexoCodes.AccountPendingDeletion);
        return await store.DeleteAsync(owner, id, ct);
    }
    public async Task<Result<WorkRecommendation>> RecommendAsync(Guid viewer, Guid id, bool desired, CancellationToken ct)
    {
        if ((await accounts.ReadAsync(viewer, ct))?.Active != true) return Fail<WorkRecommendation>(NexoCodes.AccountPendingDeletion);
        var work = await store.FindAsync(id, ct);
        if (work == null) return Fail<WorkRecommendation>(NexoCodes.WorkNotFound);
        var provider = await PublicProviderAsync(work.ProviderId, ct);
        if (provider == null) return Fail<WorkRecommendation>(NexoCodes.WorkNotFound);
        if (provider.AccountId == viewer) return Fail<WorkRecommendation>(NexoCodes.WorkOwnContent);
        return await store.RecommendAsync(viewer, id, desired, ct);
    }
    private async Task<StoredProvider?> PublicProviderAsync(Guid id, CancellationToken ct)
    {
        var provider = (await profiles.PublicAsync([id], null, ct)).SingleOrDefault();
        return provider?.Published == true && (await accounts.ReadAsync(provider.AccountId, ct))?.Active == true ? provider : null;
    }
    public static object Created(Work w) => new { w.WorkId, w.Category, w.Title, w.Images, w.ZoneLabel, w.PublicPoint, w.Recs, w.CreatedAt, moderation = new { status = "approved", checkedAt = w.CheckedAt } };
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se puede completar esta operación sobre el trabajo.");
}
