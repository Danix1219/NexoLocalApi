using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Administracion (2 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class AdministracionEndpoints
{
    public static RouteGroupBuilder MapAdministracionEndpoints(this RouteGroupBuilder group)
    {
        // EP-080 — nv-24
        group.MapGet("/admin/reports", () => Cascaron.PendienteDeImplementar("EP-080", "nv-24"))
            .WithName("GetAdminReports")
            .WithSummary("GET /admin/reports");

        // EP-081 — nv-24
        group.MapPost("/admin/reports/{id}/decision", () => Cascaron.PendienteDeImplementar("EP-081", "nv-24"))
            .WithName("PostAdminReportsPorIdDecision")
            .WithSummary("POST /admin/reports/{id}/decision");

        return group;
    }
}
