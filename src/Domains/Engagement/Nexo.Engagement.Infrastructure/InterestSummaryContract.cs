using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Contracts;

namespace Nexo.Engagement.Infrastructure;

public sealed class InterestSummaryContract([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database) : IInterestSummaryContract
{
    public async Task<IReadOnlyDictionary<Guid, InterestSummary>> ReadAsync(Guid[] requestIds, Guid? viewerProviderId, CancellationToken ct)
    {
        if (requestIds.Length > 50) throw new ArgumentOutOfRangeException(nameof(requestIds));
        if (requestIds.Length == 0) return new Dictionary<Guid, InterestSummary>();
        var ids = new BsonArray(requestIds.Select(id => (BsonValue)new BsonBinaryData(id, GuidRepresentation.Standard)));
        // withdrawnAt absence is the existing group 07 contract; never read another module's base.
        var rows = await database.GetCollection<BsonDocument>("interests").Aggregate<BsonDocument>(new BsonDocument[]
        {
            new("$match", new BsonDocument { ["requestId"] = new BsonDocument("$in", ids), ["withdrawnAt"] = new BsonDocument("$exists", false) }),
            new("$sort", new BsonDocument { ["createdAt"] = -1, ["_id"] = -1 }),
            new("$group", new BsonDocument { ["_id"] = "$requestId", ["count"] = new BsonDocument("$sum", 1),
                ["preview"] = new BsonDocument("$firstN", new BsonDocument { ["input"] = "$providerId", ["n"] = 4 }),
                ["interested"] = new BsonDocument("$max", viewerProviderId == null ? (BsonValue)0 : new BsonDocument("$cond", new BsonArray
                    { new BsonDocument("$eq", new BsonArray { "$providerId", new BsonBinaryData(viewerProviderId.Value, GuidRepresentation.Standard) }), 1, 0 })) })
        }, new AggregateOptions { MaxTime = TimeSpan.FromSeconds(2) }).ToListAsync(ct);
        var output = requestIds.Distinct().ToDictionary(id => id, _ => new InterestSummary(0, [], false));
        foreach (var row in rows) output[row["_id"].AsBsonBinaryData.ToGuid()] = new(row["count"].ToInt32(),
            row["preview"].AsBsonArray.Select(v => v.AsBsonBinaryData.ToGuid()).ToArray(), row["interested"].ToInt32() == 1);
        return output;
    }
}
