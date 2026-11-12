using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Providers;

public sealed class ProviderService(IProviderStore store, IProviderSafety safety, IProviderAccounts accounts, IPlaceGeocoder? geocoder = null, ISearchLocationStore? savedLocations = null)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    public static readonly Dictionary<string, string[]> Sections = new()
    {
        ["basic"] = ["name", "owner", "experience", "crew", "teamSize", "photoUploadId", "coverUploadId"],
        ["services"] = ["sector", "activity", "trades", "categories", "specialties", "bio"],
        ["zone"] = ["location", "radiusKm", "attention"],
        ["hours"] = ["schedule"],
        ["prices"] = ["visit", "prices"],
        ["terms"] = ["channels", "payment", "responseTime", "warranty", "invoice", "web"],
        ["portfolio"] = ["portfolioUploadIds"]
    };
    private static readonly string[] CreateFields = ["kind", "name", "owner", "sector", "activity", "bio", "experience", "crew", "teamSize", "attention", "location", "radiusKm", "trades", "categories", "specialties", "photoUploadId", "coverUploadId", "acceptedRules"];
    public async Task<Result<ProviderProfile>> CreateAsync(Guid owner, string? key, JsonElement body, CancellationToken ct)
    {
        if (!Guid.TryParse(key, out var parsedKey)) return Fail<ProviderProfile>(NexoCodes.RequestIdempotencyKeyMissing);
        if (!Allowed(body, CreateFields) || !body.TryGetProperty("acceptedRules", out var accepted) || accepted.ValueKind != JsonValueKind.True ||
            new[] { "kind", "name", "bio", "attention", "location", "radiusKm" }.Any(f => !body.TryGetProperty(f, out _))) return Fail<ProviderProfile>(NexoCodes.ValidationFailed);
        var hash = Hash(body);
        var old = await store.OwnAsync(owner, ct);
        if (old != null)
        {
            if (old.CreationKey != parsedKey.ToString()) return Fail<ProviderProfile>(NexoCodes.ProviderAlreadyExists);
            if (old.CreationHash != hash) return Fail<ProviderProfile>(NexoCodes.RequestIdempotencyKeyReused);
            return await accounts.EnableProviderAsync(owner, ct) ? Result<ProviderProfile>.Success(old.CreationProfile ?? old.Profile) : Fail<ProviderProfile>(NexoCodes.AccountPendingDeletion);
        }
        var account = await accounts.ReadAsync(owner, ct);
        if (account?.Active != true) return Fail<ProviderProfile>(NexoCodes.AccountPendingDeletion);
        if (!account.EmailVerified) return Fail<ProviderProfile>(NexoCodes.AuthEmailNotVerified);
        ProviderProfile profile;
        try
        {
            var node = JsonNode.Parse(body.GetRawText())!.AsObject();
            foreach (var field in new[] { "acceptedRules", "photoUploadId", "coverUploadId" }) node.Remove(field);
            profile = node.Deserialize<ProviderProfile>(Json)!;
        }
        catch (JsonException) { return Fail<ProviderProfile>(NexoCodes.ValidationFailed); }
        profile = await DeriveAsync(profile, null, ct, owner);
        if (ProviderRules.Validate(profile, "all", account) is { } code) return Fail<ProviderProfile>(code);
        var moderated = await safety.ModerateAsync(string.Join("\n", profile.Name, profile.Owner, profile.Activity, profile.Bio), ct);
        if (moderated.IsFailure) return Result<ProviderProfile>.Failure(moderated.Error!);
        var images = await ResolveImages(owner, profile, body, ct);
        if (images.IsFailure) return images;
        profile = images.Value! with { ProviderId = Guid.CreateVersion7(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        if (!await store.InsertAsync(new(owner, profile, 0, parsedKey.ToString(), hash, true), ct))
            return await CreateAsync(owner, key, body, ct); // Unique accountId winner: compare its key/hash, never insert twice.
        if (!await accounts.EnableProviderAsync(owner, ct)) return Fail<ProviderProfile>(NexoCodes.AccountPendingDeletion);
        return Result<ProviderProfile>.Success(profile);
    }
    public async Task<Result<Dictionary<string, object?>>> OwnAsync(Guid owner, CancellationToken ct)
    {
        var p = await store.OwnAsync(owner, ct);
        if (p == null) return Fail<Dictionary<string, object?>>(NexoCodes.ProviderNotFound);
        if ((geocoder != null || savedLocations != null) && p.Profile.Location?.IsValid == true && p.Profile.PublicZone?.Label == "Zona aproximada")
        {
            var refreshed = await DeriveAsync(p.Profile, null, ct, owner);
            if (refreshed.PublicZone?.Label != "Zona aproximada" &&
                await store.PatchAsync(p, refreshed, "zone", ["publicZone"], ct))
                p = p with { Profile = refreshed };
        }
        var state = await accounts.ReadAsync(owner, ct);
        if (state?.Active != true) return Fail<Dictionary<string, object?>>(NexoCodes.AccountPendingDeletion);
        // Repairs a previously interrupted create/Identity acknowledgement without changing mode.
        if (!await accounts.EnableProviderAsync(owner, ct)) return Fail<Dictionary<string, object?>>(NexoCodes.AccountPendingDeletion);
        return Result<Dictionary<string, object?>>.Success(View(p, state, true));
    }
    public async Task<Result<Dictionary<string, object?>>> PublicAsync(Guid id, ProviderPoint? from, CancellationToken ct)
    {
        var result = await VisibleAsync([id], from, ct);
        if (result.IsSuccess) await store.RecordViewAsync(id, ct);
        return result.IsSuccess ? Result<Dictionary<string, object?>>.Success(result.Value![0]) : Result<Dictionary<string, object?>>.Failure(result.Error!);
    }
    public async Task<Result<List<Dictionary<string, object?>>>> VisibleAsync(Guid[] ids, ProviderPoint? from, CancellationToken ct)
    {
        var values = await store.PublicAsync(ids, from, ct);
        var output = new List<Dictionary<string, object?>>();
        foreach (var id in ids)
        {
            var value = values.FirstOrDefault(p => p.Profile.ProviderId == id);
            if (value == null) return Fail<List<Dictionary<string, object?>>>(NexoCodes.ProviderNotFound);
            var account = await accounts.ReadAsync(value.AccountId, ct);
            if (account?.Active != true || !value.Published) return Fail<List<Dictionary<string, object?>>>(NexoCodes.ProviderNotFound);
            output.Add(View(value, account, false));
        }
        return Result<List<Dictionary<string, object?>>>.Success(output);
    }
    public async Task<Result<Dictionary<string, object?>>> PatchAsync(Guid owner, string section, JsonElement body, CancellationToken ct)
    {
        if (!Sections.TryGetValue(section, out var fields)) return Fail<Dictionary<string, object?>>(NexoCodes.ResourceNotFound);
        if (!Allowed(body, fields) || !body.EnumerateObject().Any()) return Fail<Dictionary<string, object?>>(NexoCodes.ValidationFailed);
        var old = await store.OwnAsync(owner, ct);
        if (old == null) return Fail<Dictionary<string, object?>>(NexoCodes.ProviderNotFound);
        var account = await accounts.ReadAsync(owner, ct);
        if (account?.Active != true) return Fail<Dictionary<string, object?>>(NexoCodes.AccountPendingDeletion);
        ProviderProfile profile;
        try
        {
            var merged = JsonSerializer.SerializeToNode(old.Profile, Json)!.AsObject();
            foreach (var property in body.EnumerateObject())
                if (property.Name is not ("photoUploadId" or "coverUploadId" or "portfolioUploadIds")) merged[property.Name] = JsonNode.Parse(property.Value.GetRawText());
            profile = merged.Deserialize<ProviderProfile>(Json)!;
        }
        catch (JsonException) { return Fail<Dictionary<string, object?>>(NexoCodes.ValidationFailed); }
        profile = await DeriveAsync(profile, old.Profile, ct, owner);
        if (ProviderRules.Validate(profile, section, account) is { } error) return Fail<Dictionary<string, object?>>(error);
        if (section is "basic" or "services" or "prices" or "terms")
        {
            var text = section switch
            {
                "basic" => string.Join("\n", profile.Name, profile.Owner),
                "services" => string.Join("\n", profile.Activity, profile.Bio, string.Join(" ", profile.Trades)),
                "prices" => string.Join("\n", profile.Prices.Select(p => p.Service)),
                _ => profile.Warranty ?? ""
            };
            if (text.Length > 0)
            {
                var moderation = await safety.ModerateAsync(text, ct);
                if (moderation.IsFailure) return Result<Dictionary<string, object?>>.Failure(moderation.Error!);
            }
        }
        var images = await ResolveImages(owner, profile, body, ct);
        if (images.IsFailure) return Result<Dictionary<string, object?>>.Failure(images.Error!);
        profile = images.Value! with { UpdatedAt = DateTime.UtcNow };
        var persisted = section switch
        {
            "basic" => new[] { "name", "owner", "experience", "crew", "teamSize", "photoUrl", "coverUrl" },
            "services" => new[] { "sector", "activity", "trades", "categories", "specialties", "bio" },
            "zone" => new[] { "location", "radiusKm", "attention", "publicZone" },
            "portfolio" => new[] { "portfolio" },
            _ => fields
        };
        if (!await store.PatchAsync(old, profile, section, persisted, ct)) return Fail<Dictionary<string, object?>>(NexoCodes.AccountConcurrentUpdate);
        return Result<Dictionary<string, object?>>.Success(View(old with { Profile = profile }, account, true));
    }
    private async Task<ProviderProfile> DeriveAsync(ProviderProfile p, ProviderProfile? old, CancellationToken ct, Guid owner)
    {
        var location = p.Location;
        var zone = old != null && old.Location == location ? old.PublicZone : null;
        if (location?.IsValid == true && zone == null)
        {
            var label = "Zona aproximada";
            if (savedLocations != null)
            {
                var selected = await savedLocations.GetAsync(owner, ct);
                if (selected != null && Math.Abs(selected.Lat - location.Lat) < 0.00001 &&
                    Math.Abs(selected.Lng - location.Lng) < 0.00001 &&
                    !string.IsNullOrWhiteSpace(selected.ZoneLabel) && !selected.ZoneLabel.StartsWith("Ubicaci", StringComparison.OrdinalIgnoreCase))
                    label = selected.ZoneLabel;
            }
            if (label == "Zona aproximada" && geocoder != null)
            {
                try
                {
                    var place = await geocoder.ReverseAsync(location.Lat, location.Lng, ct);
                    if (!string.IsNullOrWhiteSpace(place?.ZoneLabel)) label = place.ZoneLabel;
                }
                catch (GeocodingUnavailableException) { }
            }
            zone = new(label, location.Blur());
        }
        return p with
        {
            Name = p.Name?.Trim()!,
            Bio = p.Bio?.Trim()!,
            Attention = p.Kind == "trade" ? "home" : p.Attention,
            Categories = p.Kind == "business" ? ProviderRules.Infer($"{p.Activity} {p.Bio}") : p.Categories,
            PublicZone = location?.IsValid == true ? zone : null
        };
    }
    private async Task<Result<ProviderProfile>> ResolveImages(Guid owner, ProviderProfile p, JsonElement body, CancellationToken ct)
    {
        foreach (var (field, purpose) in new[] { ("photoUploadId", "avatar"), ("coverUploadId", "cover") })
        {
            if (!body.TryGetProperty(field, out var value)) continue;
            string? url = null;
            if (value.ValueKind != JsonValueKind.Null)
            {
                if (value.ValueKind != JsonValueKind.String || !Guid.TryParse(value.GetString(), out var id)) return Fail<ProviderProfile>(NexoCodes.ValidationFailed);
                var image = await safety.ImageAsync(owner, id, purpose, ct);
                if (image.IsFailure) return Result<ProviderProfile>.Failure(image.Error!);
                url = image.Value!.Url;
            }
            p = field == "photoUploadId" ? p with { PhotoUrl = url } : p with { CoverUrl = url };
        }
        if (body.TryGetProperty("portfolioUploadIds", out var portfolio))
        {
            if (portfolio.ValueKind != JsonValueKind.Array) return Fail<ProviderProfile>(NexoCodes.ValidationFailed);
            if (portfolio.GetArrayLength() > 6) return Fail<ProviderProfile>(NexoCodes.ContentImagesLimit);
            var images = new List<ProviderImage>();
            var ids = new HashSet<Guid>();
            foreach (var value in portfolio.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.String || !Guid.TryParse(value.GetString(), out var id) || !ids.Add(id)) return Fail<ProviderProfile>(NexoCodes.ValidationFailed);
                var image = await safety.ImageAsync(owner, id, "portfolio", ct);
                if (image.IsFailure) return Result<ProviderProfile>.Failure(image.Error!);
                images.Add(image.Value!);
            }
            p = p with { Portfolio = images.ToArray() };
        }
        return Result<ProviderProfile>.Success(p);
    }
    private static bool Allowed(JsonElement body, string[] fields) => body.ValueKind == JsonValueKind.Object &&
        body.EnumerateObject().All(p => fields.Contains(p.Name, StringComparer.Ordinal)) && body.EnumerateObject().Select(p => p.Name).Distinct().Count() == body.EnumerateObject().Count();
    private static string Hash(JsonElement body)
    {
        static object? Canonical(JsonElement e) => e.ValueKind switch
        {
            JsonValueKind.Object => e.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToDictionary(p => p.Name, p => Canonical(p.Value)),
            JsonValueKind.Array => e.EnumerateArray().Select(Canonical).ToArray(),
            _ => e
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Canonical(body)))));
    }
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operacion del perfil.");
    public static Dictionary<string, object?> View(StoredProvider stored, ProviderAccountState account, bool own)
    {
        var p = stored.Profile;
        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(p, Json), Json)!;
        data.Remove("location"); data.Remove("createdAt"); data.Remove("ratingAverage"); data.Remove("ratingCount");
        data.Remove("completeness");
        var open = p.Schedule?.IsOpen(DateTime.UtcNow) == true;
        var distance = stored.DistanceKm;
        data["rating"] = new { avg = p.RatingAverage, count = p.RatingCount };
        data["emailVerified"] = account.EmailVerified;
        data["isNew"] = DateTime.UtcNow - p.CreatedAt < TimeSpan.FromDays(30);
        data["channels"] = p.Channels with { Call = p.Channels.Call && account.EmailVerified && account.HasPhone };
        data["status"] = new { open, text = p.Schedule == null ? "Horario sin publicar" : open ? "Abierto ahora" : "Cerrado" };
        data["distanceKm"] = distance is { } d ? Math.Round(d, 1) : null;
        data["servesYou"] = distance is null ? (bool?)null : distance <= p.RadiusKm || (p.Attention is "shop" or "both" && distance <= 10);
        data["attentionText"] = distance is null ? (p.Attention == "home" ? "A domicilio" : p.Attention == "shop" ? "En local" : "Local y domicilio") : p.Attention == "home" ? (distance <= p.RadiusKm ? "Va a domicilio" : "Fuera de su zona de atencion") : FormattableString.Invariant($"Su local esta a {distance:F1} km");
        data["shopLocation"] = p.Attention is "shop" or "both" ? p.Location : null;
        data["minPrice"] = p.Prices.Where(i => i.Unit == "from" && i.FromCents != null).OrderBy(i => i.FromCents).FirstOrDefault() is { } min ? new { fromCents = min.FromCents } : null;
        data["updatedText"] = "Actualizado " + p.UpdatedAt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        data["badges"] = account.EmailVerified ? new[] { new { key = "emailVerified", level = "verifiedByNexo", label = "Correo confirmado" } } : [];
        data["trust"] = new { declared = new[] { "experience", "payment", "warranty", "responseTime", "invoice" }, verifiedByNexo = account.EmailVerified ? new[] { "email" } : [], userOpinion = new { rating = new { avg = p.RatingAverage, count = p.RatingCount }, recs = p.Recs } };
        if (own)
        {
            data["location"] = p.Location;
            data["completeness"] = p.Completeness;
            data["policyVersion"] = ProviderProfile.CompletenessPolicyVersion;
            data["stats"] = new { views = stored.Views, contactClicks = (int?)null, interestsGiven = (int?)null };
            data["todo"] = new[] { ("photo", "Agrega tu foto", 7, p.PhotoUrl != null), ("schedule", "Publica tu horario", 14, p.Schedule?.IsValid == true), ("prices", "Agrega precios", 14, p.Prices.Length > 0), ("works", "Publica un trabajo", 14, p.Jobs > 0), ("terms", "Completa condiciones", 9, p.Payment.Length > 0 || p.Warranty != null), ("cover", "Agrega una portada", 7, p.CoverUrl != null) }.Select(t => new { key = t.Item1, label = t.Item2, points = t.Item3, done = t.Item4 }).ToArray();
        }
        else { data.Remove("sector"); data.Remove("teamSize"); }
        return data;
    }
}
