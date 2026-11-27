using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Accounts;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Conversations;
using Nexo.Engagement.Application.Orders;
using Nexo.Engagement.Domain.Orders;

namespace Nexo.Engagement.Infrastructure;

/// <summary>
/// Parte de la baja de cuenta en Engagement (nv-12). Los pedidos se cancelan por la misma transición
/// que usa la app, para conservar su cadena de eventos y avisar a la contraparte.
/// </summary>
public sealed class EngagementAccountLifecycle(
    [FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database,
    IOrderStore orders, IOrderRequestLink requests,
    IPrivateChatAttachments attachments, IChatAttachmentOwners attachmentOwners) : IAccountDataLifecycle
{
    private const string ClosedBy = "closedByDeletion";

    public string Domain => "engagement";

    private IMongoCollection<BsonDocument> Of(string name) => database.GetCollection<BsonDocument>(name);
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);
    private static BsonDocument In(IEnumerable<BsonValue> values) => new("$in", new BsonArray(values));

    private static BsonDocument Either(Guid account, string first, string second) =>
        new("$or", new BsonArray { new BsonDocument(first, Id(account)), new BsonDocument(second, Id(account)) });

    public async Task SuspendAsync(AccountSubject subject, DateTime now, CancellationToken ct)
    {
        await CancelAsync(subject.AccountId, [ServiceOrder.Pending], now, ct);
        // Cerradas a mensajes nuevos para ambas partes; se reabren si la baja se cancela.
        await Of("conversations").UpdateManyAsync(Either(subject.AccountId, "clientId", "providerAccountId"),
            new BsonDocument("$addToSet", new BsonDocument(ClosedBy, Id(subject.AccountId))), cancellationToken: ct);
    }

    public Task RestoreAsync(AccountSubject subject, DateTime now, CancellationToken ct) =>
        Of("conversations").UpdateManyAsync(Either(subject.AccountId, "clientId", "providerAccountId"),
            new BsonDocument("$pull", new BsonDocument(ClosedBy, Id(subject.AccountId))), cancellationToken: ct);

    public async Task EraseAsync(AccountSubject subject, DateTime now, CancellationToken ct)
    {
        var account = subject.AccountId;
        var id = Id(account);

        // Vencido el plazo ya no se espera a los pedidos aceptados.
        await CancelAsync(account, [ServiceOrder.Pending, ServiceOrder.Accepted], now, ct);

        // Solo sobreviven los pedidos resueltos, sin texto ni domicilio.
        var mine = Either(account, "clientId", "providerAccountId");
        var discarded = await Of("orders")
            .Find(new BsonDocument("$and", new BsonArray { mine, new BsonDocument("status", new BsonDocument("$ne", ServiceOrder.Resolved)) }))
            .Project(new BsonDocument("_id", 1)).ToListAsync(ct);
        if (discarded.Count > 0)
        {
            var ids = discarded.Select(d => d["_id"]).ToList();
            await Of("order_events").DeleteManyAsync(new BsonDocument("streamId", In(ids)), ct);
            await Of("order_idempotency").DeleteManyAsync(new BsonDocument("orderId", In(ids)), ct);
            await Of("orders").DeleteManyAsync(new BsonDocument("_id", In(ids)), ct);
        }

        await Of("orders").UpdateManyAsync(mine, new BsonDocument
        {
            ["$set"] = new BsonDocument { ["description"] = "", ["addressCipher"] = BsonNull.Value, ["placeZone"] = "" },
            ["$unset"] = new BsonDocument { ["rejectNote"] = "", ["resolveNote"] = "", ["priceIncludes"] = "" }
        }, cancellationToken: ct);

        // Lo que escribió deja de tener contenido; la otra persona conserva su propia parte del hilo.
        await Of("messages").UpdateManyAsync(
            new BsonDocument { ["senderId"] = id, ["kind"] = new BsonDocument("$ne", "system") },
            new BsonDocument
            {
                ["$set"] = new BsonDocument { ["kind"] = "deleted", ["text"] = BsonNull.Value, ["imageUrl"] = BsonNull.Value, ["thumbUrl"] = BsonNull.Value },
                ["$unset"] = new BsonDocument { ["w"] = "", ["h"] = "" }
            }, cancellationToken: ct);
        var conversations = Either(account, "clientId", "providerAccountId");
        await Of("conversations").UpdateManyAsync(conversations, new BsonDocument
        {
            ["$set"] = new BsonDocument("lastPreview", BsonNull.Value),
            ["$addToSet"] = new BsonDocument(ClosedBy, id)
        }, cancellationToken: ct);

        // Archivos cifrados que subió. Cada registro se olvida solo después de borrar su objeto.
        foreach (var attachment in await attachmentOwners.OfAsync(account, ct))
        {
            await attachments.DeleteAsync(attachment.ConversationId, attachment.AttachmentId, ct);
            await attachmentOwners.ForgetAsync(attachment, ct);
        }

        await Of("chat_public_keys").DeleteManyAsync(new BsonDocument("account", account.ToString("D")), ct);
        await Of("notifications").DeleteManyAsync(new BsonDocument("accountId", id), ct);
        var interests = new BsonArray { new BsonDocument("clientId", id) };
        if (subject.ProviderId is { } provider) interests.Add(new BsonDocument("providerId", Id(provider)));
        await Of("interests").DeleteManyAsync(new BsonDocument("$or", interests), ct);
        await Of("contact_log").DeleteManyAsync(Either(account, "actorId", "targetId"), ct);
        // El reporte sigue disponible para moderación, sin el texto libre de quien se dio de baja.
        await Of("reports").UpdateManyAsync(new BsonDocument("reporterId", id),
            new BsonDocument("$set", new BsonDocument("reason", "")), cancellationToken: ct);
    }

    private async Task CancelAsync(Guid account, string[] statuses, DateTime now, CancellationToken ct)
    {
        var filter = new BsonDocument("$and", new BsonArray
        {
            Either(account, "clientId", "providerAccountId"),
            new BsonDocument("status", In(statuses.Select(s => (BsonValue)s)))
        });
        var open = await Of("orders").Find(filter).Project(new BsonDocument("_id", 1)).ToListAsync(ct);
        foreach (var row in open)
        {
            var order = await orders.FindByIdAsync(row["_id"].AsBsonBinaryData.ToGuid(), ct);
            if (order is null || !statuses.Contains(order.Status)) continue;
            var role = order.ClientId == account ? "client" : "provider";
            var result = await orders.TransitionAsync(order.Id, account, null, ServiceOrder.Cancelled, order.Version,
                new OrderMutation(ServiceOrder.Cancelled, order.Version + 1, now, role, Reason: "account_deletion"), ct);
            // Alguien lo cambió a la vez: se repite el ciclo completo.
            if (result.Order is null || result.Order.Status != ServiceOrder.Cancelled)
                throw new InvalidOperationException("Order changed while closing the account.");
            // La solicitud de la otra persona vuelve a quedar disponible.
            if (role == "provider" && order.RequestId is { } request)
                await requests.ReleaseAsync(request, order.ProviderId, ct);
        }
    }
}
