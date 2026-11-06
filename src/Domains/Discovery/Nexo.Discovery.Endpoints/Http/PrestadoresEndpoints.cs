using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de Prestadores (6 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class PrestadoresEndpoints
{
    public static RouteGroupBuilder MapPrestadoresEndpoints(this RouteGroupBuilder group)
    {
        // EP-016 — nv-06
        group.MapPost("/providers", () => Cascaron.PendienteDeImplementar("EP-016", "nv-06"))
            .WithName("PostProviders")
            .WithSummary("POST /providers");

        // EP-017 — nv-06
        group.MapGet("/providers/{id}", () => Cascaron.PendienteDeImplementar("EP-017", "nv-06"))
            .WithName("GetProvidersPorId")
            .WithSummary("GET /providers/{id}");

        // EP-025 — nv-07
        group.MapGet("/providers", () => Cascaron.PendienteDeImplementar("EP-025", "nv-07"))
            .WithName("GetProviders")
            .WithSummary("GET /providers");

        // EP-027 — nv-07
        group.MapGet("/providers/compare", () => Cascaron.PendienteDeImplementar("EP-027", "nv-07"))
            .WithName("GetProvidersCompare")
            .WithSummary("GET /providers/compare");

        // EP-042 — nv-19
        group.MapGet("/providers/{id}/reviews", () => Cascaron.PendienteDeImplementar("EP-042", "nv-19"))
            .WithName("GetProvidersPorIdReviews")
            .WithSummary("GET /providers/{id}/reviews");

        // EP-091 — nv-25
        group.MapGet("/providers/{id}/contact", () => Cascaron.PendienteDeImplementar("EP-091", "nv-25"))
            .WithName("GetProvidersPorIdContact")
            .WithSummary("GET /providers/{id}/contact");

        return group;
    }
}
