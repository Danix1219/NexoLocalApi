using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;
using Nexo.Discovery.Application.Directory;

namespace Nexo.Discovery.Infrastructure.Providers;

public sealed class MongoProviderStore([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : IProviderStore, IExpandedDirectoryProfiles
{
    private readonly IMongoCollection<BsonDocument> profiles = database.GetCollection<BsonDocument>("provider_profiles");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await profiles.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("accountId"), new CreateIndexOptions { Unique = true, Name = "ux_provider_account" }),
                new(Builders<BsonDocument>.IndexKeys.Geo2DSphere("locationGeo"), new CreateIndexOptions { Name = "ix_provider_location" })
            ], ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);
    private static BsonDocument Data(ProviderProfile p) => BsonDocument.Parse(JsonSerializer.Serialize(p, ProviderService.Json));
    private static BsonDocument Geo(ProviderPoint p) => new() { ["type"] = "Point", ["coordinates"] = new BsonArray { p.Lng, p.Lat } };
    private static StoredProvider Read(BsonDocument doc) => new(
        doc["accountId"].AsBsonBinaryData.ToGuid(),
        JsonSerializer.Deserialize<ProviderProfile>(doc["data"].AsBsonDocument.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }), ProviderService.Json)!,
        doc.GetValue("revision", 0).ToInt64(), doc.GetValue("creationKey", "").AsString, doc.GetValue("creationHash", "").AsString,
        doc.GetValue("published", false).AsBoolean, doc.TryGetValue("distanceMeters", out var distance) ? distance.ToDouble() / 1000 : null,
        doc.TryGetValue("creationProfile", out var original) ? JsonSerializer.Deserialize<ProviderProfile>(original.AsBsonDocument.ToJson(), ProviderService.Json) : null,
        doc.GetValue("views", 0L).ToInt64());
    public async Task<StoredProvider?> OwnAsync(Guid accountId, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var doc = await profiles.Find(new BsonDocument("accountId", Id(accountId))).FirstOrDefaultAsync(ct);
        return doc == null ? null : Read(doc);
    }
    public async Task<IReadOnlyList<StoredProvider>> PublicAsync(Guid[] ids, ProviderPoint? from, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var filter = new BsonDocument { ["_id"] = new BsonDocument("$in", new BsonArray(ids.Select(id => (BsonValue)Id(id)))), ["published"] = true };
        var pipeline = new List<BsonDocument>();
        if (from != null)
            pipeline.Add(new("$geoNear", new BsonDocument { ["near"] = Geo(from), ["distanceField"] = "distanceMeters", ["key"] = "locationGeo", ["spherical"] = true, ["query"] = filter }));
        else pipeline.Add(new("$match", filter));
        // Home coordinates are removed by Mongo itself, not after materializing the document.
        pipeline.Add(new("$set", new BsonDocument("data.location", new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$in", new BsonArray { "$data.attention", new BsonArray { "shop", "both" } }), "$data.location", "$$REMOVE"
        }))));
        pipeline.Add(new("$project", new BsonDocument { ["locationGeo"] = 0, ["events"] = 0, ["creationKey"] = 0, ["creationHash"] = 0, ["creationProfile"] = 0, ["views"] = 0 }));
        var docs = await profiles.Aggregate<BsonDocument>(pipeline).ToListAsync(ct);
        return docs.Select(Read).ToArray();
    }
    public async Task<bool> InsertAsync(StoredProvider stored, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var doc = new BsonDocument
        {
            ["_id"] = Id(stored.Profile.ProviderId), ["accountId"] = Id(stored.AccountId), ["data"] = Data(stored.Profile),
            ["locationGeo"] = Geo(stored.Profile.Location!), ["revision"] = 0L, ["published"] = true,
            ["creationKey"] = stored.CreationKey, ["creationHash"] = stored.CreationHash, ["creationProfile"] = Data(stored.Profile), ["views"] = 0L,
            ["events"] = new BsonArray { Event(stored.Profile, "provider.created", "all") }
        };
        try { await profiles.InsertOneAsync(doc, cancellationToken: ct); return true; }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey) { return false; }
    }
    public Task<IReadOnlyList<StoredProvider>> CandidatesAsync(ProviderPoint? point, string? category, CancellationToken ct) => DirectoryAsync(point, category, false, ct);
    public Task<IReadOnlyList<StoredProvider>> ExpandedAsync(ProviderPoint point, string? category, CancellationToken ct) => DirectoryAsync(point, category, true, ct);
    private async Task<IReadOnlyList<StoredProvider>> DirectoryAsync(ProviderPoint? point, string? category, bool expanded, CancellationToken ct)
    {
        try { return await DirectoryCoreAsync(point, category, expanded, ct); }
        catch (Exception e) when (e is MongoException or TimeoutException) { throw new DirectoryUnavailableException(); }
    }
    private async Task<IReadOnlyList<StoredProvider>> DirectoryCoreAsync(ProviderPoint? point, string? category, bool expanded, CancellationToken ct)
    {
        await EnsureAsync(ct);
        var filter = new BsonDocument("published", true);
        if (category != null) filter["data.categories"] = category;
        var pipeline = new List<BsonDocument>();
        if (point != null)
        {
            var near = new BsonDocument { ["near"] = Geo(point), ["key"] = "locationGeo", ["distanceField"] = "distanceMeters", ["spherical"] = true, ["query"] = filter };
            if (!expanded) near["maxDistance"] = 20000;
            pipeline.Add(new("$geoNear", near));
        }
        else pipeline.Add(new("$match", filter));
        pipeline.Add(new("$limit", 1001));
        // Inclusion projection: neither exact home point, creation payload, owner name nor contacts leave Mongo.
        var projection = new BsonDocument { ["accountId"] = 1, ["published"] = 1, ["distanceMeters"] = 1 };
        foreach (var field in new[] { "providerId", "kind", "name", "bio", "activity", "trades", "categories", "specialties", "attention", "radiusKm", "publicZone", "schedule", "visit", "prices", "photoUrl", "recs", "ratingAverage", "ratingCount", "createdAt", "updatedAt" })
            projection["data." + field] = 1;
        projection["data.location"] = new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$in", new BsonArray { "$data.attention", new BsonArray { "shop", "both" } }), "$data.location", "$$REMOVE"
        });
        pipeline.Add(new("$project", projection));
        var docs = await profiles.Aggregate<BsonDocument>(pipeline, new AggregateOptions { MaxTime = TimeSpan.FromSeconds(5) }).ToListAsync(ct);
        if (docs.Count > 1000) throw new DirectoryUnavailableException();
        return docs.Select(Read).ToArray();
    }
    public async Task<bool> PatchAsync(StoredProvider expected, ProviderProfile profile, string section, string[] fields, CancellationToken ct)
    {
        var data = Data(profile);
        var set = new BsonDocument("data.updatedAt", data["updatedAt"]);
        foreach (var field in fields) set["data." + field] = data[field];
        if (section == "zone") set["locationGeo"] = Geo(profile.Location!);
        var filter = new BsonDocument { ["accountId"] = Id(expected.AccountId), ["revision"] = expected.Revision, ["events.255"] = new BsonDocument("$exists", false) };
        var update = new BsonDocument { ["$set"] = set, ["$inc"] = new BsonDocument("revision", 1L), ["$push"] = new BsonDocument("events", Event(profile, "provider.updated", section)) };
        return (await profiles.UpdateOneAsync(filter, update, cancellationToken: ct)).ModifiedCount == 1;
    }
    public async Task RecordViewAsync(Guid providerId, CancellationToken ct) =>
        await profiles.UpdateOneAsync(new BsonDocument { ["_id"] = Id(providerId), ["published"] = true }, new BsonDocument("$inc", new BsonDocument("views", 1L)), cancellationToken: ct);
    private static BsonDocument Event(ProviderProfile p, string type, string section) => new()
    {
        ["id"] = Id(Guid.CreateVersion7()), ["type"] = type, ["providerId"] = Id(p.ProviderId), ["section"] = section,
        ["occurredAt"] = DateTime.UtcNow, ["categories"] = new BsonArray(p.Categories), ["publicZone"] = Data(p)["publicZone"]
    };
}
