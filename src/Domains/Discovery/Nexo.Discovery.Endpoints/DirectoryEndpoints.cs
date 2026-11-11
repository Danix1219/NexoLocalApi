using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Endpoints;

public static class DirectoryEndpoints
{
    public static RouteGroupBuilder MapDirectoryEndpoints(this RouteGroupBuilder group)
    {
        Public(group.MapGet("/categories", async (IDirectoryCatalog catalog, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                var data = JsonSerializer.SerializeToUtf8Bytes(await catalog.ReadAsync(ct), ProviderService.Json);
                var etag = '"' + Convert.ToHexString(SHA256.HashData(data)) + '"';
                http.Response.Headers.ETag = etag;
                http.Response.Headers.CacheControl = "public, max-age=3600";
                if (http.Request.GetTypedHeaders().IfNoneMatch?.Any(t => t.Tag == etag || t.Tag == "*") == true) return Results.StatusCode(304);
                return Results.Bytes(data, "application/json");
            }
            catch (DirectoryUnavailableException) { return Unavailable(http); }
        })).Produces(304).WithSummary("Catálogo público de categorías y especialidades")
            .WithDescription("Sin parámetros. Catálogo versionado con ETag y soporte If-None-Match/304; no publica keywords de inferencia.");
        Public(group.MapGet("/search/providers", async (DirectoryService service, HttpContext http, CancellationToken ct) =>
        {
            Private(http);
            if (!TryQuery(http.Request.Query, out var query)) return Invalid(http);
            try
            {
                var result = await service.SearchAsync(query, ct);
                return result.Match<IResult>(body => Results.Json(body, ProviderService.Json), e => NexoProblem.From(e, http));
            }
            catch (DirectoryUnavailableException) { return Unavailable(http); }
        })).WithSummary("Buscar prestadores publicados")
            .WithDescription("Query: q (hasta 80), cat, lat/lng, radiusKm (2/5/10/20), open/free/prices/urgent (bool), attention (all/home/shop), sort (relevance/near/recs), expanded (bool), cursor (hasta 64), limit (1–50). near y shop requieren punto. Sin ubicación no se aplica zona. No devuelve contactos ni coordenadas home.");
        Public(group.MapGet("/search/suggest", async (DirectoryService service, HttpContext http, CancellationToken ct) =>
        {
            Private(http);
            var q = http.Request.Query;
            if (!SingleValues(q) || !Point(q, out var point) || !Int(q, "limit", 8, out var limit)) return Invalid(http);
            try
            {
                var result = await service.SuggestAsync(q["q"].ToString(), point, limit, ct);
                return result.Match<IResult>(body => Results.Ok(body), e => NexoProblem.From(e, http));
            }
            catch (DirectoryUnavailableException) { return Unavailable(http); }
        })).WithSummary("Sugerencias de especialidades, categorías y prestadores")
            .WithDescription("Query: q (hasta 40; menos de 2 devuelve vacío), lat/lng opcionales, limit (1–10; por defecto 8). Orden: especialidad, categoría, nombre público de prestador activo.");
        Public(group.MapGet("/map/nearby", async (DirectoryService service, Nexo.Discovery.Application.Requests.RequestService requests, HttpContext http, CancellationToken ct) =>
        {
            Private(http);
            var q = http.Request.Query;
            var kind = q.ContainsKey("kind") ? q["kind"].ToString() : http.User.FindFirst("mode")?.Value == "provider" ? "requests" : "providers";
            if (!SingleValues(q) || !Point(q, out var point) || point == null || !Int(q, "radiusKm", 10, out var radius) || radius is not (2 or 5 or 10 or 20) ||
                !Int(q, "limit", 60, out var limit) || limit < 1 || kind is not ("providers" or "requests") ||
                (q.ContainsKey("cat") && (kind != "requests" || !DirectoryCatalog.Current.Categories.Any(c => c.Slug == q["cat"])))) return Invalid(http);
            if (limit > 100) return NexoProblem.From(NexoCodes.RequestLimitExceeded, "El límite del mapa es 100.", http);
            if (kind == "requests")
            {
                if (http.User.Identity?.IsAuthenticated != true || !Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
                try
                {
                    return (await requests.MapAsync(id, point, radius, q.ContainsKey("cat") ? q["cat"].ToString() : null, limit, ct))
                    .Match<IResult>(body => Results.Json(body, ProviderService.Json), e => RequestEndpoints.Error(e, http));
                }
                catch (Exception e) when (RequestEndpoints.Unavailable(e)) { return Unavailable(http); }
            }
            try { return Results.Json(await service.MapAsync(point, radius, limit, ct), ProviderService.Json); }
            catch (DirectoryUnavailableException) { return Unavailable(http); }
        })).ProducesProblem(401).ProducesProblem(403).WithSummary("Puntos públicos de prestadores cercanos")
            .WithDescription("Query: lat/lng requeridos, radiusKm (2/5/10/20; por defecto 10), kind providers/requests, limit (1–100; por defecto 60). count es el total antes de limitar puntos. kind=requests exige sesión/perfil, usa categorías propias y radio acotado por su cobertura; puntos siempre difuminados.");
        return group;
    }
    public static bool TryQuery(IQueryCollection q, out DirectoryQuery query)
    {
        query = new();
        if (!SingleValues(q) || !Point(q, out var point) || !Int(q, "radiusKm", 10, out var radius) || !Int(q, "limit", 20, out var limit) ||
            !Bool(q, "open", out var open) || !Bool(q, "free", out var free) || !Bool(q, "prices", out var prices) ||
            !Bool(q, "urgent", out var urgent) || !Bool(q, "expanded", out var expanded)) return false;
        query = new(q["q"].ToString(), Optional(q, "cat"), point?.Lat, point?.Lng, radius, open, free, prices, urgent,
            Optional(q, "attention") ?? "all", Optional(q, "sort") ?? "relevance", expanded, q.ContainsKey("cursor") && q["cursor"].ToString().Length > 0 ? q["cursor"].ToString() : null, limit);
        return true;
    }
    private static string? Optional(IQueryCollection q, string key) => q.ContainsKey(key) ? q[key].ToString() : null;
    private static bool SingleValues(IQueryCollection q) => q.All(p => p.Value.Count == 1);
    private static bool Int(IQueryCollection q, string key, int fallback, out int value)
    {
        value = fallback;
        return !q.ContainsKey(key) || int.TryParse(q[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
    private static bool Bool(IQueryCollection q, string key, out bool value)
    {
        value = false;
        return !q.ContainsKey(key) || bool.TryParse(q[key], out value);
    }
    private static bool Point(IQueryCollection q, out ProviderPoint? point)
    {
        point = null;
        if (!q.ContainsKey("lat") && !q.ContainsKey("lng")) return true;
        if (!double.TryParse(q["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(q["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)) return false;
        point = new(lat, lng);
        return point.IsValid;
    }
    private static void Private(HttpContext http) => http.Response.Headers.CacheControl = "private, no-store";
    private static IResult Invalid(HttpContext http) => NexoProblem.From(NexoCodes.ValidationFailed, "Revisa los filtros y coordenadas.", http);
    private static IResult Unavailable(HttpContext http) => NexoProblem.From(NexoCodes.ServerDependencyUnavailable, "El directorio no está disponible para esta consulta. Intenta con una zona o categoría más específica.", http);
    private static RouteHandlerBuilder Public(RouteHandlerBuilder route) => route.AllowAnonymous().RequireRateLimiting(NexoRateLimits.Public)
        .Produces(200).ProducesProblem(400).ProducesProblem(429).ProducesProblem(503);
}
