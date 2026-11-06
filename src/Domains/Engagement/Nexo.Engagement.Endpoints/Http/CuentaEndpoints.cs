using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Cuenta (2 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class CuentaEndpoints
{
    public static RouteGroupBuilder MapCuentaEndpoints(this RouteGroupBuilder group)
    {
        // EP-067 — nv-11
        group.MapGet("/me/conversations", () => Cascaron.PendienteDeImplementar("EP-067", "nv-11"))
            .WithName("GetMeConversations")
            .WithSummary("GET /me/conversations");

        // EP-079 — nv-05
        group.MapGet("/me/reports", () => Cascaron.PendienteDeImplementar("EP-079", "nv-05"))
            .WithName("GetMeReports")
            .WithSummary("GET /me/reports");

        return group;
    }
}
