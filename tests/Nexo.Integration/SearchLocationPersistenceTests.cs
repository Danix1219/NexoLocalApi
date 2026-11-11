using MongoDB.Driver;
using Nexo.Discovery.Application;
using Nexo.Discovery.Infrastructure;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class SearchLocationPersistenceTests(InfraFixture infra)
{
    [Fact]
    public async Task Restaura_coordenadas_radio_etiqueta_y_aisla_por_cuenta()
    {
        var store = new MongoSearchLocationStore(new MongoClient(infra.MongoConnectionString).GetDatabase("location_" + Guid.NewGuid().ToString("N")));
        var owner = Guid.NewGuid();
        var chosen = new SearchLocation(20.05, -99.34, 5, "search", "Tula de Allende, Hidalgo");
        await store.SaveAsync(owner, chosen, default);
        Assert.Equal(chosen, await store.GetAsync(owner, default));
        Assert.Null(await store.GetAsync(Guid.NewGuid(), default));
        await store.SaveAsync(owner, chosen with { RadiusKm = 20, ZoneLabel = "Otra zona" }, default);
        Assert.Equal("Otra zona", (await store.GetAsync(owner, default))!.Label);
    }
}
