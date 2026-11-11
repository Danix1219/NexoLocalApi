namespace Nexo.Discovery.Application;

public sealed record Place(double Lat, double Lng, string Label, string ZoneLabel);
public interface IPlaceGeocoder
{
    Task<IReadOnlyList<Place>> SearchAsync(string query, double? lat, double? lng, CancellationToken ct);
    Task<Place?> ReverseAsync(double lat, double lng, CancellationToken ct);
}
public sealed class GeocodingUnavailableException : Exception;
