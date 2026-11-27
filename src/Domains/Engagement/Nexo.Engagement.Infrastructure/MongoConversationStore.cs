using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Conversations;
using Nexo.Outbox;

namespace Nexo.Engagement.Infrastructure;

/// <summary>Chat persistence. The inbox reads only `conversations` (denormalized preview/unread); messages are paged by UUID v7 `_id`.</summary>
public sealed class MongoConversationStore([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database) : IConversationStore
{
    public async Task<bool> EnableEncryptionAsync(Guid id, Guid account, CancellationToken ct)
    {
        var filter = new BsonDocument { ["_id"] = Id(id), ["blockedBy"] = new BsonDocument("$size", 0) };
        filter.AddRange(Participant(account));
        var result = await conversations.UpdateOneAsync(filter,
            new BsonDocument("$set", new BsonDocument { ["encrypted"] = true, ["lastPreview"] = "Conversación cifrada" }), cancellationToken: ct);
        return result.MatchedCount == 1;
    }
    private readonly IMongoCollection<BsonDocument> conversations = database.GetCollection<BsonDocument>("conversations");
    private readonly IMongoCollection<BsonDocument> messages = database.GetCollection<BsonDocument>("messages");
    private readonly IMongoCollection<BsonDocument> orders = database.GetCollection<BsonDocument>("orders");
    private readonly IMongoCollection<OutboxMessage> outbox = database.GetCollection<OutboxMessage>("outbox");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);
    private static BsonDocument Participant(Guid account) =>
        new("$or", new BsonArray { new BsonDocument("clientId", Id(account)), new BsonDocument("providerAccountId", Id(account)) });

    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await conversations.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Ascending("providerId"), new CreateIndexOptions { Unique = true, Name = "ux_conversation_pair" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Descending("sortAt").Descending("_id"), new CreateIndexOptions { Name = "ix_conversation_client_inbox" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("providerAccountId").Descending("sortAt").Descending("_id"), new CreateIndexOptions { Name = "ix_conversation_provider_inbox" })
            ], ct);
            await messages.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("conversationId").Descending("_id"), new CreateIndexOptions { Name = "ix_message_conversation" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("senderId").Ascending("idempotencyKey"), new CreateIndexOptions { Unique = true, Name = "ux_message_sender_key" })
            ], ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }

    private async Task<T> Run<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try { await EnsureAsync(ct); return await action(); }
        catch (MongoException e) { throw new ConversationUnavailableException(e); }
        catch (TimeoutException e) { throw new ConversationUnavailableException(e); }
    }

    public Task<(Conversation Conversation, bool Created)> OpenAsync(Guid clientId, Guid providerId, Guid providerAccountId, Guid? requestId, DateTime now, CancellationToken ct) => Run(async () =>
    {
        var newId = Guid.CreateVersion7();
        var pair = new BsonDocument { ["clientId"] = Id(clientId), ["providerId"] = Id(providerId) };
        BsonDocument doc;
        try
        {
            doc = await conversations.FindOneAndUpdateAsync(pair, new BsonDocument("$setOnInsert", new BsonDocument
            {
                ["_id"] = Id(newId),
                ["providerAccountId"] = Id(providerAccountId),
                ["requestId"] = requestId is { } r ? Id(r) : BsonNull.Value,
                ["createdAt"] = now,
                ["sortAt"] = now,
                ["lastMessageAt"] = BsonNull.Value,
                ["lastPreview"] = BsonNull.Value,
                ["unread"] = new BsonDocument { ["client"] = 0, ["provider"] = 0 },
                ["blockedBy"] = new BsonArray()
            }), new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, ct);
        }
        // Two simultaneous upserts on the unique pair: the loser reads the winner.
        catch (MongoCommandException e) when (e.Code == 11000) { doc = await conversations.Find(pair).FirstAsync(ct); }
        var created = doc["_id"].AsBsonBinaryData.ToGuid() == newId;
        if (created)
            await outbox.InsertOneAsync(new OutboxMessage
            {
                MessageId = Guid.CreateVersion7(),
                Type = "conversation.created",
                Payload = JsonSerializer.Serialize(new { conversationId = newId, clientId, providerId }),
                OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Utc))
            }, cancellationToken: ct);
        return (Read(doc), created);
    }, ct);

    public Task<Conversation?> FindAsync(Guid id, Guid account, CancellationToken ct) => Run(async () =>
    {
        var filter = Participant(account); filter["_id"] = Id(id);
        var doc = await conversations.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : Read(doc);
    }, ct);

    public Task<Guid?> PairAsync(Guid clientId, Guid providerId, CancellationToken ct) => Run(async () =>
    {
        var doc = await conversations.Find(new BsonDocument { ["clientId"] = Id(clientId), ["providerId"] = Id(providerId) })
            .Project(new BsonDocument("_id", 1)).FirstOrDefaultAsync(ct);
        return doc == null ? (Guid?)null : doc["_id"].AsBsonBinaryData.ToGuid();
    }, ct);

    public Task<Conversation[]> ListAsync(Guid account, string role, bool unreadOnly, (DateTime At, Guid Id)? before, int limit, CancellationToken ct) => Run(async () =>
    {
        var owner = role == "client" ? "clientId" : "providerAccountId";
        var filter = new BsonDocument(owner, Id(account));
        if (unreadOnly) filter["unread." + role] = new BsonDocument("$gt", 0);
        if (before is { } b)
            filter["$or"] = new BsonArray
            {
                new BsonDocument("sortAt", new BsonDocument("$lt", b.At)),
                new BsonDocument { ["sortAt"] = b.At, ["_id"] = new BsonDocument("$lt", Id(b.Id)) }
            };
        var docs = await conversations.Find(filter).Sort(new BsonDocument { ["sortAt"] = -1, ["_id"] = -1 }).Limit(limit).ToListAsync(ct);
        return docs.Select(Read).ToArray();
    }, ct);

    public Task<int> TotalUnreadAsync(Guid account, string role, CancellationToken ct) => Run(async () =>
    {
        var owner = role == "client" ? "clientId" : "providerAccountId";
        var rows = await conversations.Aggregate().Match(new BsonDocument(owner, Id(account)))
            .Group(new BsonDocument { ["_id"] = BsonNull.Value, ["n"] = new BsonDocument("$sum", "$unread." + role) }).ToListAsync(ct);
        return rows.Count == 0 ? 0 : rows[0]["n"].ToInt32();
    }, ct);

    public Task<ChatMessage[]> MessagesAsync(Guid conversationId, Guid? before, Guid? after, int limit, CancellationToken ct) => Run(async () =>
    {
        var filter = new BsonDocument("conversationId", Id(conversationId));
        if (before is { } b) filter["_id"] = new BsonDocument("$lt", Id(b));
        if (after is { } a)
        {
            // Recovery after a disconnect: the oldest ones after the last seen, returned newest first like any page.
            filter["_id"] = new BsonDocument("$gt", Id(a));
            var newer = await messages.Find(filter).Sort(new BsonDocument("_id", 1)).Limit(limit).ToListAsync(ct);
            return newer.Select(ReadMessage).Reverse().ToArray();
        }
        var docs = await messages.Find(filter).Sort(new BsonDocument("_id", -1)).Limit(limit).ToListAsync(ct);
        return docs.Select(ReadMessage).ToArray();
    }, ct);

    public Task<ChatMessage?> SendAsync(Conversation c, ChatMessage m, string preview, Guid key, CancellationToken ct) => Run(async () =>
    {
        var previous = await messages.Find(new BsonDocument { ["senderId"] = Id(m.SenderId), ["idempotencyKey"] = Id(key) }).FirstOrDefaultAsync(ct);
        if (previous != null) return ReadMessage(previous);
        var recipientRole = c.IsClient(m.SenderId) ? "provider" : "client";
        var recipient = c.IsClient(m.SenderId) ? c.ProviderAccountId : c.ClientId;
        try
        {
            using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
            return await session.WithTransactionAsync<ChatMessage?>(async (s, token) =>
            {
                // Block check and counters in the same conditional write as the message.
                var bumped = await conversations.UpdateOneAsync(s,
                    new BsonDocument
                    {
                        ["_id"] = Id(c.Id),
                        ["blockedBy"] = new BsonDocument("$size", 0),
                        ["closedByDeletion.0"] = new BsonDocument("$exists", false),
                        ["encrypted"] = m.Kind == "encrypted" ? BsonBoolean.True : new BsonDocument("$ne", true)
                    },
                    new BsonDocument
                    {
                        ["$set"] = new BsonDocument { ["lastMessageAt"] = m.CreatedAt, ["sortAt"] = m.CreatedAt, ["lastPreview"] = preview },
                        ["$inc"] = new BsonDocument("unread." + recipientRole, 1)
                    }, cancellationToken: token);
                if (bumped.MatchedCount == 0) return null;
                await messages.InsertOneAsync(s, new BsonDocument
                {
                    ["_id"] = Id(m.Id),
                    ["conversationId"] = Id(m.ConversationId),
                    ["senderId"] = Id(m.SenderId),
                    ["idempotencyKey"] = Id(key),
                    ["kind"] = m.Kind,
                    ["text"] = m.Text == null ? BsonNull.Value : m.Text,
                    ["imageUrl"] = m.ImageUrl == null ? BsonNull.Value : m.ImageUrl,
                    ["thumbUrl"] = m.ThumbUrl == null ? BsonNull.Value : m.ThumbUrl,
                    ["w"] = m.Width is { } w ? w : BsonNull.Value,
                    ["h"] = m.Height is { } h ? h : BsonNull.Value,
                    ["createdAt"] = m.CreatedAt,
                    ["readAt"] = BsonNull.Value
                }, cancellationToken: token);
                // Push payload carries ids only, never the text.
                await outbox.InsertOneAsync(s, new OutboxMessage
                {
                    MessageId = Guid.CreateVersion7(),
                    Type = "message.sent",
                    Payload = JsonSerializer.Serialize(new { conversationId = c.Id, messageId = m.Id, senderId = m.SenderId, recipientId = recipient }),
                    OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(m.CreatedAt, DateTimeKind.Utc))
                }, cancellationToken: token);
                return m;
            }, cancellationToken: ct);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return ReadMessage(await messages.Find(new BsonDocument { ["senderId"] = Id(m.SenderId), ["idempotencyKey"] = Id(key) }).FirstAsync(ct));
        }
    }, ct);

    public Task<ChatMessage?> MutateAsync(Conversation c, Guid author, Guid target, string? encryptedBody,
        Guid operation, DateTime now, CancellationToken ct) => Run(async () =>
    {
        // Ownership and conversation are part of the write filter, never client claims.
        var filter = new BsonDocument { ["_id"] = Id(target), ["conversationId"] = Id(c.Id), ["senderId"] = Id(author) };
        var previous = await messages.Find(filter).FirstOrDefaultAsync(ct);
        if (previous is null || previous["kind"] == "system") return null;
        if (previous["kind"] == "deleted") return encryptedBody is null ? ReadMessage(previous) : null;
        // A retry of an older revision must not overwrite a more recent edit.
        if (previous.GetValue("revisionOperations", new BsonArray()).AsBsonArray.Contains(Id(operation))) return ReadMessage(previous);
        if (encryptedBody is not null && previous["kind"] != "encrypted") return null;
        filter["kind"] = new BsonDocument("$nin", new BsonArray { "system", "deleted" });
        var update = new BsonDocument
        {
            ["kind"] = encryptedBody is null ? "deleted" : "encrypted",
            ["text"] = encryptedBody is null ? BsonNull.Value : new BsonString(encryptedBody),
            ["imageUrl"] = BsonNull.Value,
            ["thumbUrl"] = BsonNull.Value,
            ["revisionOperation"] = Id(operation),
            ["editedAt"] = now
        };
        var saved = await messages.FindOneAndUpdateAsync(filter, new BsonDocument
        { ["$set"] = update, ["$addToSet"] = new BsonDocument("revisionOperations", Id(operation)) },
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, ct);
        if (saved is null) return null;
        // A preview must not retain text of a removed/edited legacy message.
        await conversations.UpdateOneAsync(new BsonDocument { ["_id"] = Id(c.Id), ["lastMessageAt"] = previous["createdAt"] },
            new BsonDocument("$set", new BsonDocument("lastPreview", encryptedBody is null ? "Mensaje eliminado" : "Mensaje cifrado")), cancellationToken: ct);
        return ReadMessage(saved);
    }, ct);

    public Task<int> MarkDeliveredAsync(Conversation c, Guid reader, Guid upTo, DateTime now, CancellationToken ct) => Run(async () =>
    {
        var boundary = await messages.Find(new BsonDocument { ["_id"] = Id(upTo), ["conversationId"] = Id(c.Id) }).AnyAsync(ct);
        if (!boundary) return 0;
        var filter = new BsonDocument
        {
            ["conversationId"] = Id(c.Id),
            ["senderId"] = new BsonDocument("$ne", Id(reader)),
            ["_id"] = new BsonDocument("$lte", Id(upTo)),
            ["deliveredAt"] = BsonNull.Value
        };
        var result = await messages.UpdateManyAsync(filter, new BsonDocument("$set", new BsonDocument("deliveredAt", now)), cancellationToken: ct);
        return (int)result.ModifiedCount;
    }, ct);

    public Task<(int Marked, int Unread)> MarkReadAsync(Conversation c, Guid reader, Guid? upTo, DateTime now, CancellationToken ct) => Run(async () =>
    {
        var others = new BsonDocument { ["conversationId"] = Id(c.Id), ["senderId"] = new BsonDocument("$ne", Id(reader)), ["readAt"] = BsonNull.Value };
        var mark = new BsonDocument(others);
        if (upTo is { } u) mark["_id"] = new BsonDocument("$lte", Id(u));
        var marked = await messages.UpdateManyAsync(mark, new BsonDocument("$set", new BsonDocument("readAt", now)), cancellationToken: ct);
        // Unread is recounted, not zeroed: messages that arrived after upTo stay unread.
        var remaining = (int)await messages.CountDocumentsAsync(others, cancellationToken: ct);
        await conversations.UpdateOneAsync(new BsonDocument("_id", Id(c.Id)),
            new BsonDocument("$set", new BsonDocument("unread." + (c.IsClient(reader) ? "client" : "provider"), remaining)), cancellationToken: ct);
        return ((int)marked.ModifiedCount, remaining);
    }, ct);

    public Task<Conversation?> BlockAsync(Guid id, Guid actor, bool blocked, CancellationToken ct) => Run(async () =>
    {
        var filter = Participant(actor); filter["_id"] = Id(id);
        var update = new BsonDocument(blocked ? "$addToSet" : "$pull", new BsonDocument("blockedBy", Id(actor)));
        var doc = await conversations.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, ct);
        if (doc == null) return null;
        await outbox.InsertOneAsync(new OutboxMessage
        {
            MessageId = Guid.CreateVersion7(),
            Type = blocked ? "conversation.blocked" : "conversation.unblocked",
            Payload = JsonSerializer.Serialize(new { conversationId = id, actorId = actor }),
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: ct);
        return Read(doc);
    }, ct);

    public Task<PairOrder[]> PairOrdersAsync((Guid ClientId, Guid ProviderId)[] pairs, string[] statuses, CancellationToken ct) => Run(async () =>
    {
        if (pairs.Length == 0) return [];
        var filter = new BsonDocument
        {
            ["$or"] = new BsonArray(pairs.Select(p => new BsonDocument { ["clientId"] = Id(p.ClientId), ["providerId"] = Id(p.ProviderId) })),
            ["status"] = new BsonDocument("$in", new BsonArray(statuses))
        };
        var docs = await orders.Find(filter).Project(new BsonDocument { ["clientId"] = 1, ["providerId"] = 1, ["status"] = 1 })
            .Sort(new BsonDocument("_id", -1)).ToListAsync(ct);
        return docs.Select(ReadOrder).ToArray();
    }, ct);

    public Task<PairOrder?> ProviderOrderOnRequestAsync(Guid providerAccountId, Guid requestId, CancellationToken ct) => Run(async () =>
    {
        var doc = await orders.Find(new BsonDocument { ["providerAccountId"] = Id(providerAccountId), ["requestId"] = Id(requestId) })
            .Project(new BsonDocument { ["clientId"] = 1, ["providerId"] = 1, ["status"] = 1 }).Sort(new BsonDocument("_id", -1)).FirstOrDefaultAsync(ct);
        return doc == null ? null : ReadOrder(doc);
    }, ct);

    private static PairOrder ReadOrder(BsonDocument d) => new(d["_id"].AsBsonBinaryData.ToGuid(), d["clientId"].AsBsonBinaryData.ToGuid(),
        d["providerId"].AsBsonBinaryData.ToGuid(), d["status"].AsString);

    private static Conversation Read(BsonDocument d) => new(d["_id"].AsBsonBinaryData.ToGuid(), d["clientId"].AsBsonBinaryData.ToGuid(),
        d["providerId"].AsBsonBinaryData.ToGuid(), d["providerAccountId"].AsBsonBinaryData.ToGuid(),
        d.GetValue("requestId", BsonNull.Value).IsBsonNull ? null : d["requestId"].AsBsonBinaryData.ToGuid(),
        d["createdAt"].ToUniversalTime(), d["sortAt"].ToUniversalTime(),
        d.GetValue("lastPreview", BsonNull.Value).IsBsonNull ? null : d["lastPreview"].AsString,
        d.GetValue("lastMessageAt", BsonNull.Value).IsBsonNull ? null : d["lastMessageAt"].ToUniversalTime(),
        d["unread"]["client"].ToInt32(), d["unread"]["provider"].ToInt32(),
        d.GetValue("blockedBy", new BsonArray()).AsBsonArray.Select(x => x.AsBsonBinaryData.ToGuid()).ToArray(), d.GetValue("encrypted", false).AsBoolean);

    private static ChatMessage ReadMessage(BsonDocument d) => new(d["_id"].AsBsonBinaryData.ToGuid(), d["conversationId"].AsBsonBinaryData.ToGuid(),
        d["senderId"].AsBsonBinaryData.ToGuid(), d["kind"].AsString,
        d.GetValue("text", BsonNull.Value).IsBsonNull ? null : d["text"].AsString,
        d.GetValue("imageUrl", BsonNull.Value).IsBsonNull ? null : d["imageUrl"].AsString,
        d.GetValue("thumbUrl", BsonNull.Value).IsBsonNull ? null : d["thumbUrl"].AsString,
        d.GetValue("w", BsonNull.Value).IsBsonNull ? null : d["w"].ToInt32(), d.GetValue("h", BsonNull.Value).IsBsonNull ? null : d["h"].ToInt32(),
        d["createdAt"].ToUniversalTime(), d.GetValue("readAt", BsonNull.Value).IsBsonNull ? null : d["readAt"].ToUniversalTime(),
        d.GetValue("deliveredAt", BsonNull.Value).IsBsonNull ? null : d["deliveredAt"].ToUniversalTime(),
        d.GetValue("editedAt", BsonNull.Value).IsBsonNull ? null : d["editedAt"].ToUniversalTime());
}
