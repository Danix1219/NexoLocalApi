using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Works;

namespace Nexo.Discovery.Endpoints;

public static class WorkEndpoints
{
    public static RouteGroupBuilder MapWorkEndpoints(this RouteGroupBuilder group)
    {
        Document(group.MapPost("/works", async (JsonElement body, HttpContext http, WorkService service, CancellationToken ct) =>
        {
            NoStore(http);
            try
            {
                var result = await service.PublishAsync(User(http), http.Request.Headers["Idempotency-Key"], body, ct);
                return result.Match<IResult>(work =>
                {
                    http.Response.Headers.Location = $"/v1/discovery/works/{work.WorkId}";
                    return Results.Json(WorkService.Created(work), ProviderService.Json, statusCode: 201);
                }, error => Error(error, http));
            }
            catch (Exception error) when (error is WorkUnavailableException || DependencyErrors.IsUnavailable(error)) { return Unavailable(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(201).WithSummary("Publicar un trabajo moderado")
            .WithDescription("Idempotency-Key UUID obligatorio. Cuota durable: 30 publicaciones por cuenta/día UTC. Categoría del perfil, título 5–90, texto 20–1000 y punto real requerido (solo se persiste difuminado). Hasta seis imágenes aprobadas.");
        Document(group.MapPatch("/works/{id}", async (string id, JsonElement body, HttpContext http, WorkService service, CancellationToken ct) =>
        {
            NoStore(http); if (!Guid.TryParse(id, out var work)) return Invalid(http);
            try { return (await service.EditAsync(User(http), work, body, ct)).Match<IResult>(
                value => Results.Json(value, ProviderService.Json), error => Error(error, http)); }
            catch (Exception error) when (error is WorkUnavailableException || DependencyErrors.IsUnavailable(error)) { return Unavailable(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(200).WithSummary("Editar trabajo propio moderado");
        Document(group.MapGet("/providers/{id}/works", async (string id, HttpContext http, WorkService service, CancellationToken ct) =>
        {
            NoStore(http);
            var query = http.Request.Query;
            if (!Guid.TryParse(id, out var provider) || query.Any(p => p.Value.Count != 1)) return Invalid(http);
            var limit = 12;
            if (query.ContainsKey("limit") && !int.TryParse(query["limit"], NumberStyles.Integer, CultureInfo.InvariantCulture, out limit)) return Invalid(http);
            Guid? viewer = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var account) ? account : null;
            try
            {
                var result = await service.ListAsync(provider, viewer, query["cursor"], limit, ct);
                return result.Match<IResult>(body => Results.Json(body, ProviderService.Json), error => Error(error, http));
            }
            catch (Exception error) when (error is WorkUnavailableException || DependencyErrors.IsUnavailable(error)) { return Unavailable(http); }
        }).AllowAnonymous().RequireRateLimiting(NexoRateLimits.Public)).Produces(200).WithSummary("Trabajos publicados de un prestador")
            .WithDescription("Query: cursor (hasta 64), limit 1–50 (por defecto 12). Paginación por UUID v7; saved/recommended solo con sesión, consultados por usuario.");
        Document(group.MapDelete("/works/{id}", async (string id, HttpContext http, WorkService service, CancellationToken ct) =>
        {
            NoStore(http);
            if (!Guid.TryParse(id, out var work)) return Invalid(http);
            try { return (await service.DeleteAsync(User(http), work, ct)).Match<IResult>(_ => Results.NoContent(), error => Error(error, http)); }
            catch (Exception error) when (error is WorkUnavailableException || DependencyErrors.IsUnavailable(error)) { return Unavailable(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(204).WithSummary("Eliminar un trabajo propio");
        MapRecommendation(group, true);
        MapRecommendation(group, false);
        return group;
    }
    private static void MapRecommendation(RouteGroupBuilder group, bool desired)
    {
        async Task<IResult> Handle(string id, HttpContext http, WorkService service, CancellationToken ct)
        {
            NoStore(http);
            if (!Guid.TryParse(id, out var work)) return Invalid(http);
            try { return (await service.RecommendAsync(User(http), work, desired, ct)).Match<IResult>(body => Results.Ok(body), error => Error(error, http)); }
            catch (Exception error) when (error is WorkUnavailableException || DependencyErrors.IsUnavailable(error)) { return Unavailable(http); }
        }
        var route = desired ? group.MapPost("/works/{id}/recommend", Handle) : group.MapDelete("/works/{id}/recommend", Handle);
        Document(route.RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)).Produces(200)
            .WithSummary(desired ? "Recomendar un trabajo" : "Retirar mi recomendación");
    }
    private static IResult Error(NexoError error, HttpContext http) => error.Code == NexoCodes.ProviderProfileRequired
        ? Results.Problem(statusCode: 403, title: "Necesitas un perfil de prestador", instance: http.Request.Path,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = http.TraceIdentifier })
        : NexoProblem.From(error, http);
    private static Guid User(HttpContext http) => Guid.Parse(http.User.FindFirst("sub")!.Value);
    private static void NoStore(HttpContext http) => http.Response.Headers.CacheControl = "private, no-store";
    private static IResult Invalid(HttpContext http) => NexoProblem.From(NexoCodes.ValidationFailed, "Revisa el identificador o la página solicitada.", http);
    private static IResult Unavailable(HttpContext http) => NexoProblem.From(NexoCodes.ServerDependencyUnavailable, "No pudimos completar la operación del trabajo.", http);
    private static RouteHandlerBuilder Document(RouteHandlerBuilder route) => route.ProducesProblem(400).ProducesProblem(401)
        .ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(429).ProducesProblem(503);
}
