using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Discovery.Application.Saved;

namespace Nexo.Discovery.Endpoints;

public static class SavedEndpoints
{
    public static RouteGroupBuilder MapSavedEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/saved", async (HttpContext http, SavedService service, CancellationToken ct) =>
        {
            Private(http);
            var q = http.Request.Query;
            if (q.Any(p => p.Value.Count != 1) || !Int(q, "limit", 20, out var limit)) return Invalid(http);
            var kind = q.TryGetValue("kind", out var kindValue) ? kindValue.ToString() : null;
            var cursor = q.TryGetValue("cursor", out var cursorValue) ? cursorValue.ToString() : null;
            try
            {
                var result = await service.ListAsync(User(http), kind, cursor, limit, ct);
                return result.Match<IResult>(Results.Ok, error => NexoProblem.From(error, http));
            }
            catch (SavedUnavailableException) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Listar mis elementos guardados");

        MapMutation(group, true);
        MapMutation(group, false);
        return group;
    }

    private static void MapMutation(RouteGroupBuilder group, bool desired)
    {
        async Task<IResult> Handle(string kind, Guid id, HttpContext http, SavedService service, CancellationToken ct)
        {
            Private(http);
            try
            {
                var result = await service.SetAsync(User(http), kind, id, desired, ct);
                return result.Match<IResult>(Results.Ok, error => NexoProblem.From(error, http));
            }
            catch (SavedUnavailableException) { return Dependency(http); }
        }
        var route = desired ? group.MapPost("/saved/{kind}/{id:guid}", Handle) : group.MapDelete("/saved/{kind}/{id:guid}", Handle);
        route.RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(429).ProducesProblem(503)
            .WithSummary(desired ? "Guardar un perfil o publicación" : "Quitar un elemento de guardados");
    }

    private static void Private(HttpContext http) => http.Response.Headers.CacheControl = "private, no-store";
    private static Guid User(HttpContext http) => Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
    private static bool Int(IQueryCollection query, string key, int fallback, out int value)
    {
        value = fallback;
        return !query.ContainsKey(key) || int.TryParse(query[key], NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
    private static IResult Invalid(HttpContext http) => NexoProblem.From(NexoCodes.ValidationFailed, "Revisa los parámetros de consulta.", http);
    private static IResult Dependency(HttpContext http) => NexoProblem.From(NexoCodes.ServerDependencyUnavailable, null, http);
}
