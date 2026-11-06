using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Busqueda (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class BusquedaEndpoints
{
    public static RouteGroupBuilder MapBusquedaEndpoints(this RouteGroupBuilder group)
    {
        // EP-026 — nv-07
        group.MapGet("/search/suggestions", () => Cascaron.PendienteDeImplementar("EP-026", "nv-07"))
            .WithName("GetSearchSuggestions")
            .WithSummary("GET /search/suggestions");

        return group;
    }
}
