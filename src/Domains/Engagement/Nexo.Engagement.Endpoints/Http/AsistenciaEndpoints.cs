using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Asistencia (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class AsistenciaEndpoints
{
    public static RouteGroupBuilder MapAsistenciaEndpoints(this RouteGroupBuilder group)
    {
        // EP-082 — nv-13
        group.MapPost("/assist/draft", () => Cascaron.PendienteDeImplementar("EP-082", "nv-13"))
            .WithName("PostAssistDraft")
            .WithSummary("POST /assist/draft");

        return group;
    }
}
