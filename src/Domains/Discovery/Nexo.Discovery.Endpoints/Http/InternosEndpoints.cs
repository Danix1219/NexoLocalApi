using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Internos (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class InternosEndpoints
{
    public static RouteGroupBuilder MapInternosEndpoints(this RouteGroupBuilder group)
    {
        // EP-100 — nv-25
        group.MapGet("/internal/providers/{id}/contact-policy", () => Cascaron.PendienteDeImplementar("EP-100", "nv-25"))
            .WithName("GetInternalProvidersPorIdContactPolicy")
            .WithSummary("GET /internal/providers/{id}/contact-policy");

        return group;
    }
}
