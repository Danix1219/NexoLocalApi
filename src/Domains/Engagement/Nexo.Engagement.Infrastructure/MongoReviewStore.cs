using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Reviews;
using Nexo.Outbox;

namespace Nexo.Engagement.Infrastructure;

/// <summary>Opiniones de clientes. Una por pedido, garantizado por índice único; la fila y su outbox comparten transacción.</summary>
public sealed class MongoReviewStore([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database) : IReviewStore
{
    private readonly IMongoCollection<BsonDocument> reviews = database.GetCollection<BsonDocument>("reviews");
    private readonly IMongoCollection<OutboxMessage> outbox = database.GetCollection<OutboxMessage>("outbox");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);
    private static readonly BsonDocument Approved = new("moderation.status", "approved");

    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await reviews.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("orderId"), new CreateIndexOptions { Unique = true, Sparse = true, Name = "ux_review_order" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("providerId").Descending("_id"), new CreateIndexOptions { Name = "ix_review_provider_cursor" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Descending("createdAt"), new CreateIndexOptions { Name = "ix_review_client_created" })
            ], ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }

    private async Task<T> Run<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try { await EnsureAsync(ct); return await action(); }
        catch (MongoException e) { throw new ReviewUnavailableException(e); }
        catch (TimeoutException e) { throw new ReviewUnavailableException(e); }
    }

    public Task<StoredReview?> ByOrderAsync(Guid orderId, CancellationToken ct) => Run(async () =>
    {
        var doc = await reviews.Find(new BsonDocument("orderId", Id(orderId))).FirstOrDefaultAsync(ct);
        return doc == null ? null : Read(doc);
    }, ct);

    public Task<int> CountSinceAsync(Guid clientId, DateTime since, CancellationToken ct) => Run(async () =>
        (int)await reviews.CountDocumentsAsync(new BsonDocument { ["clientId"] = Id(clientId), ["createdAt"] = new BsonDocument("$gte", since) }, cancellationToken: ct), ct);

    public Task<bool> InsertAsync(StoredReview r, CancellationToken ct) => Run(async () =>
    {
        try
        {
            using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
            return await session.WithTransactionAsync<bool>(async (s, token) =>
            {
                await reviews.InsertOneAsync(s, new BsonDocument
                {
                    ["_id"] = Id(r.Id),
                    ["orderId"] = Id(r.OrderId),
                    ["providerId"] = Id(r.ProviderId),
                    ["clientId"] = Id(r.ClientId),
                    ["idempotencyKey"] = Id(r.IdempotencyKey),
                    ["category"] = r.Category,
                    ["stars"] = r.Stars,
                    ["text"] = r.Text == null ? BsonNull.Value : r.Text,
                    ["recommend"] = r.Recommend,
                    ["moderation"] = new BsonDocument { ["status"] = "approved", ["checkedAt"] = r.CreatedAt },
                    ["createdAt"] = r.CreatedAt
                }, cancellationToken: token);
                await outbox.InsertOneAsync(s, new OutboxMessage
                {
                    MessageId = Guid.CreateVersion7(),
                    Type = "review.created",
                    Payload = JsonSerializer.Serialize(new { reviewId = r.Id, providerId = r.ProviderId, stars = r.Stars, recommend = r.Recommend }),
                    OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc))
                }, cancellationToken: token);
                return true;
            }, cancellationToken: ct);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { return false; }
        catch (MongoWriteException e) { throw new ReviewUnavailableException(e); }
    }, ct);

    public Task<ReviewSummary> SummaryAsync(Guid providerId, CancellationToken ct) => Run(async () =>
    {
        var match = new BsonDocument(Approved) { ["providerId"] = Id(providerId) };
        var groups = await reviews.Aggregate().Match(match)
            .Group(new BsonDocument { ["_id"] = "$stars", ["n"] = new BsonDocument("$sum", 1), ["rec"] = new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { "$recommend", 1, 0 })) })
            .ToListAsync(ct);
        var breakdown = Enumerable.Range(1, 5).Reverse().ToDictionary(s => s.ToString(), _ => 0);
        int count = 0, rec = 0, total = 0;
        foreach (var g in groups)
        {
            var stars = g["_id"].ToInt32(); var n = g["n"].ToInt32();
            breakdown[stars.ToString()] = n; count += n; total += stars * n; rec += g["rec"].ToInt32();
        }
        return new ReviewSummary(count == 0 ? 0 : Math.Round((double)total / count, 1), count, rec, breakdown);
    }, ct);

    public Task<StoredReview[]> PageAsync(Guid providerId, Guid? before, int limit, CancellationToken ct) => Run(async () =>
    {
        var filter = new BsonDocument(Approved) { ["providerId"] = Id(providerId) };
        if (before != null) filter["_id"] = new BsonDocument("$lt", Id(before.Value));
        var docs = await reviews.Find(filter).Sort(new BsonDocument("_id", -1)).Limit(limit).ToListAsync(ct);
        return docs.Select(Read).ToArray();
    }, ct);

    private static StoredReview Read(BsonDocument d) => new(d["_id"].AsBsonBinaryData.ToGuid(), d["orderId"].AsBsonBinaryData.ToGuid(),
        d["providerId"].AsBsonBinaryData.ToGuid(), d["clientId"].AsBsonBinaryData.ToGuid(), d["idempotencyKey"].AsBsonBinaryData.ToGuid(),
        d.GetValue("category", "").AsString, d["stars"].ToInt32(), d.GetValue("text", BsonNull.Value).IsBsonNull ? null : d["text"].AsString,
        d.GetValue("recommend", false).ToBoolean(), d["createdAt"].ToUniversalTime());
}
