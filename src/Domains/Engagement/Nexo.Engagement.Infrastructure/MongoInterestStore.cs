using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Outbox;
using Nexo.Engagement.Application.Interests;

namespace Nexo.Engagement.Infrastructure;

public sealed class MongoInterestStore : IInterestStore
{
    private readonly IMongoCollection<BsonDocument> interests;
    private readonly IMongoCollection<BsonDocument> orders;
    private readonly IMongoCollection<BsonDocument> conversations;
    private readonly IMongoCollection<OutboxMessage> outbox;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;

    public MongoInterestStore([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database)
    {
        interests = database.GetCollection<BsonDocument>("interests");
        orders = database.GetCollection<BsonDocument>("orders");
        conversations = database.GetCollection<BsonDocument>("conversations");
        _ = new MongoOutboxRepository(database); // Registers the shared OutboxMessage BSON mapping.
        outbox = database.GetCollection<OutboxMessage>("outbox");
        this.database = database;
    }

    private readonly IMongoDatabase database;

    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);
    private static string Key(Guid request, Guid provider) => $"{request:N}:{provider:N}";

    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await interests.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("requestId").Ascending("providerId"), new CreateIndexOptions { Unique = true, Name = "ux_interest_request_provider" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Ascending("requestId").Ascending("withdrawnAt"), new CreateIndexOptions { Name = "ix_interest_client_request_active" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("providerId").Descending("createdAt"), new CreateIndexOptions { Name = "ix_interest_provider_created" })
            ], ct);
            initialized = true;
        }
        catch (MongoException e) { throw new InterestUnavailableException(e); }
        finally { gate.Release(); }
    }

    public async Task<ExpressInterestResult> ExpressAsync(Guid requestId, Guid clientId, Guid providerId, string note, DateTime now, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var key = Key(requestId, providerId);
        using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
        return await session.WithTransactionAsync(async (s, token) =>
        {
            var filter = new BsonDocument("_id", key);
            var existing = await interests.Find(s, filter).FirstOrDefaultAsync(token);
            var wasActive = existing != null && !existing.Contains("withdrawnAt");
            var update = new BsonDocument
            {
                ["$setOnInsert"] = new BsonDocument { ["requestId"] = Id(requestId), ["clientId"] = Id(clientId), ["providerId"] = Id(providerId), ["note"] = note, ["createdAt"] = now },
                ["$unset"] = new BsonDocument("withdrawnAt", "")
            };
            await interests.UpdateOneAsync(s, filter, update, new UpdateOptions { IsUpsert = true }, token);
            if (!wasActive) await EmitAsync(s, "interest.expressed", requestId, clientId, providerId, now, token);
            var count = await interests.CountDocumentsAsync(s, new BsonDocument { ["requestId"] = Id(requestId), ["withdrawnAt"] = new BsonDocument("$exists", false) }, cancellationToken: token);
            var current = await interests.Find(s, filter).FirstAsync(token);
            return new ExpressInterestResult(wasActive, checked((int)count), current["createdAt"].ToUniversalTime());
        }, cancellationToken: ct);
    }

    public async Task<WithdrawInterestResult> WithdrawAsync(Guid requestId, Guid providerId, DateTime now, CancellationToken ct)
    {
        await EnsureAsync(ct);
        using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
        return await session.WithTransactionAsync(async (s, token) =>
        {
            var orderFilter = new BsonDocument { ["requestId"] = Id(requestId), ["providerId"] = Id(providerId), ["status"] = new BsonDocument("$in", new BsonArray { "pending", "accepted" }) };
            if (await orders.CountDocumentsAsync(s, orderFilter, cancellationToken: token) > 0) return new WithdrawInterestResult(0, false, true);
            var filter = new BsonDocument { ["requestId"] = Id(requestId), ["providerId"] = Id(providerId), ["withdrawnAt"] = new BsonDocument("$exists", false) };
            var changed = await interests.UpdateOneAsync(s, filter, new BsonDocument("$set", new BsonDocument("withdrawnAt", now)), cancellationToken: token);
            if (changed.ModifiedCount > 0)
            {
                var current = await interests.Find(s, new BsonDocument { ["requestId"] = Id(requestId), ["providerId"] = Id(providerId) }).FirstAsync(token);
                await EmitAsync(s, "interest.withdrawn", requestId, current["clientId"].AsBsonBinaryData.ToGuid(), providerId, now, token);
            }
            var count = await interests.CountDocumentsAsync(s, new BsonDocument { ["requestId"] = Id(requestId), ["withdrawnAt"] = new BsonDocument("$exists", false) }, cancellationToken: token);
            return new WithdrawInterestResult(checked((int)count), changed.ModifiedCount > 0, false);
        }, cancellationToken: ct);
    }

    public async Task<InterestEntry[]> ActiveAsync(Guid requestId, Guid clientId, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var filter = new BsonDocument { ["requestId"] = Id(requestId), ["clientId"] = Id(clientId), ["withdrawnAt"] = new BsonDocument("$exists", false) };
        var documents = await interests.Find(filter).Sort(new BsonDocument { ["createdAt"] = -1, ["providerId"] = 1 }).Limit(1000).ToListAsync(ct);
        return documents.Select(d => new InterestEntry(d["providerId"].AsBsonBinaryData.ToGuid(), d.GetValue("note", "").AsString, d["createdAt"].ToUniversalTime())).ToArray();
    }

    public async Task<(Guid? ConversationId, string? OrderStatus, bool CanOrder)> ActionsAsync(Guid requestId, Guid clientId, Guid providerId, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var activeFilter = new BsonDocument { ["requestId"] = Id(requestId), ["providerId"] = Id(providerId), ["status"] = new BsonDocument("$in", new BsonArray { "pending", "accepted" }) };
        var order = await orders.Find(activeFilter).Sort(new BsonDocument("createdAt", -1)).FirstOrDefaultAsync(ct);
        var conversation = await conversations.Find(new BsonDocument { ["clientId"] = Id(clientId), ["providerId"] = Id(providerId), ["status"] = new BsonDocument("$ne", "closed") }).FirstOrDefaultAsync(ct);
        Guid? conversationId = null;
        if (conversation != null)
        {
            var value = conversation.GetValue("conversationId", conversation.GetValue("_id", BsonNull.Value));
            if (value.BsonType == BsonType.Binary) conversationId = value.AsBsonBinaryData.ToGuid();
            else if (value.BsonType == BsonType.String && Guid.TryParse(value.AsString, out var parsed)) conversationId = parsed;
        }
        return (conversationId, order?.GetValue("status").AsString, order == null);
    }

    private Task EmitAsync(IClientSessionHandle session, string type, Guid requestId, Guid clientId, Guid providerId, DateTime occurredAt, CancellationToken ct) =>
        outbox.InsertOneAsync(session, new OutboxMessage
        {
            MessageId = Guid.CreateVersion7(), Type = type,
            Payload = System.Text.Json.JsonSerializer.Serialize(new { requestId, clientId, providerId }),
            OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc))
        }, cancellationToken: ct);
}
