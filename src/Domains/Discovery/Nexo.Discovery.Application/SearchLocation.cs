namespace Nexo.Discovery.Application;

public sealed record SearchLocation(double Lat, double Lng, int RadiusKm, string Source, string? ZoneLabel = null)
{
    public bool IsValid => double.IsFinite(Lat) && double.IsFinite(Lng)
        && Lat is >= 14 and <= 33 && Lng is >= -119 and <= -86
        && RadiusKm is 2 or 5 or 10 or 20 && Source is "map" or "gps" or "search";
    public string Label => string.IsNullOrWhiteSpace(ZoneLabel) ? FormattableString.Invariant($"Ubicación {Lat:F3}, {Lng:F3}") : ZoneLabel;
}

public interface ISearchLocationStore
{
    Task SaveAsync(Guid accountId, SearchLocation location, CancellationToken ct);
    Task<SearchLocation?> GetAsync(Guid accountId, CancellationToken ct);
}
