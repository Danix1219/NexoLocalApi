using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Catalogo (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class CatalogoEndpoints
{
    public static RouteGroupBuilder MapCatalogoEndpoints(this RouteGroupBuilder group)
    {
        // EP-014 — nv-17
        group.MapGet("/catalog/categories", () => Cascaron.PendienteDeImplementar("EP-014", "nv-17"))
            .WithName("GetCatalogCategories")
            .WithSummary("GET /catalog/categories");

        return group;
    }
}
