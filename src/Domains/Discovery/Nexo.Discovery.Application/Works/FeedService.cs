using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Buffers.Text;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Works;

public sealed record FeedQuery(double Lat, double Lng, string? Cursor = null, int Limit = 10)
{
    public ProviderPoint Point => new(Lat, Lng);
}

public sealed record FeedPage(object[] Items, string? NextCursor, bool HasMore, bool Guest, string ZoneLabel);

public interface IFeedStore
{
    Task<WorkPage> ReadAsync(ProviderPoint point, Guid[] providerIds, Guid? after, int limit, Guid? viewer, CancellationToken ct);
}

public sealed class FeedUnavailableException : Exception;

public static class FeedRules
{
    public static bool Validate(FeedQuery query, out string error)
    {
        if (!query.Point.IsValid)
        {
            error = NexoCodes.ValidationFailed;
            return false;
        }
        if (query.Limit is < 1 or > 50)
        {
            error = NexoCodes.RequestLimitExceeded;
            return false;
        }
        if (!TryCursor(query.Cursor, query.Point, out _))
        {
            error = NexoCodes.RequestCursorInvalid;
            return false;
        }
        error = string.Empty;
        return true;
    }

    public static string Cursor(Guid workId, ProviderPoint point)
    {
        Span<byte> payload = stackalloc byte[32];
        workId.TryWriteBytes(payload);
        Scope(point).CopyTo(payload[16..]);
        return Base64Url.EncodeToString(payload);
    }

    public static bool TryCursor(string? cursor, ProviderPoint point, out Guid? workId)
    {
        workId = null;
        if (string.IsNullOrEmpty(cursor)) return true;
        if (cursor.Length > 64) return false;
        try
        {
            var bytes = Base64Url.DecodeFromChars(cursor);
            if (bytes.Length != 32 || !CryptographicOperations.FixedTimeEquals(bytes.AsSpan(16), Scope(point))) return false;
            var id = new Guid(bytes.AsSpan(0, 16));
            if (id == Guid.Empty || Cursor(id, point) != cursor) return false;
            workId = id;
            return true;
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            return false;
        }
    }

    private static byte[] Scope(ProviderPoint point)
    {
        var value = string.Join(':', point.Lat.ToString("R", CultureInfo.InvariantCulture), point.Lng.ToString("R", CultureInfo.InvariantCulture));
        return SHA256.HashData(Encoding.UTF8.GetBytes("feed/1:" + value))[..16];
    }
}

public sealed class FeedService(IDirectoryProfiles profiles, IProviderAccounts accounts, IFeedStore feed, TimeProvider clock)
{
    public async Task<Result<FeedPage>> ReadAsync(FeedQuery query, Guid? viewer, CancellationToken ct)
    {
        if (!FeedRules.Validate(query, out var error)) return Result<FeedPage>.Failure(error, "Revisa la ubicación y la página solicitadas.");

        var candidates = await profiles.CandidatesAsync(query.Point, null, ct);
        var states = await accounts.ReadManyAsync(candidates.Select(p => p.AccountId).Distinct().ToArray(), ct);
        var visible = candidates.Where(p => p.Published && p.DistanceKm is { } distance && DirectoryService.Serves(p) &&
            states.TryGetValue(p.AccountId, out var state) && state.Active).ToArray();
        var byProvider = visible.ToDictionary(p => p.Profile.ProviderId);
        var after = (Guid?)null;
        FeedRules.TryCursor(query.Cursor, query.Point, out after);
        var page = await feed.ReadAsync(query.Point, byProvider.Keys.ToArray(), after, query.Limit, viewer, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var items = page.Items.Where(w => byProvider.ContainsKey(w.ProviderId)).Select(w =>
        {
            var provider = byProvider[w.ProviderId];
            var state = states[provider.AccountId];
            var distance = provider.DistanceKm is { } km ? DirectoryService.PublicDistance(km) : null;
            var days = Math.Max(0, (int)(now - w.CreatedAt).TotalDays);
            var item = new Dictionary<string, object?>
            {
                ["workId"] = w.WorkId,
                ["provider"] = new
                {
                    provider.Profile.ProviderId,
                    provider.Profile.Name,
                    photoUrl = provider.Profile.PhotoUrl ?? state.PhotoUrl,
                    provider.Profile.Kind,
                    emailVerified = state.EmailVerified,
                    badges = state.EmailVerified ? new[] { "emailVerified" } : []
                },
                ["category"] = w.Category,
                ["title"] = w.Title,
                ["textExcerpt"] = w.Text[..Math.Min(300, w.Text.Length)],
                ["images"] = w.Images,
                ["zoneLabel"] = w.ZoneLabel,
                ["distanceText"] = distance is null ? "Zona aproximada" : distance < 1 ? "a menos de 1 km" : "a ~" + distance.Value.ToString("0.#", CultureInfo.InvariantCulture) + " km",
                ["recs"] = w.Recs,
                ["recsText"] = w.Recs == 1 ? "1 recomendación" : $"{w.Recs} recomendaciones",
                ["tag"] = w.Tag,
                ["ago"] = days == 0 ? "hoy" : $"hace {days} " + (days == 1 ? "día" : "días"),
                ["canContact"] = viewer != null && viewer != provider.AccountId
            };
            if (viewer != null)
            {
                item["saved"] = page.Saved.Contains(w.WorkId);
                item["recommended"] = page.Recommended.Contains(w.WorkId);
            }
            return (object)item;
        }).ToArray();

        var hasMore = page.HasMore && items.Length > 0;
        var next = hasMore ? FeedRules.Cursor(page.Items.Where(w => byProvider.ContainsKey(w.ProviderId)).Take(query.Limit).Last().WorkId, query.Point) : null;
        return Result<FeedPage>.Success(new(items, next, hasMore, viewer == null, string.Empty));
    }
}
