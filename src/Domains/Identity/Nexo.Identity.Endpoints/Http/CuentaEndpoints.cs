using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Identity.Endpoints.Http;

/// <summary>
/// Cascaron de Cuenta (3 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class CuentaEndpoints
{
    public static RouteGroupBuilder MapCuentaEndpoints(this RouteGroupBuilder group)
    {
        // EP-009 — nv-16
        group.MapPatch("/me", () => Cascaron.PendienteDeImplementar("EP-009", "nv-16"))
            .WithName("PatchMe")
            .WithSummary("PATCH /me");

        // EP-012 — nv-12
        group.MapPost("/me/deletion", () => Cascaron.PendienteDeImplementar("EP-012", "nv-12"))
            .WithName("PostMeDeletion")
            .WithSummary("POST /me/deletion");

        // EP-013 — nv-12
        group.MapGet("/me/deletion/{id}", () => Cascaron.PendienteDeImplementar("EP-013", "nv-12"))
            .WithName("GetMeDeletionPorId")
            .WithSummary("GET /me/deletion/{id}");

        return group;
    }
}
