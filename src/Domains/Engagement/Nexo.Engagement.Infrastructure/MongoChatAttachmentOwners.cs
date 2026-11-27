using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Conversations;

namespace Nexo.Engagement.Infrastructure;

/// <summary>Colección `chat_attachments`: solo identificadores, nunca el contenido ni su clave.</summary>
public sealed class MongoChatAttachmentOwners([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database)
    : IChatAttachmentOwners
{
    private readonly IMongoCollection<BsonDocument> records = database.GetCollection<BsonDocument>("chat_attachments");

    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);
    private static string Key(ChatAttachmentRef a) => $"{a.ConversationId:D}/{a.AttachmentId:D}";

    public async Task<bool> ClaimAsync(ChatAttachmentRef attachment, Guid uploader, DateTime now, CancellationToken ct)
    {
        await records.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("uploaderId"),
            new CreateIndexOptions { Name = "ix_chat_attachment_uploader" }), cancellationToken: ct);
        try
        {
            await records.InsertOneAsync(new BsonDocument
            {
                ["_id"] = Key(attachment),
                ["conversationId"] = Id(attachment.ConversationId),
                ["attachmentId"] = Id(attachment.AttachmentId),
                ["uploaderId"] = Id(uploader),
                ["createdAt"] = now
            }, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Reintento de quien ya lo había anotado.
            return await records.Find(new BsonDocument { ["_id"] = Key(attachment), ["uploaderId"] = Id(uploader) }).AnyAsync(ct);
        }
    }

    public async Task<IReadOnlyList<ChatAttachmentRef>> OfAsync(Guid uploader, CancellationToken ct) =>
        (await records.Find(new BsonDocument("uploaderId", Id(uploader))).ToListAsync(ct))
            .Select(d => new ChatAttachmentRef(d["conversationId"].AsBsonBinaryData.ToGuid(), d["attachmentId"].AsBsonBinaryData.ToGuid()))
            .ToList();

    public Task ForgetAsync(ChatAttachmentRef attachment, CancellationToken ct) =>
        records.DeleteOneAsync(new BsonDocument("_id", Key(attachment)), ct);
}
