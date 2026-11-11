using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application;

namespace Nexo.Discovery.Infrastructure;

public sealed class MongoSearchLocationStore(
    [FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : ISearchLocationStore
{
    public Task SaveAsync(Guid accountId, SearchLocation location, CancellationToken ct) =>
        database.GetCollection<BsonDocument>("user_profiles").UpdateOneAsync(
            new BsonDocument("_id", new BsonBinaryData(accountId, GuidRepresentation.Standard)),
            new BsonDocument("$set", new BsonDocument
            {
                ["searchLocation"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { location.Lng, location.Lat } },
                ["searchZoneLabel"] = location.Label,
                ["searchRadiusKm"] = location.RadiusKm,
                ["searchLocationSource"] = location.Source,
                ["updatedAt"] = DateTime.UtcNow
            }), new UpdateOptions { IsUpsert = true }, ct);

    public async Task<SearchLocation?> GetAsync(Guid accountId, CancellationToken ct)
    {
        var doc = await database.GetCollection<BsonDocument>("user_profiles").Find(
            new BsonDocument("_id", new BsonBinaryData(accountId, GuidRepresentation.Standard))).FirstOrDefaultAsync(ct);
        if (doc is null || !doc.TryGetValue("searchLocation", out var raw) || !raw.IsBsonDocument ||
            !raw.AsBsonDocument.TryGetValue("coordinates", out var coords) || !coords.IsBsonArray || coords.AsBsonArray.Count != 2) return null;
        return new(coords[1].ToDouble(), coords[0].ToDouble(), doc.GetValue("searchRadiusKm", 10).ToInt32(),
            doc.GetValue("searchLocationSource", "map").AsString, doc.GetValue("searchZoneLabel", "").AsString);
    }
}
