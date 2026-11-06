using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Http;

namespace Nexo.Engagement.Endpoints.Http;

/// <summary>
/// Cascaron de Conversaciones (6 operaciones). Las rutas estan montadas y documentadas;
/// cada caso de uso se implementa en su incremento y sustituye el 501 por su logica.
/// </summary>
public static class ConversacionesEndpoints
{
    public static RouteGroupBuilder MapConversacionesEndpoints(this RouteGroupBuilder group)
    {
        // EP-066 — nv-11
        group.MapPost("/conversations", () => Cascaron.PendienteDeImplementar("EP-066", "nv-11"))
            .WithName("PostConversations")
            .WithSummary("POST /conversations");

        // EP-068 — nv-11
        group.MapGet("/conversations/{id}/messages", () => Cascaron.PendienteDeImplementar("EP-068", "nv-11"))
            .WithName("GetConversationsPorIdMessages")
            .WithSummary("GET /conversations/{id}/messages");

        // EP-069 — nv-11
        group.MapPost("/conversations/{id}/messages", () => Cascaron.PendienteDeImplementar("EP-069", "nv-11"))
            .WithName("PostConversationsPorIdMessages")
            .WithSummary("POST /conversations/{id}/messages");

        // EP-070 — nv-11
        group.MapPut("/conversations/{id}/read", () => Cascaron.PendienteDeImplementar("EP-070", "nv-11"))
            .WithName("PutConversationsPorIdRead")
            .WithSummary("PUT /conversations/{id}/read");

        // EP-071 — nv-11
        group.MapPut("/conversations/{id}/block", () => Cascaron.PendienteDeImplementar("EP-071", "nv-11"))
            .WithName("PutConversationsPorIdBlock")
            .WithSummary("PUT /conversations/{id}/block");

        // EP-072 — nv-11
        group.MapDelete("/conversations/{id}/block", () => Cascaron.PendienteDeImplementar("EP-072", "nv-11"))
            .WithName("DeleteConversationsPorIdBlock")
            .WithSummary("DELETE /conversations/{id}/block");

        return group;
    }
}
