using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Reportes (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class ReportesEndpoints
{
    public static RouteGroupBuilder MapReportesEndpoints(this RouteGroupBuilder group)
    {
        // EP-078 — nv-05
        group.MapPost("/reports", () => Cascaron.PendienteDeImplementar("EP-078", "nv-05"))
            .WithName("PostReports")
            .WithSummary("POST /reports");

        return group;
    }
}
