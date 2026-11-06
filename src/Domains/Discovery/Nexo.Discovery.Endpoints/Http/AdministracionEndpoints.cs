using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Administracion (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class AdministracionEndpoints
{
    public static RouteGroupBuilder MapAdministracionEndpoints(this RouteGroupBuilder group)
    {
        // EP-015 — nv-17
        group.MapPut("/admin/catalog/categories/{id}", () => Cascaron.PendienteDeImplementar("EP-015", "nv-17"))
            .WithName("PutAdminCatalogCategoriesPorId")
            .WithSummary("PUT /admin/catalog/categories/{id}");

        return group;
    }
}
