using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Notifications;

namespace Nexo.Engagement.Infrastructure.Notifications;

/// <summary>`notifications`, written only by the outbox handler. TTL 90 days (data minimisation).</summary>
public sealed class MongoNotificationStore([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database) : INotificationStore
{
    private readonly IMongoCollection<BsonDocument> rows = database.GetCollection<BsonDocument>("notifications");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);

    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await rows.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("accountId").Ascending("type").Ascending("groupKey"), new CreateIndexOptions { Unique = true, Name = "ux_notification_group" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("accountId").Descending("createdAt").Descending("_id"), new CreateIndexOptions { Name = "ix_notification_account_created" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("createdAt"), new CreateIndexOptions { Name = "ttl_notification_90d", ExpireAfter = TimeSpan.FromDays(90) })
            ], ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }

    public async Task UpsertAsync(NotificationDraft d, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var key = new BsonDocument { ["accountId"] = Id(d.AccountId), ["type"] = d.Type, ["groupKey"] = d.GroupKey };
        var update = new BsonDocument
        {
            ["$set"] = new BsonDocument
            {
                ["title"] = d.Title,
                ["body"] = d.Body,
                ["deepLink"] = d.DeepLink == null ? BsonNull.Value : d.DeepLink,
                ["actionLabel"] = d.ActionLabel == null ? BsonNull.Value : d.ActionLabel,
                ["icon"] = d.Icon,
                ["readAt"] = BsonNull.Value,
                ["createdAt"] = d.At
            },
            ["$setOnInsert"] = new BsonDocument("_id", Id(Guid.CreateVersion7()))
        };
        try { await rows.UpdateOneAsync(key, update, new UpdateOptions { IsUpsert = true }, ct); }
        // Two workers upserting the same group: the loser retries as a plain update.
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            update.Remove("$setOnInsert");
            await rows.UpdateOneAsync(key, update, cancellationToken: ct);
        }
    }

    public async Task<StoredNotification[]> ListAsync(Guid account, bool unreadOnly, (DateTime At, Guid Id)? before, int limit, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var filter = new BsonDocument("accountId", Id(account));
        if (unreadOnly) filter["readAt"] = BsonNull.Value;
        if (before is { } b)
            filter["$or"] = new BsonArray
            {
                new BsonDocument("createdAt", new BsonDocument("$lt", b.At)),
                new BsonDocument { ["createdAt"] = b.At, ["_id"] = new BsonDocument("$lt", Id(b.Id)) }
            };
        var docs = await rows.Find(filter).Sort(new BsonDocument { ["createdAt"] = -1, ["_id"] = -1 }).Limit(limit).ToListAsync(ct);
        return docs.Select(d => new StoredNotification(d["_id"].AsBsonBinaryData.ToGuid(), d["type"].AsString, d["title"].AsString, d["body"].AsString,
            d["deepLink"].IsBsonNull ? null : d["deepLink"].AsString, d["actionLabel"].IsBsonNull ? null : d["actionLabel"].AsString,
            d["icon"].AsString, d["readAt"].IsBsonNull ? null : d["readAt"].ToUniversalTime(), d["createdAt"].ToUniversalTime())).ToArray();
    }

    public async Task<int> UnreadAsync(Guid account, CancellationToken ct)
    {
        await EnsureAsync(ct);
        return (int)await rows.CountDocumentsAsync(new BsonDocument { ["accountId"] = Id(account), ["readAt"] = BsonNull.Value }, cancellationToken: ct);
    }

    public async Task<int> MarkReadAsync(Guid account, Guid[]? ids, DateTime now, CancellationToken ct)
    {
        await EnsureAsync(ct);
        // accountId always in the filter: a foreign id is silently ignored, which confirms nothing.
        var filter = new BsonDocument { ["accountId"] = Id(account), ["readAt"] = BsonNull.Value };
        if (ids != null) filter["_id"] = new BsonDocument("$in", new BsonArray(ids.Select(Id)));
        return (int)(await rows.UpdateManyAsync(filter, new BsonDocument("$set", new BsonDocument("readAt", now)), cancellationToken: ct)).ModifiedCount;
    }
}
