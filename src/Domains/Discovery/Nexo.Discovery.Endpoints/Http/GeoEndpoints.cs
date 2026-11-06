using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Geo (2 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class GeoEndpoints
{
    public static RouteGroupBuilder MapGeoEndpoints(this RouteGroupBuilder group)
    {
        // EP-028 — nv-07
        group.MapGet("/geo/search", () => Cascaron.PendienteDeImplementar("EP-028", "nv-07"))
            .WithName("GetGeoSearch")
            .WithSummary("GET /geo/search");

        // EP-029 — nv-07
        group.MapGet("/geo/reverse", () => Cascaron.PendienteDeImplementar("EP-029", "nv-07"))
            .WithName("GetGeoReverse")
            .WithSummary("GET /geo/reverse");

        return group;
    }
}
