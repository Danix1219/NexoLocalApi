using System.Globalization;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Directory;

// Implementations must exclude home coordinates in Mongo before materializing candidates.
public interface IDirectoryProfiles
{
    Task<IReadOnlyList<StoredProvider>> CandidatesAsync(ProviderPoint? point, string? category, CancellationToken ct);
}
public sealed record VisibleCandidate(StoredProvider Stored, ProviderAccountState Account)
{
    public ProviderProfile Profile => Stored.Profile;
}
public interface IProviderSortStrategy
{
    string Key { get; }
    IOrderedEnumerable<VisibleCandidate> Sort(IEnumerable<VisibleCandidate> values);
}
public sealed class RelevantProviderSort : IProviderSortStrategy
{
    public string Key => "relevance";
    public IOrderedEnumerable<VisibleCandidate> Sort(IEnumerable<VisibleCandidate> values) => values.OrderByDescending(p => p.Account.EmailVerified)
        .ThenByDescending(p => p.Profile.UpdatedAt).ThenByDescending(p => p.Profile.ProviderId);
}
public sealed class NearestProviderSort : IProviderSortStrategy
{
    public string Key => "near";
    public IOrderedEnumerable<VisibleCandidate> Sort(IEnumerable<VisibleCandidate> values) => values.OrderBy(p => p.Stored.DistanceKm)
        .ThenByDescending(p => p.Profile.ProviderId);
}
public sealed class RecommendedProviderSort : IProviderSortStrategy
{
    public string Key => "recs";
    public IOrderedEnumerable<VisibleCandidate> Sort(IEnumerable<VisibleCandidate> values) => values.OrderByDescending(p => p.Profile.Recs)
        .ThenByDescending(p => p.Profile.RatingAverage).ThenByDescending(p => p.Profile.ProviderId);
}
public sealed record Suggestion(string Kind, string Slug, string Label, string Category);
public sealed record SuggestResponse(Suggestion[] Suggestions);
public sealed record DirectoryResponse(object[] Items, string? NextCursor, bool HasMore, int Total, bool OutOfZone, string[] AppliedFilters, string? Notice);
public sealed record DirectoryMapPoint(Guid Id, ProviderPoint Point, string State, string Title, string Subtitle, double? DistanceKm, bool Exact);
public sealed record DirectoryMapResponse(string Kind, ProviderPoint Center, int RadiusKm, int Count, string Summary, DirectoryMapPoint[] Points);

public sealed class DirectoryService(IDirectoryProfiles profiles, IProviderAccounts accounts, IDirectoryCatalog catalog,
    IEnumerable<IProviderSortStrategy> strategies, TimeProvider clock)
{
    public static bool Serves(StoredProvider p) => p.DistanceKm is not { } d || d <= p.Profile.RadiusKm || (p.Profile.Attention is "shop" or "both" && d <= 10);
    public static double? PublicDistance(double? distance) => distance is { } km ? Math.Round(km * 2, MidpointRounding.AwayFromZero) / 2 : null;
    public static bool Matches(ProviderProfile profile, string query, CatalogResponse catalog)
    {
        var labels = catalog.Categories.Where(c => profile.Categories.Contains(c.Slug)).Select(c => c.Name + " " +
            string.Join(' ', c.Specialties.Where(s => profile.Specialties.Contains(s.Slug)).Select(s => s.Name)));
        var haystack = DirectoryCatalog.Normalize(string.Join(' ', profile.Name, profile.Bio, profile.Activity,
            string.Join(' ', profile.Trades), string.Join(' ', profile.Categories), string.Join(' ', profile.Specialties), string.Join(' ', labels)));
        return DirectoryCatalog.Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries).All(w => haystack.Contains(w, StringComparison.Ordinal));
    }
    private async Task<List<VisibleCandidate>> VisibleAsync(ProviderPoint? point, string? cat, CancellationToken ct)
    {
        return await ActiveAsync(await profiles.CandidatesAsync(point, cat, ct), ct);
    }
    private IEnumerable<VisibleCandidate> Filter(IEnumerable<VisibleCandidate> candidates, DirectoryQuery query, CatalogResponse cats, DateTime now) => candidates.Where(p =>
        (query.Cat == null || p.Profile.Categories.Contains(query.Cat)) && Matches(p.Profile, query.Q, cats) &&
        (!query.Open || p.Profile.Schedule?.IsOpen(now) == true) && (!query.Free || p.Profile.Visit.Mode == "free") &&
        (!query.Prices || p.Profile.Prices.Length > 0) && (!query.Urgent || p.Profile.Schedule?.Urgent == true) &&
        (query.Attention != "home" || p.Profile.Attention is "home" or "both") &&
        (query.Attention != "shop" || p.Profile.Attention is "shop" or "both"));
    public async Task<Result<DirectoryResponse>> SearchAsync(DirectoryQuery query, CancellationToken ct)
    {
        if (query.Error is { } error) return Fail<DirectoryResponse>(error);
        if (!DirectoryCursor.TryDecode(query.Cursor, query, out var after)) return Fail<DirectoryResponse>(NexoCodes.RequestCursorInvalid);
        var cats = await catalog.ReadAsync(ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await VisibleAsync(query.Point, query.Cat, ct);
        var values = Filter(candidates, query, cats, now).Where(p => query.Point == null ||
            (p.Stored.DistanceKm <= query.RadiusKm && Serves(p.Stored))).ToList();
        var outOfZone = false;
        if (values.Count == 0 && query.Expanded)
        {
            values = Filter(await VisibleAsync(query.Point, query.Cat, ct, expanded: true), query, cats, now).ToList();
            outOfZone = values.Count > 0;
        }
        var sort = outOfZone || query.Attention == "shop" ? "near" : query.Sort;
        var ordered = strategies.Single(s => s.Key == sort).Sort(values).ToArray();
        if (after != null && !ordered.Any(p => p.Profile.ProviderId == after)) return Fail<DirectoryResponse>(NexoCodes.RequestCursorInvalid);
        var page = (after == null ? ordered : ordered.SkipWhile(p => p.Profile.ProviderId != after).Skip(1)).Take(query.Limit + 1).ToArray();
        var hasMore = page.Length > query.Limit;
        var selected = page.Take(query.Limit).ToArray();
        return Result<DirectoryResponse>.Success(new(selected.Select(p => Card(p, now)).ToArray(),
            hasMore ? DirectoryCursor.Encode(selected[^1].Profile.ProviderId, query) : null, hasMore, values.Count, outOfZone, Filters(query),
            outOfZone ? "No encontramos prestadores en tu zona. Estos resultados están fuera del radio elegido." : null));
    }
    private async Task<List<VisibleCandidate>> VisibleAsync(ProviderPoint? point, string? cat, CancellationToken ct, bool expanded)
    {
        // The store's explicit expanded capability preserves distances without exposing private points.
        if (profiles is not IExpandedDirectoryProfiles all) throw new DirectoryUnavailableException();
        return await ActiveAsync(await all.ExpandedAsync(point!, cat, ct), ct);
    }
    private async Task<List<VisibleCandidate>> ActiveAsync(IReadOnlyList<StoredProvider> candidates, CancellationToken ct)
    {
        var states = await accounts.ReadManyAsync(candidates.Where(p => p.Published).Select(p => p.AccountId).Distinct().ToArray(), ct);
        return candidates.Where(p => p.Published && states.TryGetValue(p.AccountId, out var state) && state.Active)
            .Select(p => new VisibleCandidate(p, states[p.AccountId])).ToList();
    }
    public async Task<Result<SuggestResponse>> SuggestAsync(string q, ProviderPoint? point, int limit, CancellationToken ct)
    {
        if (q.Length > 40 || q.Any(char.IsControl) || point?.IsValid == false || limit is < 1 or > 10) return Fail<SuggestResponse>(NexoCodes.ValidationFailed);
        q = DirectoryCatalog.Normalize(q);
        if (q.Length < 2) return Result<SuggestResponse>.Success(new([]));
        var cats = await catalog.ReadAsync(ct);
        var items = cats.Categories.SelectMany(c => c.Specialties.Where(s => DirectoryCatalog.Normalize(s.Name).StartsWith(q, StringComparison.Ordinal))
            .Select(s => new Suggestion("specialty", s.Slug, s.Name, c.Slug))).Concat(cats.Categories
            .Where(c => DirectoryCatalog.Normalize(c.Name).StartsWith(q, StringComparison.Ordinal)).Select(c => new Suggestion("category", c.Slug, c.Name, c.Slug))).Take(limit).ToList();
        if (items.Count < limit)
            items.AddRange((await VisibleAsync(point, null, ct)).Where(p => Serves(p.Stored) && DirectoryCatalog.Normalize(p.Profile.Name).StartsWith(q, StringComparison.Ordinal))
                .OrderBy(p => DirectoryCatalog.Normalize(p.Profile.Name), StringComparer.Ordinal).ThenBy(p => p.Profile.ProviderId)
                .Take(limit - items.Count).Select(p => new Suggestion("provider", p.Profile.ProviderId.ToString(), p.Profile.Name, p.Profile.Categories.FirstOrDefault() ?? "otros")));
        return Result<SuggestResponse>.Success(new(items.ToArray()));
    }
    public async Task<DirectoryMapResponse> MapAsync(ProviderPoint center, int radius, int limit, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var cats = await catalog.ReadAsync(ct);
        var values = (await VisibleAsync(center, null, ct)).Where(p => p.Stored.DistanceKm <= radius && Serves(p.Stored) &&
            (p.Profile.Attention is "shop" or "both" ? p.Profile.Location?.IsValid == true : p.Profile.PublicZone?.Point.IsValid == true))
            .OrderBy(p => p.Stored.DistanceKm).ThenByDescending(p => p.Profile.ProviderId).ToArray();
        var points = values.Take(limit).Select(p =>
        {
            var exact = p.Profile.Attention is "shop" or "both";
            var open = p.Profile.Schedule?.IsOpen(now) == true;
            var label = cats.Categories.FirstOrDefault(c => p.Profile.Categories.Contains(c.Slug))?.Name ?? "Servicios";
            return new DirectoryMapPoint(p.Profile.ProviderId, exact ? p.Profile.Location! : p.Profile.PublicZone!.Point,
                open ? "open" : "closed", p.Profile.Name, label + (open ? " · Abierto" : " · Cerrado"), PublicDistance(p.Stored.DistanceKm), exact);
        }).ToArray();
        return new("providers", center, radius, values.Length, $"{values.Length} prestadores a menos de {radius} km que atienden este punto", points);
    }
    private static object Card(VisibleCandidate candidate, DateTime now)
    {
        var p = candidate.Profile;
        var distance = PublicDistance(candidate.Stored.DistanceKm);
        var min = p.Prices.Where(p => p.Unit == "from" && p.FromCents != null).OrderBy(p => p.FromCents).FirstOrDefault();
        var money = CultureInfo.GetCultureInfo("es-MX");
        string Money(int cents) => (cents / 100m).ToString(cents % 100 == 0 ? "C0" : "C2", money);
        var open = p.Schedule?.IsOpen(now) == true;
        var days = Math.Max(0, (int)(now - p.UpdatedAt).TotalDays);
        return new
        {
            p.ProviderId,
            p.Name,
            p.Kind,
            p.PhotoUrl,
            p.Categories,
            p.Specialties,
            bioExcerpt = p.Bio[..Math.Min(160, p.Bio.Length)],
            zoneLabel = p.PublicZone?.Label ?? "Zona aproximada",
            attentionText = p.Attention == "home" ? "Va a domicilio" : p.Attention == "shop" ? "En local" : "Local y domicilio",
            distanceKm = distance,
            distanceText = distance is null ? null : distance < 1 ? "a menos de 1 km" : "a ~" + distance.Value.ToString("0.#", CultureInfo.InvariantCulture) + " km",
            status = new { open, text = p.Schedule == null ? "Horario sin publicar" : open ? "Abierto ahora" : "Cerrado" },
            minPrice = min == null ? null : new { fromCents = min.FromCents, text = "Desde " + Money(min.FromCents!.Value) },
            visit = new { p.Visit.Mode, text = p.Visit.Mode == "free" ? "Visita sin costo" : p.Visit.Mode == "paid" ? "Visita " + Money(p.Visit.AmountCents.GetValueOrDefault()) : "Costo de visita sin publicar" },
            rating = new { avg = p.RatingAverage, count = p.RatingCount },
            p.Recs,
            badges = candidate.Account.EmailVerified ? new[] { "emailVerified" } : [],
            updatedText = days == 0 ? "Actualizado hoy" : $"Actualizado hace {days} " + (days == 1 ? "día" : "días")
        };
    }
    private static string[] Filters(DirectoryQuery q) => new[] { q.Open ? "open" : null, q.Free ? "free" : null, q.Prices ? "prices" : null,
        q.Urgent ? "urgent" : null, q.Attention != "all" ? "attention:" + q.Attention : null }.OfType<string>().ToArray();
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "Revisa los filtros, la ubicación y la página solicitada.");
}
public interface IExpandedDirectoryProfiles : IDirectoryProfiles
{
    Task<IReadOnlyList<StoredProvider>> ExpandedAsync(ProviderPoint point, string? category, CancellationToken ct);
}
