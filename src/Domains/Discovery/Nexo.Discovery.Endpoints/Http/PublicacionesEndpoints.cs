using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Publicaciones (5 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class PublicacionesEndpoints
{
    public static RouteGroupBuilder MapPublicacionesEndpoints(this RouteGroupBuilder group)
    {
        // EP-031 — nv-08
        group.MapPost("/posts", () => Cascaron.PendienteDeImplementar("EP-031", "nv-08"))
            .WithName("PostPosts")
            .WithSummary("POST /posts");

        // EP-032 — nv-08
        group.MapGet("/posts", () => Cascaron.PendienteDeImplementar("EP-032", "nv-08"))
            .WithName("GetPosts")
            .WithSummary("GET /posts");

        // EP-033 — nv-08
        group.MapGet("/posts/{id}", () => Cascaron.PendienteDeImplementar("EP-033", "nv-08"))
            .WithName("GetPostsPorId")
            .WithSummary("GET /posts/{id}");

        // EP-034 — nv-08
        group.MapPatch("/posts/{id}", () => Cascaron.PendienteDeImplementar("EP-034", "nv-08"))
            .WithName("PatchPostsPorId")
            .WithSummary("PATCH /posts/{id}");

        // EP-035 — nv-08
        group.MapDelete("/posts/{id}", () => Cascaron.PendienteDeImplementar("EP-035", "nv-08"))
            .WithName("DeletePostsPorId")
            .WithSummary("DELETE /posts/{id}");

        return group;
    }
}
