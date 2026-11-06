using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Medios (3 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class MediosEndpoints
{
    public static RouteGroupBuilder MapMediosEndpoints(this RouteGroupBuilder group)
    {
        // EP-075 — nv-05
        group.MapPost("/media", () => Cascaron.PendienteDeImplementar("EP-075", "nv-05"))
            .WithName("PostMedia")
            .WithSummary("POST /media");

        // EP-076 — nv-05
        group.MapGet("/media/{id}/access", () => Cascaron.PendienteDeImplementar("EP-076", "nv-05"))
            .WithName("GetMediaPorIdAccess")
            .WithSummary("GET /media/{id}/access");

        // EP-077 — nv-05
        group.MapDelete("/media/{id}", () => Cascaron.PendienteDeImplementar("EP-077", "nv-05"))
            .WithName("DeleteMediaPorId")
            .WithSummary("DELETE /media/{id}");

        return group;
    }
}
