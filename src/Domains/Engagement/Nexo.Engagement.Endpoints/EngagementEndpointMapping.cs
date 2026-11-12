using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Engagement.Application.Interests;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Engagement.Endpoints;

public static class EngagementEndpointMapping
{
    /// <summary>Superficie HTTP de Engagement; el host la monta sin conocer sus rutas internas.</summary>
    public static IEndpointRouteBuilder MapEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/engagement").WithTags("Engagement");
        group.MapPost("/requests/{id:guid}/interest", async (Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InterestNoteBody? body,
            InterestService service, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                var result = await service.ExpressAsync(User(http), id, body?.Note, ct);
                return result.Match(value => Results.Json(value.Body, statusCode: value.StatusCode), error => NexoProblem.From(error, http));
            }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(201).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Expresar interés en una solicitud visible");

        group.MapDelete("/requests/{id:guid}/interest", async (Guid id, InterestService service, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                var result = await service.WithdrawAsync(User(http), id, ct);
                return result.Match(value => Results.Ok(value), error => NexoProblem.From(error, http));
            }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Retirar el interés propio");

        group.MapGet("/requests/{id:guid}/interested", async (Guid id, HttpRequest request, InterestService service, HttpContext http, CancellationToken ct) =>
        {
            if (request.Query.Any(q => q.Value.Count != 1) || !Int(request.Query, "limit", 20, out var limit)) return Invalid(http);
            var sort = request.Query.TryGetValue("sort", out var value) ? value.ToString() : "recent";
            var cursor = request.Query.TryGetValue("cursor", out var cursorValue) ? cursorValue.ToString() : null;
            http.Response.Headers.CacheControl = "private, no-store";
            try
            {
                var result = await service.InterestedAsync(User(http), id, sort, cursor, limit, ct);
                return result.Match(value => Results.Ok(value), error => NexoProblem.From(error, http));
            }
            catch (Exception e) when (Unavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Consultar los prestadores interesados en una solicitud propia");

        group.MapPost("/orders", async (OrderService.CreateOrderBody? body, OrderService service, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            try
            {
                var result = await service.CreateAsync(User(http), null, http.Request.Headers["Idempotency-Key"].FirstOrDefault(), body, ct);
                return result.Match(value => Results.Json(value.Body, statusCode: value.StatusCode), error => NexoProblem.From(error, http));
            }
            catch (Exception e) when (e is OrderUnavailableException || DependencyErrors.IsUnavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(201).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Crear un pedido de servicio");

        group.MapGet("/orders", async (HttpRequest request, OrderService service, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            if (request.Query.Any(q => q.Value.Count != 1) || !Int(request.Query, "limit", 20, out var limit)) return Invalid(http);
            var role = request.Query.TryGetValue("role", out var rv) ? rv.ToString() : "client";
            var status = request.Query.TryGetValue("status", out var sv) ? sv.ToString() : "active";
            var cursor = request.Query.TryGetValue("cursor", out var cv) ? cv.ToString() : null;
            try { var result = await service.ListAsync(User(http), role, status, cursor, limit, ct); return result.Match(Results.Ok, error => NexoProblem.From(error, http)); }
            catch (Exception e) when (e is OrderUnavailableException || DependencyErrors.IsUnavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Listar pedidos propios");

        group.MapGet("/orders/{id:guid}", async (Guid id, OrderService service, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            try { var result = await service.DetailAsync(User(http), id, ct); return result.Match(Results.Ok, error => NexoProblem.From(error, http)); }
            catch (Exception e) when (e is OrderUnavailableException || DependencyErrors.IsUnavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(401).ProducesProblem(404).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Consultar el detalle privado de un pedido");

        MapTransition(group, "/orders/{id:guid}/accept", "accepted");
        MapTransition(group, "/orders/{id:guid}/reject", "rejected");
        MapTransition(group, "/orders/{id:guid}/cancel", "cancelled");
        MapTransition(group, "/orders/{id:guid}/resolve", "resolved");

        group.MapPut("/orders/{id:guid}/address", async (Guid id, OrderService.ChangeAddressBody? body, OrderService service, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            try { var result = await service.ChangeAddressAsync(User(http), id, ExpectedVersion(http), body, ct); return result.Match(Results.Ok, error => NexoProblem.From(error, http)); }
            catch (Exception e) when (e is OrderUnavailableException || DependencyErrors.IsUnavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(409).ProducesProblem(412).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Actualizar el domicilio privado del pedido");

        group.MapGet("/internal/orders/{id:guid}/review-eligibility", async (Guid id, IOrderStore store, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            try
            {
                var order = await store.FindAsync(id, User(http), ct);
                return order == null ? NexoProblem.From(NexoCodes.ResourceNotFound, null, http) : Results.Ok(new { participant = true, resolved = order.Status == "resolved", providerId = order.ProviderId });
            }
            catch (Exception e) when (e is OrderUnavailableException || DependencyErrors.IsUnavailable(e)) { return Dependency(http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .WithSummary("Comprobar la elegibilidad interna de reseña sin exponer el domicilio");
        return app;

        void MapTransition(RouteGroupBuilder routes, string pattern, string target)
        {
            routes.MapPost(pattern, async (Guid id, OrderService.TransitionBody? body, OrderService service, HttpContext http, CancellationToken ct) =>
            {
                http.Response.Headers.CacheControl = "private, no-store";
                try { var result = await service.TransitionAsync(User(http), null, id, target, ExpectedVersion(http), body, ct); return result.Match(Results.Ok, error => NexoProblem.From(error, http)); }
                catch (Exception e) when (e is OrderUnavailableException || DependencyErrors.IsUnavailable(e)) { return Dependency(http); }
            }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(412).ProducesProblem(429).ProducesProblem(503)
                .WithSummary($"Cambiar pedido a {target}");
        }
    }

    private static bool Unavailable(Exception e) => e is InterestUnavailableException || DependencyErrors.IsUnavailable(e);
    private static IResult Dependency(HttpContext http) => NexoProblem.From(NexoCodes.ServerDependencyUnavailable, null, http);
    private static IResult Invalid(HttpContext http) => NexoProblem.From(NexoCodes.ValidationFailed, "Revisa los parámetros de consulta.", http);
    private static Guid User(HttpContext http) => Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
    private static int? ExpectedVersion(HttpContext http)
    {
        var raw = http.Request.Headers.IfMatch.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return int.TryParse(raw.Trim().Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : -1;
    }
    private static bool Int(IQueryCollection query, string key, int fallback, out int value) { value = fallback; return !query.ContainsKey(key) || int.TryParse(query[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out value); }
    public sealed record InterestNoteBody(string? Note = null);
}
