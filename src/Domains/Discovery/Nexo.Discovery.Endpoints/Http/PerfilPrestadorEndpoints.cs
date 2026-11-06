using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Discovery.Endpoints.Http;

/// <summary>
/// Cascaron de PerfilPrestador (9 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class PerfilPrestadorEndpoints
{
    public static RouteGroupBuilder MapPerfilPrestadorEndpoints(this RouteGroupBuilder group)
    {
        // EP-018 — nv-06
        group.MapGet("/me/provider", () => Cascaron.PendienteDeImplementar("EP-018", "nv-06"))
            .WithName("GetMeProvider")
            .WithSummary("GET /me/provider");

        // EP-019 — nv-06
        group.MapPatch("/me/provider", () => Cascaron.PendienteDeImplementar("EP-019", "nv-06"))
            .WithName("PatchMeProvider")
            .WithSummary("PATCH /me/provider");

        // EP-020 — nv-06
        group.MapPatch("/me/provider/coverage", () => Cascaron.PendienteDeImplementar("EP-020", "nv-06"))
            .WithName("PatchMeProviderCoverage")
            .WithSummary("PATCH /me/provider/coverage");

        // EP-021 — nv-06
        group.MapPatch("/me/provider/schedule", () => Cascaron.PendienteDeImplementar("EP-021", "nv-06"))
            .WithName("PatchMeProviderSchedule")
            .WithSummary("PATCH /me/provider/schedule");

        // EP-022 — nv-06
        group.MapPatch("/me/provider/prices", () => Cascaron.PendienteDeImplementar("EP-022", "nv-06"))
            .WithName("PatchMeProviderPrices")
            .WithSummary("PATCH /me/provider/prices");

        // EP-023 — nv-06
        group.MapPost("/me/provider/refresh", () => Cascaron.PendienteDeImplementar("EP-023", "nv-06"))
            .WithName("PostMeProviderRefresh")
            .WithSummary("POST /me/provider/refresh");

        // EP-024 — nv-06
        group.MapDelete("/me/provider", () => Cascaron.PendienteDeImplementar("EP-024", "nv-06"))
            .WithName("DeleteMeProvider")
            .WithSummary("DELETE /me/provider");

        // EP-046 — nv-18
        group.MapGet("/me/provider/metrics", () => Cascaron.PendienteDeImplementar("EP-046", "nv-18"))
            .WithName("GetMeProviderMetrics")
            .WithSummary("GET /me/provider/metrics");

        // EP-096 — nv-25
        group.MapPatch("/me/provider/contact-preferences", () => Cascaron.PendienteDeImplementar("EP-096", "nv-25"))
            .WithName("PatchMeProviderContactPreferences")
            .WithSummary("PATCH /me/provider/contact-preferences");

        return group;
    }
}
