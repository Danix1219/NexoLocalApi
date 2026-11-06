using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Guardados (3 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class GuardadosEndpoints
{
    public static RouteGroupBuilder MapGuardadosEndpoints(this RouteGroupBuilder group)
    {
        // EP-038 — nv-18
        group.MapGet("/me/saved", () => Cascaron.PendienteDeImplementar("EP-038", "nv-18"))
            .WithName("GetMeSaved")
            .WithSummary("GET /me/saved");

        // EP-039 — nv-18
        group.MapPut("/me/saved/{type}/{id}", () => Cascaron.PendienteDeImplementar("EP-039", "nv-18"))
            .WithName("PutMeSavedPorTypePorId")
            .WithSummary("PUT /me/saved/{type}/{id}");

        // EP-040 — nv-18
        group.MapDelete("/me/saved/{type}/{id}", () => Cascaron.PendienteDeImplementar("EP-040", "nv-18"))
            .WithName("DeleteMeSavedPorTypePorId")
            .WithSummary("DELETE /me/saved/{type}/{id}");

        return group;
    }
}
