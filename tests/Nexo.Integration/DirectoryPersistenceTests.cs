using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;
using Nexo.Discovery.Infrastructure.Directory;
using Nexo.Discovery.Infrastructure.Providers;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class DirectoryPersistenceTests(InfraFixture infra)
{
    private IMongoDatabase Database() => new MongoClient(infra.MongoConnectionString).GetDatabase("directory_" + Guid.NewGuid().ToString("N"));
    private static StoredProvider Profile(double lat, string cat = "plomeria", string attention = "home") => new(Guid.NewGuid(), new()
    {
        ProviderId = Guid.CreateVersion7(),
        Name = "Prestador real de prueba",
        Categories = [cat],
        Attention = attention,
        Location = new(lat, -99.34),
        RadiusKm = 5,
        PublicZone = new("Zona pública", new(lat + .006, -99.34)),
        Owner = "NOMBRE PRIVADO",
        Web = "https://privado.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    }, 0, "CLAVE PRIVADA", "HASH PRIVADO", true);
    [Fact]
    public async Task Semilla_idempotente_sin_keywords_y_version_publica_estable()
    {
        var db = Database(); var catalog = new MongoDirectoryCatalog(db);
        var first = await catalog.ReadAsync(default); var second = await catalog.ReadAsync(default);
        Assert.Equal(7, first.Categories.Length); Assert.Equal(first.Version, second.Version);
        Assert.Equal(7, await db.GetCollection<BsonDocument>("categories").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        // Changing deployment version requires migration, not silently serving a mixed catalog.
        await db.GetCollection<BsonDocument>("categories").UpdateOneAsync(new BsonDocument("_id", "plomeria"), new BsonDocument("$set", new BsonDocument("version", "old")));
        await Assert.ThrowsAsync<DirectoryUnavailableException>(() => catalog.ReadAsync(default));
    }
    [Fact]
    public async Task GeoNear_filtra_categoria_y_radio_y_Mongo_retira_home_y_datos_de_creacion()
    {
        var db = Database(); var store = new MongoProviderStore(db);
        var home = Profile(20.05); var shop = Profile(20.06, attention: "shop"); var far = Profile(21.05); var other = Profile(20.05, "pintura");
        foreach (var p in new[] { home, shop, far, other }) Assert.True(await store.InsertAsync(p, default));
        var values = await store.CandidatesAsync(new(20.05, -99.34), "plomeria", default);
        Assert.Equal(2, values.Count);
        var first = values.Single(v => v.AccountId == home.AccountId);
        Assert.Null(first.Profile.Location); Assert.Null(first.Profile.Owner); Assert.Null(first.Profile.Web);
        Assert.Null(first.CreationProfile); Assert.Equal("", first.CreationKey); Assert.Equal(home.Profile.PublicZone, first.Profile.PublicZone);
        Assert.InRange(first.DistanceKm!.Value, 0, .001);
        Assert.Equal(shop.Profile.Location, values.Single(v => v.AccountId == shop.AccountId).Profile.Location);
        Assert.Equal(3, (await store.ExpandedAsync(new(20.05, -99.34), "plomeria", default)).Count);
        await db.GetCollection<BsonDocument>("provider_profiles").UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, new BsonDocument("$set", new BsonDocument("published", false)));
        Assert.Empty(await store.CandidatesAsync(null, null, default));
    }
    [Fact]
    public async Task Presupuesto_de_candidatos_no_devuelve_totales_truncados()
    {
        var db = Database(); var store = new MongoProviderStore(db);
        await store.InsertAsync(Profile(20.05), default);
        var collection = db.GetCollection<BsonDocument>("provider_profiles");
        var original = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();
        await collection.InsertManyAsync(Enumerable.Range(0, 1000).Select(i =>
        {
            var doc = original.DeepClone().AsBsonDocument;
            doc["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);
            doc["accountId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);
            return doc;
        }));
        await Assert.ThrowsAsync<DirectoryUnavailableException>(() => store.CandidatesAsync(null, null, default));
    }
}
