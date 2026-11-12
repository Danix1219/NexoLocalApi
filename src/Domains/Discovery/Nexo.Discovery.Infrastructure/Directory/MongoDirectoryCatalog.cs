using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;

namespace Nexo.Discovery.Infrastructure.Directory;

public sealed class MongoDirectoryCatalog([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : IDirectoryCatalog
{
    public async Task<CatalogResponse> ReadAsync(CancellationToken ct)
    {
        try { return await ReadCoreAsync(ct); }
        catch (Exception e) when (e is MongoException or TimeoutException) { throw new DirectoryUnavailableException(); }
    }
    private async Task<CatalogResponse> ReadCoreAsync(CancellationToken ct)
    {
        var collection = database.GetCollection<BsonDocument>("categories");
        var seed = DirectoryCatalog.Current;
        var order = 0;
        // Only the seven public seed documents; no profile or user is created.
        foreach (var category in seed.Categories)
        {
            var doc = BsonDocument.Parse(JsonSerializer.Serialize(category, ProviderService.Json));
            doc["order"] = order++;
            doc["version"] = seed.Version;
            await collection.UpdateOneAsync(new BsonDocument("_id", category.Slug), new BsonDocument("$setOnInsert", doc),
                new UpdateOptions { IsUpsert = true }, ct);
        }
        var docs = await collection.Find(new BsonDocument("_id", new BsonDocument("$in", new BsonArray(seed.Categories.Select(c => c.Slug)))))
            .Sort(new BsonDocument("order", 1)).Limit(7).ToListAsync(ct);
        if (docs.Count != 7 || docs.Any(d => d.GetValue("version", "").AsString != seed.Version)) throw new DirectoryUnavailableException();
        // The deployment's versioned public schema is authoritative; private seed fields cannot leak.
        return seed;
    }
}
