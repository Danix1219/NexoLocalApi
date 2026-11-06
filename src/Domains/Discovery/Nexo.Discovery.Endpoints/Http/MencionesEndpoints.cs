using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Menciones (1 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class MencionesEndpoints
{
    public static RouteGroupBuilder MapMencionesEndpoints(this RouteGroupBuilder group)
    {
        // EP-093 — nv-19
        group.MapGet("/mentions/suggestions", () => Cascaron.PendienteDeImplementar("EP-093", "nv-19"))
            .WithName("GetMentionsSuggestions")
            .WithSummary("GET /mentions/suggestions");

        return group;
    }
}
