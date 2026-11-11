using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Requests;

namespace Nexo.Discovery.Endpoints;

public static class RequestEndpoints
{
    public static RouteGroupBuilder MapRequestEndpoints(this RouteGroupBuilder group)
    {
        Document(group.MapPost("/requests", async (JsonElement body, HttpContext http, RequestService service, CancellationToken ct) =>
        {
            Private(http);
            try
            {
                return (await service.PublishAsync(User(http), http.Request.Headers["Idempotency-Key"], body, ct)).Match<IResult>(request =>
                {
                    http.Response.Headers.Location = $"/v1/discovery/requests/{request.RequestId}";
                    return Results.Json(RequestService.Created(request), ProviderService.Json, statusCode: 201);
                }, e => Error(e, http));
            }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        })).Produces(201).WithSummary("Publicar una solicitud moderada")
            .WithDescription("Idempotency-Key UUID obligatorio. Título 5–90, descripción 10–600, urgency today/week/noRush, contact chat/chatCall y location real. Solo sale punto difuminado. Cuota 10/día UTC. chatCall exige correo confirmado y móvil declarado; hasta seis imágenes aprobadas.");
        Document(group.MapPatch("/requests/{id}", async (string id, JsonElement body, HttpContext http, RequestService service, CancellationToken ct) =>
        {
            Private(http); if (!Guid.TryParse(id, out var requestId)) return Invalid(http);
            try { return (await service.EditAsync(User(http), requestId, body, ct)).Match<IResult>(
                value => Results.Json(value, ProviderService.Json), e => Error(e, http)); }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        })).Produces(200).WithSummary("Editar una solicitud propia abierta");
        Document(group.MapGet("/requests/mine", async (HttpContext http, RequestService service, CancellationToken ct) =>
        {
            Private(http); var q = http.Request.Query;
            if (!Singles(q) || !Int(q, "limit", 20, out var limit)) return Invalid(http);
            try { return (await service.MineAsync(User(http), q.ContainsKey("status") ? q["status"].ToString() : "all", q["cursor"], limit, ct))
                .Match<IResult>(body => Results.Json(body, ProviderService.Json), e => Error(e, http)); }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        })).Produces(200).WithSummary("Mis solicitudes")
            .WithDescription("Query: status all/open/assigned/closed, cursor hasta 64, limit 1–50 (20). Solo propias; resumen real de Engagement por contrato, sin teléfono ni punto exacto.");
        Document(group.MapGet("/requests/opportunities", async (HttpContext http, RequestService service, CancellationToken ct) =>
        {
            Private(http); var q = http.Request.Query;
            if (!Singles(q) || !Int(q, "limit", 20, out var limit) || !Bool(q, "today", false, out var today) ||
                !Bool(q, "inRadius", true, out var inRadius) || !Bool(q, "myServiceOnly", true, out var myService)) return Invalid(http);
            var query = new OpportunityQuery(q.ContainsKey("cat") ? q["cat"].ToString() : null, today, inRadius, myService, q["q"].ToString(), q["cursor"], limit);
            try { return (await service.OpportunitiesAsync(User(http), query, ct)).Match<IResult>(body => Results.Json(body, ProviderService.Json), e => Error(e, http)); }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        })).Produces(200).WithSummary("Oportunidades para mi perfil de prestador")
            .WithDescription("Query: cat, today=false, inRadius=true, myServiceOnly=true, q hasta 80, cursor hasta 64, limit 1–50. Categorías/cobertura reales del perfil; local radio 0 usa 10 km. Nunca propias, cerradas o asignadas; solo iniciales del cliente.");
        Document(group.MapPost("/requests/{id}/close", async (string id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement? body,
            HttpContext http, RequestService service, CancellationToken ct) =>
        {
            Private(http); if (!Guid.TryParse(id, out var requestId)) return Invalid(http);
            try
            {
                return (await service.CloseAsync(User(http), requestId, body, ct)).Match<IResult>(result => result.AlreadyClosed
                    ? Results.Problem(statusCode: 409, title: "La solicitud ya está cerrada", instance: http.Request.Path,
                        extensions: new Dictionary<string, object?> { ["code"] = NexoCodes.RequestAlreadyClosed, ["closedAt"] = result.Request.ClosedAt, ["traceId"] = http.TraceIdentifier })
                    : Results.Json(new { requestId = result.Request.RequestId, status = "closed", result.Request.ClosedAt, result.Request.InterestCount,
                        canReview = false, reviewProviderId = (Guid?)null }, ProviderService.Json), e => Error(e, http));
            }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        })).Produces(200).WithSummary("Cerrar una solicitud propia")
            .WithDescription("Cuerpo opcional: outcome solvedHere/solvedElsewhere/noLongerNeeded, pickedProviderId solo con solvedHere (declaración privada, no concede reseña). Cierre repetido: 409 con fecha original. Propiedad y estado se validan atómicamente.");
        return group;
    }
    public static IResult Error(NexoError error, HttpContext http) => error.Code == NexoCodes.ProviderProfileRequired
        ? Results.Problem(statusCode: 403, title: "Necesitas un perfil de prestador activo", instance: http.Request.Path,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = http.TraceIdentifier }) : NexoProblem.From(error, http);
    public static bool Unavailable(Exception e) => e is RequestUnavailableException or DirectoryUnavailableException || DependencyErrors.IsUnavailable(e);
    private static IResult Dependency(HttpContext http) => NexoProblem.From(NexoCodes.ServerDependencyUnavailable, null, http);
    private static IResult Invalid(HttpContext http) => NexoProblem.From(NexoCodes.ValidationFailed, "Revisa los filtros o el identificador.", http);
    private static Guid User(HttpContext http) => Guid.Parse(http.User.FindFirst("sub")!.Value);
    private static void Private(HttpContext http) => http.Response.Headers.CacheControl = "private, no-store";
    private static bool Singles(IQueryCollection q) => q.All(p => p.Value.Count == 1);
    private static bool Int(IQueryCollection q, string key, int fallback, out int value) { value = fallback; return !q.ContainsKey(key) || int.TryParse(q[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out value); }
    private static bool Bool(IQueryCollection q, string key, bool fallback, out bool value) { value = fallback; return !q.ContainsKey(key) || bool.TryParse(q[key], out value); }
    private static RouteHandlerBuilder Document(RouteHandlerBuilder route) => route.RequireAuthorization().RequireRateLimiting(NexoRateLimits.User)
        .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(429).ProducesProblem(503);
}
