using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nexo.Discovery.Application;

namespace Nexo.Discovery.Infrastructure;

public sealed class GeocodingOptions
{
    public string BaseUrl { get; set; } = "https://photon.komoot.io/";
}

/// <summary>Bounded cache, one upstream request at a time, no address/request logging.</summary>
public sealed class PhotonGeocoder(HttpClient client, IOptions<GeocodingOptions> options) : IPlaceGeocoder, IDisposable
{
    private readonly SemaphoreSlim gate = new(1);
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 128 });
    private DateTimeOffset nextRequest;

    public Task<IReadOnlyList<Place>> SearchAsync(string query, double? lat, double? lng, CancellationToken ct) =>
        FetchAsync($"api/?q={Uri.EscapeDataString(query)}&limit=5&bbox=-119,14,-86,33" +
            (lat.HasValue && lng.HasValue ? $"&lat={F(lat.Value)}&lon={F(lng.Value)}" : ""), ct);

    public async Task<Place?> ReverseAsync(double lat, double lng, CancellationToken ct) =>
        (await FetchAsync($"reverse?lat={F(lat)}&lon={F(lng)}&limit=1&radius=2", ct)).FirstOrDefault();

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private async Task<IReadOnlyList<Place>> FetchAsync(string path, CancellationToken ct)
    {
        if (cache.TryGetValue(path, out IReadOnlyList<Place>? found)) return found!;
        // Fail promptly rather than build an unbounded queue for the public provider.
        if (!await gate.WaitAsync(0, ct)) throw new GeocodingUnavailableException();
        try
        {
            if (!Uri.TryCreate(options.Value.BaseUrl, UriKind.Absolute, out var origin) || origin.Scheme != "https" ||
                !string.IsNullOrEmpty(origin.UserInfo)) throw new GeocodingUnavailableException();
            var delay = nextRequest - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
            nextRequest = DateTimeOffset.UtcNow.AddSeconds(1);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(origin, path));
            request.Headers.UserAgent.ParseAdd("NexoLocal/1.0");
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw new GeocodingUnavailableException();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var result = Parse(json.RootElement);
            cache.Set(path, result, new MemoryCacheEntryOptions().SetSize(1).SetAbsoluteExpiration(TimeSpan.FromMinutes(15)));
            return result;
        }
        catch (HttpRequestException) { throw new GeocodingUnavailableException(); }
        catch (JsonException) { throw new GeocodingUnavailableException(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new GeocodingUnavailableException(); }
        finally { gate.Release(); }
    }

    public static IReadOnlyList<Place> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
            throw new GeocodingUnavailableException();
        var result = new List<Place>();
        foreach (var feature in features.EnumerateArray().Take(20))
        {
            if (!feature.TryGetProperty("properties", out var p) || p.ValueKind != JsonValueKind.Object ||
                !feature.TryGetProperty("geometry", out var geometry) || geometry.ValueKind != JsonValueKind.Object ||
                !geometry.TryGetProperty("coordinates", out var coords) || coords.ValueKind != JsonValueKind.Array || coords.GetArrayLength() != 2 ||
                coords[0].ValueKind != JsonValueKind.Number || coords[1].ValueKind != JsonValueKind.Number ||
                !coords[0].TryGetDouble(out var lng) || !coords[1].TryGetDouble(out var lat) ||
                !new SearchLocation(lat, lng, 10, "search").IsValid ||
                !string.Equals(Text(p, "countrycode"), "MX", StringComparison.OrdinalIgnoreCase)) continue;
            var city = Text(p, "city");
            if (city.Length == 0) city = Text(p, "county");
            if (city.Length == 0 && Text(p, "type") is "city" or "county" or "district" or "locality") city = Text(p, "name");
            var zone = Join(Text(p, "district"), city, Text(p, "state"));
            if (zone.Length == 0) zone = Text(p, "name");
            var label = Join(Text(p, "name"), Text(p, "street"), Text(p, "housenumber"), Text(p, "district"), city, Text(p, "state"));
            if (label.Length > 0 && zone.Length > 0) result.Add(new(lat, lng, label, zone));
            if (result.Count == 5) break;
        }
        return result;
    }
    private static string Text(JsonElement p, string key) => p.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
        ? (v.GetString() ?? "").Trim()[..Math.Min((v.GetString() ?? "").Trim().Length, 160)] : "";
    private static string Join(params string[] parts) => string.Join(", ", parts.Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
    public void Dispose() { cache.Dispose(); gate.Dispose(); client.Dispose(); }
}
