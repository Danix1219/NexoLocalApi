using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Works;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Infrastructure.Works;

public sealed class MongoFeedStore([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : IFeedStore
{
    private readonly IMongoCollection<BsonDocument> works = database.GetCollection<BsonDocument>("works");
    private readonly IMongoCollection<BsonDocument> recommendations = database.GetCollection<BsonDocument>("recommendations");
    private readonly IMongoCollection<BsonDocument> saved = database.GetCollection<BsonDocument>("saved");

    private static BsonBinaryData Id(Guid id) => new(id, GuidRepresentation.Standard);
    private static BsonDocument Geo(ProviderPoint point) => new()
    {
        ["type"] = "Point",
        ["coordinates"] = new BsonArray { point.Lng, point.Lat }
    };
    private static Work Read(BsonDocument document) => JsonSerializer.Deserialize<Work>(document["data"].AsBsonDocument.ToJson(
        new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }), ProviderService.Json)!;

    public async Task<WorkPage> ReadAsync(ProviderPoint point, Guid[] providerIds, Guid? after, int limit, Guid? viewer, CancellationToken ct)
    {
        try
        {
            var anchor = after?.ToString("D");
            var nearQuery = new BsonDocument("moderation.status", "approved");
            if (anchor != null) nearQuery["_id"] = new BsonDocument("$lt", anchor);
            var geo = await works.Aggregate<BsonDocument>(new BsonDocument[]
            {
                new BsonDocument("$geoNear", new BsonDocument
                {
                    ["near"] = Geo(point), ["key"] = "publicGeo", ["distanceField"] = "feedDistanceMeters",
                    ["maxDistance"] = 3000, ["spherical"] = true, ["query"] = nearQuery
                }),
                new BsonDocument("$sort", new BsonDocument("_id", -1)),
                new BsonDocument("$limit", limit + 1),
                new BsonDocument("$project", new BsonDocument { ["_id"] = 1, ["providerId"] = 1, ["data"] = 1 })
            }, new AggregateOptions { MaxTime = TimeSpan.FromSeconds(5) }).ToListAsync(ct);

            var byProvider = new List<BsonDocument>();
            if (providerIds.Length > 0)
            {
                var filter = new BsonDocument
                {
                    ["providerId"] = new BsonDocument("$in", new BsonArray(providerIds.Select(id => (BsonValue)Id(id)))),
                    ["moderation.status"] = "approved"
                };
                if (anchor != null) filter["_id"] = new BsonDocument("$lt", anchor);
                byProvider = await works.Find(filter).Sort(new BsonDocument("_id", -1)).Limit(limit + 1)
                    .Project(new BsonDocument { ["_id"] = 1, ["providerId"] = 1, ["data"] = 1 }).ToListAsync(ct);
            }

            var docs = geo.Concat(byProvider)
                .GroupBy(d => d["_id"].AsString, StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderByDescending(d => d["_id"].AsString, StringComparer.Ordinal)
                .Take(limit + 1)
                .ToArray();
            var values = docs.Take(limit).Select(Read).ToArray();
            var savedIds = new HashSet<Guid>();
            var recommendedIds = new HashSet<Guid>();
            if (viewer is { } account && values.Length > 0)
            {
                var ids = new BsonArray(values.Select(w => (BsonValue)Id(w.WorkId)));
                var marks = await recommendations.Find(new BsonDocument
                { ["accountId"] = Id(account), ["workId"] = new BsonDocument("$in", ids) })
                    .Project(new BsonDocument("workId", 1)).ToListAsync(ct);
                foreach (var mark in marks) recommendedIds.Add(mark["workId"].AsBsonBinaryData.ToGuid());
                var saves = await saved.Find(new BsonDocument
                { ["accountId"] = Id(account), ["kind"] = "work", ["targetId"] = new BsonDocument("$in", ids) })
                    .Project(new BsonDocument("targetId", 1)).ToListAsync(ct);
                foreach (var mark in saves) savedIds.Add(mark["targetId"].AsBsonBinaryData.ToGuid());
            }
            return new(values, savedIds, recommendedIds, docs.Length > limit);
        }
        catch (Exception e) when (e is MongoException or TimeoutException or NotSupportedException)
        {
            throw new FeedUnavailableException();
        }
    }
}
