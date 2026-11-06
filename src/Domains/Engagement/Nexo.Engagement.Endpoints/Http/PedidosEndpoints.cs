using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Pedidos (10 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class PedidosEndpoints
{
    public static RouteGroupBuilder MapPedidosEndpoints(this RouteGroupBuilder group)
    {
        // EP-058 — nv-10
        group.MapPost("/orders", () => Cascaron.PendienteDeImplementar("EP-058", "nv-10"))
            .WithName("PostOrders")
            .WithSummary("POST /orders");

        // EP-059 — nv-10
        group.MapGet("/me/orders", () => Cascaron.PendienteDeImplementar("EP-059", "nv-10"))
            .WithName("GetMeOrders")
            .WithSummary("GET /me/orders");

        // EP-060 — nv-10
        group.MapGet("/orders/{id}", () => Cascaron.PendienteDeImplementar("EP-060", "nv-10"))
            .WithName("GetOrdersPorId")
            .WithSummary("GET /orders/{id}");

        // EP-061 — nv-10
        group.MapPost("/orders/{id}/accept", () => Cascaron.PendienteDeImplementar("EP-061", "nv-10"))
            .WithName("PostOrdersPorIdAccept")
            .WithSummary("POST /orders/{id}/accept");

        // EP-062 — nv-10
        group.MapPost("/orders/{id}/reject", () => Cascaron.PendienteDeImplementar("EP-062", "nv-10"))
            .WithName("PostOrdersPorIdReject")
            .WithSummary("POST /orders/{id}/reject");

        // EP-063 — nv-10
        group.MapPost("/orders/{id}/cancel", () => Cascaron.PendienteDeImplementar("EP-063", "nv-10"))
            .WithName("PostOrdersPorIdCancel")
            .WithSummary("POST /orders/{id}/cancel");

        // EP-064 — nv-10
        group.MapPost("/orders/{id}/complete", () => Cascaron.PendienteDeImplementar("EP-064", "nv-10"))
            .WithName("PostOrdersPorIdComplete")
            .WithSummary("POST /orders/{id}/complete");

        // EP-065 — nv-10
        group.MapPatch("/orders/{id}/address", () => Cascaron.PendienteDeImplementar("EP-065", "nv-10"))
            .WithName("PatchOrdersPorIdAddress")
            .WithSummary("PATCH /orders/{id}/address");

        // EP-092 — nv-25
        group.MapGet("/orders/{id}/contact", () => Cascaron.PendienteDeImplementar("EP-092", "nv-25"))
            .WithName("GetOrdersPorIdContact")
            .WithSummary("GET /orders/{id}/contact");

        // EP-097 — nv-25
        group.MapPatch("/orders/{id}/contact-preference", () => Cascaron.PendienteDeImplementar("EP-097", "nv-25"))
            .WithName("PatchOrdersPorIdContactPreference")
            .WithSummary("PATCH /orders/{id}/contact-preference");

        return group;
    }
}
