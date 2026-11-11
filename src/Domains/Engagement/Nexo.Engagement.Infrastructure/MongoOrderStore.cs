using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Orders;
using Nexo.Outbox;

namespace Nexo.Engagement.Infrastructure;

/// <summary>Persistencia propia de pedidos. Cada transición y su evento/outbox comparten una transacción Mongo.</summary>
public sealed class MongoOrderStore([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database) : IOrderStore
{
    private readonly IMongoCollection<BsonDocument> orders = database.GetCollection<BsonDocument>("orders");
    private readonly IMongoCollection<BsonDocument> eventRows = database.GetCollection<BsonDocument>("order_events");
    private readonly IMongoCollection<BsonDocument> idempotency = database.GetCollection<BsonDocument>("order_idempotency");
    private readonly IMongoCollection<OutboxMessage> outbox = database.GetCollection<OutboxMessage>("outbox");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);

    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await orders.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Ascending("providerId"), new CreateIndexOptions<BsonDocument> { Unique = true, Name = "ux_order_active_client_provider", PartialFilterExpression = new BsonDocument("status", new BsonDocument("$in", new BsonArray { "pending", "accepted" })) }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Ascending("status").Descending("createdAt"), new CreateIndexOptions { Name = "ix_order_client_status_created" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("providerAccountId").Ascending("status").Descending("createdAt"), new CreateIndexOptions { Name = "ix_order_provider_status_created" })
            ], ct);
            await eventRows.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("streamId").Ascending("version"), new CreateIndexOptions { Unique = true, Name = "ux_order_event_stream_version" }), cancellationToken: ct);
            await idempotency.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Ascending("key"), new CreateIndexOptions { Unique = true, Name = "ux_order_idempotency_client_key" }), cancellationToken: ct);
            _ = new MongoOutboxRepository(database);
            initialized = true;
        }
        finally { gate.Release(); }
    }

    public async Task<OrderSnapshot?> FindAsync(Guid id, Guid participant, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var doc = await orders.Find(new BsonDocument { ["_id"] = Id(id), ["$or"] = new BsonArray { new BsonDocument("clientId", Id(participant)), new BsonDocument("providerAccountId", Id(participant)) } }).FirstOrDefaultAsync(ct);
        return doc == null ? null : Read(doc);
    }
    public async Task<OrderSnapshot?> FindByIdAsync(Guid id, CancellationToken ct)
    { await EnsureAsync(ct); var doc = await orders.Find(new BsonDocument("_id", Id(id))).FirstOrDefaultAsync(ct); return doc == null ? null : Read(doc); }

    public async Task<OrderSnapshot?> CreateAsync(OrderDraft draft, Guid key, string hash, CancellationToken ct)
    {
        await EnsureAsync(ct);
        try
        {
            using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
            return await session.WithTransactionAsync<OrderSnapshot?>(async (s, token) =>
            {
                var receipt = await idempotency.Find(s, new BsonDocument { ["clientId"] = Id(draft.ClientId), ["key"] = Id(key) }).FirstOrDefaultAsync(token);
                if (receipt != null)
                {
                    if (receipt["hash"] != hash) return null;
                    var old = await orders.Find(s, new BsonDocument("_id", Id(receipt["orderId"].AsBsonBinaryData.ToGuid()))).FirstOrDefaultAsync(token);
                    return old == null ? null : Read(old);
                }
                var order = Write(draft);
                await orders.InsertOneAsync(s, order, cancellationToken: token);
                await idempotency.InsertOneAsync(s, new BsonDocument { ["clientId"] = Id(draft.ClientId), ["key"] = Id(key), ["hash"] = hash, ["orderId"] = Id(draft.Id), ["createdAt"] = draft.CreatedAt }, cancellationToken: token);
                await AppendEvent(s, draft.Id, draft.ClientId, 1, "created", draft.CreatedAt, null, token);
                await Emit(s, draft, "order.created", token);
                return Read(order);
            }, cancellationToken: ct);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { return null; }
        catch (MongoException e) { throw new OrderUnavailableException(e); }
    }

    public async Task<OrderPage> ListAsync(Guid actor, Guid? provider, string role, string status, Guid? before, int limit, CancellationToken ct)
    {
        await EnsureAsync(ct);
        // OrderService passes the provider profile id (not its account id) for the provider role.
        var ownerField = role == "client" ? "clientId" : "providerId";
        var ownerId = role == "client" ? actor : provider ?? Guid.Empty;
        var f = new BsonDocument(ownerField, Id(ownerId));
        if (status == "active") f["status"] = new BsonDocument("$in", new BsonArray { "pending", "accepted" }); else if (status != "all") f["status"] = status;
        if (before != null) f["_id"] = new BsonDocument("$lt", Id(before.Value));
        var rows = await orders.Find(f).Sort(new BsonDocument("_id", -1)).Limit(limit).ToListAsync(ct);
        var countsDoc = await orders.Aggregate().Match(new BsonDocument(ownerField, Id(ownerId)))
            .Group(new BsonDocument { ["_id"] = "$status", ["count"] = new BsonDocument("$sum", 1) }).ToListAsync(ct);
        var counts = countsDoc.ToDictionary(d => d["_id"].AsString, d => d["count"].AsInt32);
        return new(rows.Select(Read).ToArray(), counts, rows.Count >= limit);
    }

    public async Task<OrderMutationResult> TransitionAsync(Guid id, Guid actor, Guid? provider, string target,
        int? expectedVersion, OrderMutation mutation, CancellationToken ct)
    {
        await EnsureAsync(ct);
        using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
        try
        {
            return await session.WithTransactionAsync<OrderMutationResult>(async (s, token) =>
            {
                var baseFilter = new BsonDocument("_id", Id(id));
                if (target is "accepted" or "rejected" or "resolved") baseFilter["providerAccountId"] = Id(actor);
                else baseFilter["$or"] = new BsonArray { new BsonDocument("clientId", Id(actor)), new BsonDocument("providerAccountId", Id(actor)) };
                if (provider != null && target is "accepted" or "rejected" or "resolved") baseFilter["providerId"] = Id(provider.Value);
                var old = await orders.Find(s, baseFilter).FirstOrDefaultAsync(token);
                if (old == null) return new(null, true, false, false);
                var current = Read(old);
                if (expectedVersion != null && expectedVersion != current.Version) return new(current, false, false, true);
                if (!Nexo.Engagement.Domain.Orders.ServiceOrder.CanTransition(current.Status, target)) return new(current, false, true, false);
                var ver = current.Version + 1;
                var fields = new BsonDocument { ["status"] = target, ["version"] = ver };
                if (target == "accepted") { fields["acceptedAt"] = mutation.At; if (mutation.PriceCents != null) fields["priceCents"] = mutation.PriceCents; if (mutation.PriceIncludes != null) fields["priceIncludes"] = mutation.PriceIncludes; }
                if (target == "rejected") { fields["rejectedAt"] = mutation.At; fields["rejectReason"] = mutation.Reason ?? "other"; if (mutation.Note != null) fields["rejectNote"] = mutation.Note; }
                if (target == "cancelled") { fields["cancelledAt"] = mutation.At; fields["cancelledBy"] = mutation.ActorRole; if (mutation.Reason != null) fields["cancelReason"] = mutation.Reason; }
                if (target == "resolved") { fields["resolvedAt"] = mutation.At; if (mutation.PriceCents != null) fields["priceCents"] = mutation.PriceCents; if (mutation.PriceIncludes != null) fields["priceIncludes"] = mutation.PriceIncludes; if (mutation.Note != null) fields["resolveNote"] = mutation.Note; }
                var filter = new BsonDocument(baseFilter) { ["status"] = current.Status, ["version"] = current.Version };
                var saved = await orders.FindOneAndUpdateAsync(s, filter, new BsonDocument("$set", fields), new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, token);
                if (saved == null) return new(null, false, false, true);
                await AppendEvent(s, id, actor, ver, target, mutation.At, target == "accepted" ? JsonSerializer.Serialize(new { mutation.PriceCents, mutation.PriceIncludes }) : mutation.Reason, token);
                await Emit(s, Read(saved), $"order.{target}", token);
                return new(Read(saved), false, false, false);
            }, cancellationToken: ct);
        }
        catch (MongoException e) { throw new OrderUnavailableException(e); }
    }

    public async Task<OrderMutationResult> ChangeAddressAsync(Guid id, Guid client, int? expectedVersion, OrderAddress address, string cipher, string zone, CancellationToken ct)
    {
        await EnsureAsync(ct);
        using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
        return await session.WithTransactionAsync<OrderMutationResult>(async (s, token) =>
        {
            var filter = new BsonDocument { ["_id"] = Id(id), ["clientId"] = Id(client), ["attention"] = "home", ["status"] = new BsonDocument("$in", new BsonArray { "pending", "accepted" }) };
            var old = await orders.Find(s, filter).FirstOrDefaultAsync(token);
            if (old == null) return new(null, true, false, false);
            var current = Read(old);
            if (expectedVersion != null && expectedVersion != current.Version) return new(current, false, false, true);
            var ver = current.Version + 1; var now = DateTime.UtcNow;
            var saved = await orders.FindOneAndUpdateAsync(s, filter, new BsonDocument("$set", new BsonDocument { ["addressCipher"] = cipher, ["placeZone"] = zone, ["version"] = ver }), new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, token);
            if (saved == null) return new(null, false, true, false);
            await AppendEvent(s, id, client, ver, "addressChanged", now, JsonSerializer.Serialize(new { placeZone = zone }), token);
            await Emit(s, Read(saved), "order.address_changed", token);
            return new(Read(saved), false, false, false);
        }, cancellationToken: ct);
    }

    public async Task<OrderEvent[]> EventsAsync(Guid id, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var docs = await eventRows.Find(new BsonDocument("streamId", Id(id))).Sort(new BsonDocument("version", 1)).ToListAsync(ct);
        return docs.Select(d => new OrderEvent(d["version"].AsInt32, d["type"].AsString, d["actorId"].AsBsonBinaryData.ToGuid(), d["at"].ToUniversalTime(), d.GetValue("data", BsonNull.Value).IsBsonNull ? null : d["data"].AsString)).ToArray();
    }

    private async Task AppendEvent(IClientSessionHandle s, Guid id, Guid actor, int version, string type, DateTime at, string? data, CancellationToken ct)
    {
        var prev = await eventRows.Find(s, new BsonDocument("streamId", Id(id))).Sort(new BsonDocument("version", -1)).FirstOrDefaultAsync(ct);
        var prevHash = prev?.GetValue("hash", "").AsString ?? "";
        var material = $"{prevHash}|{version}|{type}|{actor:D}|{at.ToUniversalTime():O}|{data ?? ""}";
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(material)));
        var row = new BsonDocument
        {
            ["_id"] = Id(Guid.CreateVersion7()),
            ["streamId"] = Id(id),
            ["version"] = version,
            ["type"] = type,
            ["actorId"] = Id(actor),
            ["at"] = at.ToUniversalTime(),
            ["prevHash"] = prevHash,
            ["hash"] = hash,
            ["data"] = data == null ? BsonNull.Value : data
        };
        await eventRows.InsertOneAsync(s, row, cancellationToken: ct);
    }
    private Task Emit(IClientSessionHandle s, OrderDraft d, string type, CancellationToken ct) => outbox.InsertOneAsync(s, new OutboxMessage
    {
        MessageId = Guid.CreateVersion7(),
        Type = type,
        Payload = JsonSerializer.Serialize(new { orderId = d.Id, clientId = d.ClientId, providerId = d.ProviderId, requestId = d.RequestId, placeZone = d.PlaceZone }),
        OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Utc))
    }, cancellationToken: ct);
    private Task Emit(IClientSessionHandle s, OrderSnapshot o, string type, CancellationToken ct) => outbox.InsertOneAsync(s, new OutboxMessage
    {
        MessageId = Guid.CreateVersion7(),
        Type = type,
        Payload = JsonSerializer.Serialize(new { orderId = o.Id, clientId = o.ClientId, providerId = o.ProviderId, requestId = o.RequestId }),
        OccurredAt = DateTimeOffset.UtcNow
    }, cancellationToken: ct);
    private static BsonDocument Write(OrderDraft d) => new()
    {
        ["_id"] = Id(d.Id),
        ["clientId"] = Id(d.ClientId),
        ["providerId"] = Id(d.ProviderId),
        ["providerAccountId"] = Id(d.ProviderAccountId),
        ["requestId"] = d.RequestId is { } request ? Id(request) : BsonNull.Value,
        ["origin"] = d.Origin,
        ["description"] = d.Description,
        ["category"] = "",
        ["attention"] = d.Attention,
        ["placeZone"] = d.PlaceZone,
        ["addressCipher"] = d.AddressCipher == null ? BsonNull.Value : d.AddressCipher,
        ["status"] = "pending",
        ["version"] = 1,
        ["createdAt"] = d.CreatedAt
    };
    private static BsonDocument PointDoc(OrderPoint p) => new() { ["lat"] = p.Lat, ["lng"] = p.Lng };
    private static OrderSnapshot Read(BsonDocument d) => new(d["_id"].AsBsonBinaryData.ToGuid(), d["clientId"].AsBsonBinaryData.ToGuid(), d["providerId"].AsBsonBinaryData.ToGuid(),
        d.GetValue("requestId", BsonNull.Value).IsBsonNull ? null : d["requestId"].AsBsonBinaryData.ToGuid(), d.GetValue("origin", "direct").AsString, d.GetValue("category", "").AsString,
        d.GetValue("description", "").AsString, d.GetValue("attention", "shop").AsString, d.GetValue("placeZone", "").AsString,
        d.GetValue("addressCipher", BsonNull.Value).IsBsonNull ? null : d["addressCipher"].AsString,
        d.GetValue("point", BsonNull.Value).IsBsonNull ? null : new OrderPoint(d["point"]["lat"].ToDouble(), d["point"]["lng"].ToDouble()), d["status"].AsString,
        d["createdAt"].ToUniversalTime(), d.GetValue("acceptedAt", BsonNull.Value).IsBsonNull ? null : d["acceptedAt"].ToUniversalTime(),
        d.GetValue("cancelledAt", BsonNull.Value).IsBsonNull ? null : d["cancelledAt"].ToUniversalTime(), d.GetValue("cancelledBy", BsonNull.Value).IsBsonNull ? null : d["cancelledBy"].AsString,
        d.GetValue("resolvedAt", BsonNull.Value).IsBsonNull ? null : d["resolvedAt"].ToUniversalTime(), d.GetValue("version", 1).AsInt32,
        d.GetValue("priceCents", BsonNull.Value).IsBsonNull ? null : d["priceCents"].AsInt32, d.GetValue("priceIncludes", BsonNull.Value).IsBsonNull ? null : d["priceIncludes"].AsString);
}
