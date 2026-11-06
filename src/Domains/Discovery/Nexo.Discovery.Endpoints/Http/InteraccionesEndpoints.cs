using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Interacciones (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class InteraccionesEndpoints
{
    public static RouteGroupBuilder MapInteraccionesEndpoints(this RouteGroupBuilder group)
    {
        // EP-045 — nv-18
        group.MapPost("/interactions", () => Cascaron.PendienteDeImplementar("EP-045", "nv-18"))
            .WithName("PostInteractions")
            .WithSummary("POST /interactions");

        return group;
    }
}
