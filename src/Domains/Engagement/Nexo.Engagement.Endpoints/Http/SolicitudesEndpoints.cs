using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Solicitudes (11 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class SolicitudesEndpoints
{
    public static RouteGroupBuilder MapSolicitudesEndpoints(this RouteGroupBuilder group)
    {
        // EP-047 — nv-09
        group.MapPost("/requests", () => Cascaron.PendienteDeImplementar("EP-047", "nv-09"))
            .WithName("PostRequests")
            .WithSummary("POST /requests");

        // EP-048 — nv-09
        group.MapGet("/requests", () => Cascaron.PendienteDeImplementar("EP-048", "nv-09"))
            .WithName("GetRequests")
            .WithSummary("GET /requests");

        // EP-049 — nv-09
        group.MapGet("/requests/{id}", () => Cascaron.PendienteDeImplementar("EP-049", "nv-09"))
            .WithName("GetRequestsPorId")
            .WithSummary("GET /requests/{id}");

        // EP-050 — nv-09
        group.MapPatch("/requests/{id}", () => Cascaron.PendienteDeImplementar("EP-050", "nv-09"))
            .WithName("PatchRequestsPorId")
            .WithSummary("PATCH /requests/{id}");

        // EP-051 — nv-09
        group.MapPost("/requests/{id}/close", () => Cascaron.PendienteDeImplementar("EP-051", "nv-09"))
            .WithName("PostRequestsPorIdClose")
            .WithSummary("POST /requests/{id}/close");

        // EP-052 — nv-09
        group.MapDelete("/requests/{id}", () => Cascaron.PendienteDeImplementar("EP-052", "nv-09"))
            .WithName("DeleteRequestsPorId")
            .WithSummary("DELETE /requests/{id}");

        // EP-053 — nv-20
        group.MapPut("/requests/{id}/interest", () => Cascaron.PendienteDeImplementar("EP-053", "nv-20"))
            .WithName("PutRequestsPorIdInterest")
            .WithSummary("PUT /requests/{id}/interest");

        // EP-054 — nv-20
        group.MapDelete("/requests/{id}/interest", () => Cascaron.PendienteDeImplementar("EP-054", "nv-20"))
            .WithName("DeleteRequestsPorIdInterest")
            .WithSummary("DELETE /requests/{id}/interest");

        // EP-055 — nv-20
        group.MapGet("/requests/{id}/interests", () => Cascaron.PendienteDeImplementar("EP-055", "nv-20"))
            .WithName("GetRequestsPorIdInterests")
            .WithSummary("GET /requests/{id}/interests");

        // EP-056 — nv-20
        group.MapPut("/requests/{id}/shortlist/{providerId}", () => Cascaron.PendienteDeImplementar("EP-056", "nv-20"))
            .WithName("PutRequestsPorIdShortlistPorProviderid")
            .WithSummary("PUT /requests/{id}/shortlist/{providerId}");

        // EP-057 — nv-20
        group.MapDelete("/requests/{id}/shortlist/{providerId}", () => Cascaron.PendienteDeImplementar("EP-057", "nv-20"))
            .WithName("DeleteRequestsPorIdShortlistPorProviderid")
            .WithSummary("DELETE /requests/{id}/shortlist/{providerId}");

        return group;
    }
}
