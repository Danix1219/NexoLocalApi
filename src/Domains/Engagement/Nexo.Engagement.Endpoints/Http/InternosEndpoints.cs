using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Internos (2 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class InternosEndpoints
{
    public static RouteGroupBuilder MapInternosEndpoints(this RouteGroupBuilder group)
    {
        // EP-087 — nv-05
        group.MapPost("/internal/moderation/check", () => Cascaron.PendienteDeImplementar("EP-087", "nv-05"))
            .WithName("PostInternalModerationCheck")
            .WithSummary("POST /internal/moderation/check");

        // EP-088 — nv-10
        group.MapGet("/internal/orders/{id}/review-eligibility", () => Cascaron.PendienteDeImplementar("EP-088", "nv-10"))
            .WithName("GetInternalOrdersPorIdReviewEligibility")
            .WithSummary("GET /internal/orders/{id}/review-eligibility");

        return group;
    }
}
