using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Notificaciones (4 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class NotificacionesEndpoints
{
    public static RouteGroupBuilder MapNotificacionesEndpoints(this RouteGroupBuilder group)
    {
        // EP-083 — nv-21
        group.MapPut("/me/devices/{id}", () => Cascaron.PendienteDeImplementar("EP-083", "nv-21"))
            .WithName("PutMeDevicesPorId")
            .WithSummary("PUT /me/devices/{id}");

        // EP-084 — nv-21
        group.MapDelete("/me/devices/{id}", () => Cascaron.PendienteDeImplementar("EP-084", "nv-21"))
            .WithName("DeleteMeDevicesPorId")
            .WithSummary("DELETE /me/devices/{id}");

        // EP-085 — nv-21
        group.MapGet("/me/notifications", () => Cascaron.PendienteDeImplementar("EP-085", "nv-21"))
            .WithName("GetMeNotifications")
            .WithSummary("GET /me/notifications");

        // EP-086 — nv-21
        group.MapPut("/me/notifications/{id}/read", () => Cascaron.PendienteDeImplementar("EP-086", "nv-21"))
            .WithName("PutMeNotificationsPorIdRead")
            .WithSummary("PUT /me/notifications/{id}/read");

        return group;
    }
}
