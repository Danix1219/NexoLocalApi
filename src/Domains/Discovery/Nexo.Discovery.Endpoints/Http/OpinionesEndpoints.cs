using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Opiniones (4 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class OpinionesEndpoints
{
    public static RouteGroupBuilder MapOpinionesEndpoints(this RouteGroupBuilder group)
    {
        // EP-041 — nv-19
        group.MapPost("/reviews", () => Cascaron.PendienteDeImplementar("EP-041", "nv-19"))
            .WithName("PostReviews")
            .WithSummary("POST /reviews");

        // EP-043 — nv-19
        group.MapPatch("/reviews/{id}", () => Cascaron.PendienteDeImplementar("EP-043", "nv-19"))
            .WithName("PatchReviewsPorId")
            .WithSummary("PATCH /reviews/{id}");

        // EP-044 — nv-19
        group.MapDelete("/reviews/{id}", () => Cascaron.PendienteDeImplementar("EP-044", "nv-19"))
            .WithName("DeleteReviewsPorId")
            .WithSummary("DELETE /reviews/{id}");

        // EP-095 — nv-19
        group.MapGet("/reviews/{id}", () => Cascaron.PendienteDeImplementar("EP-095", "nv-19"))
            .WithName("GetReviewsPorId")
            .WithSummary("GET /reviews/{id}");

        return group;
    }
}
