using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Safety;
using Nexo.Outbox;

namespace Nexo.Engagement.Infrastructure;

/// <summary>`contact_log` (TTL 1 year) and `reports` (one per reporter and target, reviewed by a human).</summary>
public sealed class MongoContactReportStore([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database) : IContactReportStore
{
    private readonly IMongoCollection<BsonDocument> contacts = database.GetCollection<BsonDocument>("contact_log");
    private readonly IMongoCollection<BsonDocument> reports = database.GetCollection<BsonDocument>("reports");
    private readonly IMongoCollection<BsonDocument> reviews = database.GetCollection<BsonDocument>("reviews");
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
            await contacts.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("actorId").Descending("createdAt"), new CreateIndexOptions { Name = "ix_contact_actor_created" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("createdAt"), new CreateIndexOptions { Name = "ttl_contact_1y", ExpireAfter = TimeSpan.FromDays(365) })
            ], ct);
            await reports.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("reporterId").Ascending("targetType").Ascending("targetId"), new CreateIndexOptions { Unique = true, Name = "ux_report_reporter_target" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("status").Ascending("createdAt"), new CreateIndexOptions { Name = "ix_report_status_created" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("reporterId").Descending("createdAt"), new CreateIndexOptions { Name = "ix_report_reporter_created" })
            ], ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }

    private async Task<T> Run<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try { await EnsureAsync(ct); return await action(); }
        catch (MongoException e) { throw new ContactReportUnavailableException(e); }
        catch (TimeoutException e) { throw new ContactReportUnavailableException(e); }
    }

    public Task<int> PhoneAccessesSinceAsync(Guid actor, DateTime since, CancellationToken ct) => Run(async () =>
        (int)await contacts.CountDocumentsAsync(new BsonDocument { ["actorId"] = Id(actor), ["createdAt"] = new BsonDocument("$gte", since) }, cancellationToken: ct), ct);

    public Task LogPhoneAsync(Guid actor, Guid target, string context, DateTime at, CancellationToken ct) => Run(async () =>
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
        await session.WithTransactionAsync<bool>(async (s, token) =>
        {
            await contacts.InsertOneAsync(s, new BsonDocument
            {
                ["_id"] = Id(Guid.CreateVersion7()),
                ["actorId"] = Id(actor),
                ["targetId"] = Id(target),
                ["channel"] = "copyPhone",
                ["context"] = context,
                ["createdAt"] = at
            }, cancellationToken: token);
            await outbox.InsertOneAsync(s, new OutboxMessage
            {
                MessageId = Guid.CreateVersion7(),
                Type = "contact.phone_copied",
                Payload = JsonSerializer.Serialize(new { actorId = actor, targetId = target }),
                OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(at, DateTimeKind.Utc))
            }, cancellationToken: token);
            return true;
        }, cancellationToken: ct);
        return true;
    }, ct);

    public Task<int> ReportsSinceAsync(Guid reporter, DateTime since, CancellationToken ct) => Run(async () =>
        (int)await reports.CountDocumentsAsync(new BsonDocument { ["reporterId"] = Id(reporter), ["createdAt"] = new BsonDocument("$gte", since) }, cancellationToken: ct), ct);

    public Task<StoredReport?> ReportOfAsync(Guid reporter, string targetType, Guid targetId, CancellationToken ct) => Run(async () =>
    {
        var d = await reports.Find(new BsonDocument { ["reporterId"] = Id(reporter), ["targetType"] = targetType, ["targetId"] = Id(targetId) }).FirstOrDefaultAsync(ct);
        return d == null ? null : new StoredReport(d["_id"].AsBsonBinaryData.ToGuid(), reporter, targetType, targetId,
            d["idempotencyKey"].AsBsonBinaryData.ToGuid(), d["createdAt"].ToUniversalTime());
    }, ct);

    public Task<bool> InsertReportAsync(StoredReport r, string category, string reason, bool? flagged, Guid? evidence, CancellationToken ct) => Run(async () =>
    {
        try
        {
            using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
            return await session.WithTransactionAsync<bool>(async (s, token) =>
            {
                await reports.InsertOneAsync(s, new BsonDocument
                {
                    ["_id"] = Id(r.Id),
                    ["reporterId"] = Id(r.ReporterId),
                    ["targetType"] = r.TargetType,
                    ["targetId"] = Id(r.TargetId),
                    ["idempotencyKey"] = Id(r.IdempotencyKey),
                    ["category"] = category,
                    ["reason"] = reason,
                    ["reasonFlagged"] = flagged is { } f ? f : BsonNull.Value,
                    ["evidenceConversationId"] = evidence is { } e ? Id(e) : BsonNull.Value,
                    ["status"] = "pending",
                    ["resolution"] = BsonNull.Value,
                    ["reviewedBy"] = BsonNull.Value,
                    ["reviewedAt"] = BsonNull.Value,
                    ["createdAt"] = r.CreatedAt
                }, cancellationToken: token);
                await outbox.InsertOneAsync(s, new OutboxMessage
                {
                    MessageId = Guid.CreateVersion7(),
                    Type = "report.created",
                    Payload = JsonSerializer.Serialize(new { reportId = r.Id, targetType = r.TargetType, targetId = r.TargetId, category }),
                    OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc))
                }, cancellationToken: token);
                return true;
            }, cancellationToken: ct);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { return false; }
    }, ct);

    public Task<Guid?> ReviewAuthorAsync(Guid reviewId, CancellationToken ct) => Run(async () =>
    {
        var d = await reviews.Find(new BsonDocument("_id", Id(reviewId))).Project(new BsonDocument("clientId", 1)).FirstOrDefaultAsync(ct);
        return d == null ? (Guid?)null : d["clientId"].AsBsonBinaryData.ToGuid();
    }, ct);
}
