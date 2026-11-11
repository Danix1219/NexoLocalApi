using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Nexo.Discovery.Application;
using Nexo.Common.Errors;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Works;

namespace Nexo.Discovery.Endpoints;

public sealed record GeocodingReverseResponse(
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)] Place? Place,
    string Attribution);

public static class DiscoveryEndpointMapping
{
    /// <summary>
    /// Superficie HTTP de Discovery en `/v1/discovery/*` como fija nv-00.
    /// Perfiles, directorio público y ubicación de búsqueda. Feed y mapa de solicitudes pendientes.
    /// </summary>
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/discovery").WithTags("Discovery");
        group.MapProviderEndpoints();
        group.MapDirectoryEndpoints();
        group.MapWorkEndpoints();
        group.MapRequestEndpoints();
        group.MapSavedEndpoints();
        group.MapMediaEndpoints();
        group.MapGet("/feed", async (HttpContext http, FeedService service, CancellationToken ct) =>
        {
            var query = http.Request.Query;
            Guid? viewer = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var accountId) ? accountId : null;
            http.Response.Headers.CacheControl = viewer is null ? "public, max-age=30" : "private, max-age=30";
            http.Response.Headers.Vary = "Authorization";
            if (query.Any(p => p.Value.Count != 1) || !query.ContainsKey("lat") || !query.ContainsKey("lng") ||
                !double.TryParse(query["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(query["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng) ||
                !int.TryParse(query["limit"].ToString() is { Length: > 0 } limitText ? limitText : "10", NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit))
                return NexoProblem.From(NexoCodes.ValidationFailed, "Indica una ubicación y una página válidas.", http);
            var cursor = query.ContainsKey("cursor") ? query["cursor"].ToString() : null;
            if (cursor?.Length > 64) return NexoProblem.From(NexoCodes.RequestCursorInvalid, "Actualiza el feed para continuar.", http);
            try
            {
                var result = await service.ReadAsync(new FeedQuery(lat, lng, cursor, limit), viewer, ct);
                return result.Match<IResult>(body => Results.Ok(body), error => NexoProblem.From(error, http));
            }
            catch (Exception error) when (error is DirectoryUnavailableException or FeedUnavailableException || DependencyErrors.IsUnavailable(error))
            {
                http.Response.Headers.CacheControl = "no-store";
                return NexoProblem.From(NexoCodes.ServerDependencyUnavailable, "El feed no está disponible por el momento.", http);
            }
        }).AllowAnonymous().RequireRateLimiting(Nexo.Common.RateLimiting.NexoRateLimits.Public)
            .Produces(200).ProducesProblem(400).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Feed público de trabajos cercanos")
            .WithDescription("Query: lat/lng requeridos, cursor opaco ligado a la ubicación (hasta 64), limit 1–50 (por defecto 10). Incluye trabajos a 3 km o de prestadores que atienden el punto; solo publicaciones aprobadas y perfiles/cuentas activos. Sin token no expone marcas personales ni permite contacto.");
        group.MapGet("/places/search", async (string? q, double? lat, double? lng, HttpContext http, IPlaceGeocoder geocoder, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length is < 3 or > 160 ||
                lat.HasValue != lng.HasValue || (lat.HasValue && !new SearchLocation(lat.Value, lng!.Value, 10, "search").IsValid))
                return NexoProblem.From(NexoCodes.ValidationFailed, "Escribe al menos tres caracteres y elige una zona de México.", http);
            try { return Results.Ok(new { items = await geocoder.SearchAsync(q.Trim(), lat, lng, ct), attribution = "© OpenStreetMap contributors · Photon" }); }
            catch (GeocodingUnavailableException) { return NexoProblem.From(NexoCodes.ServerDependencyUnavailable, "No pudimos buscar direcciones. Puedes elegir tu zona en el mapa.", http); }
        }).AllowAnonymous().RequireRateLimiting(Nexo.Common.RateLimiting.NexoRateLimits.Public).Produces(200).ProducesProblem(400).ProducesProblem(429).ProducesProblem(503);
        group.MapGet("/places/reverse", async (double lat, double lng, HttpContext http, IPlaceGeocoder geocoder, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (!new SearchLocation(lat, lng, 10, "map").IsValid) return NexoProblem.From(NexoCodes.ValidationFailed, "Elige una ubicación dentro de México.", http);
            try { return Results.Ok(new GeocodingReverseResponse(await geocoder.ReverseAsync(lat, lng, ct), "© OpenStreetMap contributors · Photon")); }
            catch (GeocodingUnavailableException) { return NexoProblem.From(NexoCodes.ServerDependencyUnavailable, "No pudimos obtener el nombre de esta zona.", http); }
        }).AllowAnonymous().RequireRateLimiting(Nexo.Common.RateLimiting.NexoRateLimits.Public).Produces(200).ProducesProblem(400).ProducesProblem(429).ProducesProblem(503);
        group.MapGet("/me/location", async (HttpContext http, ISearchLocationStore store, IPlaceGeocoder geocoder, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            if (!Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            var saved = await store.GetAsync(id, ct);
            if (saved is not null && (string.IsNullOrWhiteSpace(saved.ZoneLabel) || saved.Label.StartsWith("Ubicaci", StringComparison.OrdinalIgnoreCase)))
            {
                try { saved = saved with { ZoneLabel = (await geocoder.ReverseAsync(saved.Lat, saved.Lng, ct))?.ZoneLabel ?? saved.ZoneLabel }; }
                catch (GeocodingUnavailableException) { /* The actual saved point remains available. */ }
            }
            return Results.Ok(saved is null ? null : new { searchLocation = new { lat = saved.Lat, lng = saved.Lng }, zoneLabel = saved.Label, radiusKm = saved.RadiusKm });
        }).RequireAuthorization().RequireRateLimiting(Nexo.Common.RateLimiting.NexoRateLimits.User).Produces(200).ProducesProblem(401);
        group.MapPut("/me/location", async (SearchLocation body, HttpContext http, ISearchLocationStore store, IPlaceGeocoder geocoder, CancellationToken ct) =>
        {
            if (!body.IsValid)
                return NexoProblem.From(NexoCodes.ValidationFailed, "Elige una ubicacion dentro de Mexico y un radio de 2, 5, 10 o 20 km.", http);
            if (!Guid.TryParse(http.User.FindFirst("sub")?.Value, out var accountId)) return Results.Unauthorized();
            Place? place = null;
            try { place = await geocoder.ReverseAsync(body.Lat, body.Lng, ct); }
            catch (GeocodingUnavailableException) { /* Saving a real point does not require a place name. */ }
            var selected = body with { ZoneLabel = place?.ZoneLabel };
            await store.SaveAsync(accountId, selected, ct);
            http.Response.Headers.CacheControl = "private, no-store";
            return Results.Ok(new
            {
                searchLocation = new { lat = body.Lat, lng = body.Lng },
                zoneLabel = selected.Label,
                radiusKm = body.RadiusKm,
                summary = "Ubicación guardada. Puedes buscar prestadores que atienden tu zona."
            });
        }).RequireAuthorization().RequireRateLimiting(Nexo.Common.RateLimiting.NexoRateLimits.User);
        return app;
    }
}
