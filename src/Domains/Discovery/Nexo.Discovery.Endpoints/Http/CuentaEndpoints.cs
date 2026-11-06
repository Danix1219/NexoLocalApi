using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Cuenta (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class CuentaEndpoints
{
    public static RouteGroupBuilder MapCuentaEndpoints(this RouteGroupBuilder group)
    {
        // EP-094 — nv-19
        group.MapGet("/me/reviews", () => Cascaron.PendienteDeImplementar("EP-094", "nv-19"))
            .WithName("GetMeReviews")
            .WithSummary("GET /me/reviews");

        return group;
    }
}
