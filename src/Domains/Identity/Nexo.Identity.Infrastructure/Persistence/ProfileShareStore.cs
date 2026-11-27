using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Infrastructure.Persistence;

public sealed class MongoProfileShareStore(IdentityCollections accounts,
    [FromKeyedServices(NexoDatabaseKeys.Identity)] IMongoDatabase database) : IProfileShareStore
{
    private readonly IMongoCollection<BsonDocument> shares = database.GetCollection<BsonDocument>("profile_shares");
    private static BsonBinaryData Id(Guid id) => new(id, GuidRepresentation.Standard);
    public async Task<Guid> EnableAsync(Guid owner, CancellationToken ct)
    {
        // The random public id is distinct from the private account id. One document per owner.
        await shares.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("shareId"), new CreateIndexOptions { Unique = true }), cancellationToken: ct);
        var doc = await shares.FindOneAndUpdateAsync(new BsonDocument("_id", Id(owner)),
            new BsonDocument("$setOnInsert", new BsonDocument("shareId", Id(Guid.NewGuid()))),
            new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, ct);
        return doc["shareId"].AsBsonBinaryData.ToGuid();
    }
    public async Task<SharedProfile?> ReadAsync(Guid shareId, CancellationToken ct)
    {
        var share = await shares.Find(new BsonDocument("shareId", Id(shareId))).FirstOrDefaultAsync(ct);
        if (share is null) return null;
        var owner = share["_id"].AsBsonBinaryData.ToGuid();
        return await accounts.Accounts.Find(a => a.Id == owner && a.Status == AccountStatus.Active)
            .Project(a => new SharedProfile(a.Name, a.PhotoUrl)).FirstOrDefaultAsync(ct);
    }
    public Task RevokeAsync(Guid owner, CancellationToken ct) => shares.DeleteOneAsync(new BsonDocument("_id", Id(owner)), ct);
}
