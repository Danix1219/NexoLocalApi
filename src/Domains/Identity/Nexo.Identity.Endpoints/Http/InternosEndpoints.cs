using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Identity.Endpoints.Http;

/// <summary>
/// Cascaron de Internos (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class InternosEndpoints
{
    public static RouteGroupBuilder MapInternosEndpoints(this RouteGroupBuilder group)
    {
        // EP-101 — nv-04
        group.MapPost("/internal/accounts/visibility-batch", () => Cascaron.PendienteDeImplementar("EP-101", "nv-04"))
            .WithName("PostInternalAccountsVisibilityBatch")
            .WithSummary("POST /internal/accounts/visibility-batch");

        return group;
    }
}
