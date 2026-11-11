using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Endpoints;

public static class ProviderEndpoints
{
    public static RouteGroupBuilder MapProviderEndpoints(this RouteGroupBuilder group)
    {
        Document(group.MapPost("/providers", async (JsonElement body, HttpContext http, ProviderService service, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            var result = await service.CreateAsync(User(http), http.Request.Headers["Idempotency-Key"].FirstOrDefault(), body, ct);
            return result.Match<IResult>(p => Results.Created($"/v1/discovery/providers/{p.ProviderId}", new
            {
                p.ProviderId,
                p.Kind,
                p.Name,
                p.Categories,
                inferredCategories = p.Kind == "business",
                p.Attention,
                p.PublicZone,
                p.RadiusKm,
                p.Completeness,
                policyVersion = ProviderProfile.CompletenessPolicyVersion,
                isNew = true,
                emailVerified = true,
                nextOptionalSteps = new[] { "hours", "prices", "terms", "portfolio" }
            }), error => NexoProblem.From(error, http));
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(201);
        Document(group.MapGet("/providers/me", async (HttpContext http, ProviderService service, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            var result = await service.OwnAsync(User(http), ct);
            return result.Match<IResult>(body => Results.Json(body, ProviderService.Json), error => NexoProblem.From(error, http));
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(200);
        Document(group.MapPatch("/providers/me/{section}", async (string section, JsonElement body, HttpContext http, ProviderService service, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            var result = await service.PatchAsync(User(http), section, body, ct);
            return result.Match<IResult>(value => Results.Json(value, ProviderService.Json), error => NexoProblem.From(error, http));
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(200);
        Document(group.MapGet("/providers/compare", async (string? ids, string? lat, string? lng, HttpContext http, ProviderService service, CancellationToken ct) =>
        {
            var selected = (ids ?? "").Split(',');
            if (selected.Length != 2 || selected[0] == selected[1]) return NexoProblem.From(NexoCodes.CompareNeedTwoIds, "Elige dos prestadores distintos.", http);
            if (!Guid.TryParse(selected[0], out var first) || !Guid.TryParse(selected[1], out var second) || first == Guid.Empty || second == Guid.Empty) return Invalid(http);
            if (first == second) return NexoProblem.From(NexoCodes.CompareNeedTwoIds, "Elige dos prestadores distintos.", http);
            if (!Point(http, out var point)) return Invalid(http);
            var result = await service.VisibleAsync([first, second], point, ct);
            http.Response.Headers.CacheControl = "private, no-store";
            return result.Match<IResult>(p => Results.Json(ProviderComparison.Build(p), ProviderService.Json), error => NexoProblem.From(error, http));
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(200);
        Document(group.MapGet("/providers/{id}", async (string id, string? lat, string? lng, HttpContext http, ProviderService service, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(id, out var parsed) || parsed == Guid.Empty || !Point(http, out var point)) return Invalid(http);
            var result = await service.PublicAsync(parsed, point, ct);
            return result.Match<IResult>(body =>
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(body, ProviderService.Json);
                var etag = '"' + Convert.ToHexString(SHA256.HashData(bytes)) + '"';
                http.Response.Headers.ETag = etag;
                if (http.Request.GetTypedHeaders().IfNoneMatch?.Any(tag => tag.Tag == etag || tag.Tag == "*") == true) return Results.StatusCode(304);
                return Results.Bytes(bytes, "application/json");
            }, error => NexoProblem.From(error, http));
        }).AllowAnonymous().RequireRateLimiting(NexoRateLimits.Public)).Produces(200).Produces(304);
        return group;
    }
    private static Guid User(HttpContext http) => Guid.Parse(http.User.FindFirst("sub")!.Value);
    private static IResult Invalid(HttpContext http) => NexoProblem.From(NexoCodes.ValidationFailed, "Revisa los identificadores y coordenadas.", http);
    private static bool Point(HttpContext http, out ProviderPoint? point)
    {
        point = null;
        var query = http.Request.Query;
        if (!query.ContainsKey("lat") && !query.ContainsKey("lng")) return true;
        if (!double.TryParse(query["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(query["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)) return false;
        point = new(lat, lng);
        return point.IsValid;
    }
    private static RouteHandlerBuilder Document(RouteHandlerBuilder route) => route.ProducesProblem(400).ProducesProblem(401)
        .ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(429).ProducesProblem(503);
}
