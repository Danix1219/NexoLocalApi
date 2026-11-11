using MongoDB.Driver;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;
using Nexo.Discovery.Infrastructure.Providers;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class ProviderPersistenceTests(InfraFixture infra)
{
    [Fact]
    public async Task Consulta_publica_excluye_punto_home_en_Mongo_y_calcula_distancia()
    {
        var database = new MongoClient(infra.MongoConnectionString).GetDatabase("profiles_" + Guid.NewGuid().ToString("N"));
        var store = new MongoProviderStore(database);
        var point = new ProviderPoint(20.05, -99.34);
        var profile = new ProviderProfile { ProviderId = Guid.CreateVersion7(), Name = "Privado", Location = point, PublicZone = new("Zona aproximada", point.Blur()), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var owner = Guid.NewGuid();
        Assert.True(await store.InsertAsync(new(owner, profile, 0, Guid.NewGuid().ToString(), "hash", true), default));
        var own = await store.OwnAsync(owner, default);
        Assert.Equal(point, own!.Profile.Location);
        var publicProfile = Assert.Single(await store.PublicAsync([profile.ProviderId], point, default));
        Assert.Null(publicProfile.Profile.Location);
        Assert.InRange(publicProfile.DistanceKm!.Value, 0, .001);
        Assert.Equal(profile.PublicZone, publicProfile.Profile.PublicZone);
        var shop = profile with { Attention = "shop", RadiusKm = 0 };
        Assert.True(await store.PatchAsync(own, shop, "zone", ["attention", "radiusKm", "location", "publicZone"], default));
        Assert.Equal(point, Assert.Single(await store.PublicAsync([profile.ProviderId], null, default)).Profile.Location);
    }
    [Fact]
    public async Task Indice_unico_y_CAS_impiden_doble_alta_y_perdida_de_ediciones()
    {
        var store = new MongoProviderStore(new MongoClient(infra.MongoConnectionString).GetDatabase("profiles_" + Guid.NewGuid().ToString("N")));
        var owner = Guid.NewGuid();
        var profile = new ProviderProfile { ProviderId = Guid.CreateVersion7(), Name = "Original", Location = new(20.05, -99.34), UpdatedAt = DateTime.UtcNow };
        var stored = new StoredProvider(owner, profile, 0, "key", "hash", true);
        Assert.True(await store.InsertAsync(stored, default));
        Assert.False(await store.InsertAsync(stored with { Profile = profile with { ProviderId = Guid.CreateVersion7() } }, default));
        Assert.True(await store.PatchAsync(stored, profile with { Name = "Nuevo" }, "basic", ["name"], default));
        Assert.False(await store.PatchAsync(stored, profile with { Bio = "Obsoleto" }, "services", ["bio"], default));
        var latest = await store.OwnAsync(owner, default);
        Assert.Equal("Nuevo", latest!.Profile.Name);
        Assert.Equal(profile.Bio, latest.Profile.Bio);
        Assert.Null(await store.OwnAsync(Guid.NewGuid(), default));
    }
}
