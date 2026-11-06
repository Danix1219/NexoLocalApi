using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Identity.Endpoints.Http;

/// <summary>
/// Cascaron de TiempoReal (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class TiempoRealEndpoints
{
    public static RouteGroupBuilder MapTiempoRealEndpoints(this RouteGroupBuilder group)
    {
        // EP-073 — nv-04
        group.MapPost("/realtime/ticket", () => Cascaron.PendienteDeImplementar("EP-073", "nv-04"))
            .WithName("PostRealtimeTicket")
            .WithSummary("POST /realtime/ticket");

        return group;
    }
}
