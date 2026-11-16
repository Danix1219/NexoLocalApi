using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Notifications;
using Nexo.Outbox;

namespace Nexo.Engagement.Infrastructure.Notifications;

/// <summary>Turns Engagement outbox events into bell notices (and push when configured).
/// Notices carry references only: never addresses, phones, prices or chat text.</summary>
public sealed class EngagementOutboxHandler(
    [FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database,
    INotificationStore notifications, IPushSender push, TimeProvider clock, ILogger<EngagementOutboxHandler> log) : IOutboxHandler
{
    private readonly IMongoCollection<BsonDocument> orders = database.GetCollection<BsonDocument>("orders");
    private readonly IMongoCollection<BsonDocument> reviews = database.GetCollection<BsonDocument>("reviews");
    private readonly IMongoCollection<BsonDocument> interests = database.GetCollection<BsonDocument>("interests");
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);

    public async Task HandleAsync(OutboxMessage message, CancellationToken ct = default)
    {
        var draft = await BuildAsync(message, ct);
        if (draft == null) return; // events without a notice (contact, reports, blocks...) are simply consumed
        await notifications.UpsertAsync(draft, ct);
        try { await push.SendAsync(draft.AccountId, draft.Title, draft.Body, draft.DeepLink, ct); }
        // The bell already has it; a push failure must not replay the event.
        catch (Exception e) when (e is not OperationCanceledException) { log.LogWarning(e, "Push no entregado para {MessageId}.", message.MessageId); }
    }

    public async Task<NotificationDraft?> BuildAsync(OutboxMessage message, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(message.Payload);
        var p = json.RootElement;
        var at = clock.GetUtcNow().UtcDateTime;
        Guid G(string name) => p.GetProperty(name).GetGuid();

        switch (message.Type)
        {
            case "order.created":
            case "order.accepted":
            case "order.rejected":
            case "order.cancelled":
            case "order.resolved":
            case "order.address_changed":
                {
                    var orderId = G("orderId");
                    var order = await orders.Find(new BsonDocument("_id", Id(orderId))).FirstOrDefaultAsync(ct);
                    if (order == null) return null;
                    var client = order["clientId"].AsBsonBinaryData.ToGuid();
                    var provider = order["providerAccountId"].AsBsonBinaryData.ToGuid();
                    var link = $"nexolocal://orders/{orderId}";
                    var zone = order.GetValue("placeZone", "").AsString;
                    return message.Type switch
                    {
                        "order.created" => new(provider, message.Type, orderId.ToString("N"), "Tienes un pedido nuevo",
                            string.IsNullOrEmpty(zone) ? "Revisa los detalles y acéptalo o recházalo." : $"Pedido en {zone}. Revisa los detalles y acéptalo o recházalo.",
                            link, "Ver pedido", "order", at),
                        "order.accepted" => new(client, message.Type, orderId.ToString("N"), "Aceptaron tu pedido", "Ya puedes ver los detalles y escribirle.", link, "Ver pedido", "order", at),
                        "order.rejected" => new(client, message.Type, orderId.ToString("N"), "Tu pedido no fue aceptado", "Puedes pedirle el servicio a otro prestador.", link, "Ver pedido", "order", at),
                        "order.cancelled" => order.GetValue("cancelledBy", "").AsString == "provider"
                            ? new(client, message.Type, orderId.ToString("N"), "El prestador canceló el pedido", "Puedes pedirle el servicio a otro prestador.", link, "Ver pedido", "order", at)
                            : new(provider, message.Type, orderId.ToString("N"), "El cliente canceló el pedido", "Ya no tienes que atenderlo.", link, "Ver pedido", "order", at),
                        "order.resolved" => new(client, message.Type, orderId.ToString("N"), "Servicio marcado como resuelto", "Califícalo para ayudar a otros.", link, "Calificar", "star", at),
                        _ => new(provider, message.Type, orderId.ToString("N"), "El cliente actualizó el domicilio", "Revisa el pedido antes de ir.", link, "Ver pedido", "order", at)
                    };
                }
            case "message.sent":
                {
                    var conversation = G("conversationId");
                    // One row per conversation; no message text.
                    return new(G("recipientId"), "message.received", conversation.ToString("N"), "Tienes un mensaje nuevo", "Abre el chat para leerlo.",
                        $"nexolocal://chat/{conversation}", "Abrir chat", "chat", at);
                }
            case "review.created":
                {
                    var review = await reviews.Find(new BsonDocument("_id", Id(G("reviewId")))).FirstOrDefaultAsync(ct);
                    if (review == null) return null;
                    var order = await orders.Find(new BsonDocument("_id", review["orderId"])).Project(new BsonDocument("providerAccountId", 1)).FirstOrDefaultAsync(ct);
                    if (order == null) return null;
                    var stars = review["stars"].ToInt32();
                    return new(order["providerAccountId"].AsBsonBinaryData.ToGuid(), "review.received", review["_id"].AsBsonBinaryData.ToGuid().ToString("N"),
                        "Recibiste una opinión nueva", $"Te calificaron con {stars} de 5 estrellas.",
                        $"nexolocal://provider/{review["providerId"].AsBsonBinaryData.ToGuid()}", "Ver opiniones", "star", at);
                }
            case "interest.expressed":
                {
                    var request = G("requestId");
                    var count = (int)await interests.CountDocumentsAsync(new BsonDocument { ["requestId"] = Id(request), ["withdrawnAt"] = new BsonDocument("$exists", false) }, cancellationToken: ct);
                    if (count == 0) return null;
                    return new(G("clientId"), "interest.received", request.ToString("N"),
                        count == 1 ? "1 prestador interesado" : $"{count} prestadores interesados",
                        "Ya puedes comparar y elegir a quién pedirle el servicio.", $"nexolocal://requests/{request}/interested", "Ver y elegir", "bell", at);
                }
            default:
                return null;
        }
    }
}

/// <summary>Drains the Engagement database's own outbox with the shared claim/mark logic.</summary>
public sealed class EngagementOutboxDispatcher(IServiceScopeFactory scopes, ILogger<OutboxDispatcher> log) : OutboxDispatcher(scopes, log)
{
    protected override (IOutboxRepository Repository, IOutboxHandler Handler) Resolve(IServiceProvider services) =>
        (new MongoOutboxRepository(services.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement)),
         services.GetRequiredService<EngagementOutboxHandler>());
}
