using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Catalogo (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class CatalogoEndpoints
{
    public static RouteGroupBuilder MapCatalogoEndpoints(this RouteGroupBuilder group)
    {
        // EP-099 — nv-05
        group.MapGet("/catalog/report-reasons", () => Cascaron.PendienteDeImplementar("EP-099", "nv-05"))
            .WithName("GetCatalogReportReasons")
            .WithSummary("GET /catalog/report-reasons");

        return group;
    }
}
