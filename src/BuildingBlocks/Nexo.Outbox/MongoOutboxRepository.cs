using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Nexo.Outbox;

/// <summary>Coleccion `outbox` en la base del dominio; el reclamo es un update atomico con filtro.</summary>
public sealed class MongoOutboxRepository : IOutboxRepository
{
    private const string CollectionName = "outbox";

    static MongoOutboxRepository()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(OutboxMessage)))
        {
            BsonClassMap.RegisterClassMap<OutboxMessage>(map =>
            {
                map.AutoMap();
                map.MapIdMember(message => message.MessageId);
                map.SetIgnoreExtraElements(true);
            });
        }
    }

    private readonly IMongoCollection<OutboxMessage> _collection;

    public MongoOutboxRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<OutboxMessage>(CollectionName);
    }

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(message, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int max, CancellationToken cancellationToken = default)
    {
        var staleBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var filter = Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(message => message.ProcessedAt, null),
            Builders<OutboxMessage>.Filter.Or(
                Builders<OutboxMessage>.Filter.Eq(message => message.ProcessingStartedAt, null),
                Builders<OutboxMessage>.Filter.Lt(message => message.ProcessingStartedAt, staleBefore)));

        return await _collection
            .Find(filter)
            .SortBy(message => message.OccurredAt)
            .Limit(max)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryBeginProcessingAsync(
        Guid messageId,
        DateTimeOffset now,
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        var staleBefore = now.Subtract(staleAfter);
        var filter = Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(message => message.MessageId, messageId),
            Builders<OutboxMessage>.Filter.Eq(message => message.ProcessedAt, null),
            Builders<OutboxMessage>.Filter.Or(
                Builders<OutboxMessage>.Filter.Eq(message => message.ProcessingStartedAt, null),
                Builders<OutboxMessage>.Filter.Lt(message => message.ProcessingStartedAt, staleBefore)));

        var update = Builders<OutboxMessage>.Update.Set(message => message.ProcessingStartedAt, now);
        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task MarkProcessedAsync(Guid messageId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxMessage>.Update
            .Set(message => message.ProcessedAt, now)
            .Set(message => message.ProcessingStartedAt, null)
            .Set(message => message.LastError, null);

        await _collection.UpdateOneAsync(
            Builders<OutboxMessage>.Filter.Eq(message => message.MessageId, messageId),
            update,
            cancellationToken: cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxMessage>.Update
            .Set(message => message.ProcessingStartedAt, null)
            .Set(message => message.LastError, error)
            .Inc(message => message.Attempts, 1);

        await _collection.UpdateOneAsync(
            Builders<OutboxMessage>.Filter.Eq(message => message.MessageId, messageId),
            update,
            cancellationToken: cancellationToken);
    }
}
