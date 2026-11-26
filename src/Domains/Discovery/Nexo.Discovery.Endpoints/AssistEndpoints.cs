using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Discovery.Application.Assist;

namespace Nexo.Discovery.Endpoints;

/// <summary>EP-082 under `/v1/assist`, the path the app already calls (spec 049).</summary>
public static class AssistEndpoints
{
    public static IEndpointRouteBuilder MapAssistEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/assist/draft", async (AssistService.DraftBody? body, AssistService service, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            var account = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
            try { return (await service.DraftAsync(account, body, ct)).Match(Results.Ok, e => NexoProblem.From(e, http)); }
            catch (Exception e) when (DependencyErrors.IsUnavailable(e)) { return NexoProblem.From(NexoCodes.AssistUnavailable, null, http); }
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).WithTags("Assist")
            .Produces(200).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(429).ProducesProblem(503)
            .WithSummary("Borrador editable con IA generativa; nunca publica ni guarda el texto");
        return app;
    }
}
