using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Reacciones (2 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class ReaccionesEndpoints
{
    public static RouteGroupBuilder MapReaccionesEndpoints(this RouteGroupBuilder group)
    {
        // EP-036 — nv-18
        group.MapPut("/reactions/{type}/{id}", () => Cascaron.PendienteDeImplementar("EP-036", "nv-18"))
            .WithName("PutReactionsPorTypePorId")
            .WithSummary("PUT /reactions/{type}/{id}");

        // EP-037 — nv-18
        group.MapDelete("/reactions/{type}/{id}", () => Cascaron.PendienteDeImplementar("EP-037", "nv-18"))
            .WithName("DeleteReactionsPorTypePorId")
            .WithSummary("DELETE /reactions/{type}/{id}");

        return group;
    }
}
