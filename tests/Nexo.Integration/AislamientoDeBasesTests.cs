using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;

namespace Nexo.Integration;

/// <summary>nv-00 AC-005: cada dominio resuelve solo su base; no hay cliente global compartido.</summary>
[Collection("infra")]
[Trait("Category", "Integration")]
public sealed class AislamientoDeBasesTests(InfraFixture infra)
{
    private ServiceProvider Construir()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = infra.MongoConnectionString,
            ["Mongo:IdentityDatabase"] = "nexo_identity",
            ["Mongo:DiscoveryDatabase"] = "nexo_discovery",
            ["Mongo:EngagementDatabase"] = "nexo_engagement"
        }).Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddMongoDatabases(configuracion);
        return servicios.BuildServiceProvider();
    }

    [Fact]
    public void Cada_clave_resuelve_su_propia_base()
    {
        using var proveedor = Construir();

        Assert.Equal("nexo_identity", proveedor.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Identity).DatabaseNamespace.DatabaseName);
        Assert.Equal("nexo_discovery", proveedor.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery).DatabaseNamespace.DatabaseName);
        Assert.Equal("nexo_engagement", proveedor.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement).DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void Pedir_la_base_sin_clave_falla()
    {
        // El caso negativo de AC-005: no existe un IMongoDatabase global que permita
        // escribir en la base de otro dominio por descuido. Hay que pedir una clave.
        using var proveedor = Construir();

        Assert.Null(proveedor.GetService<IMongoDatabase>());
        Assert.Throws<InvalidOperationException>(() => proveedor.GetRequiredService<IMongoDatabase>());
    }

    [Fact]
    public async Task Una_escritura_de_un_dominio_no_aparece_en_la_base_de_otro()
    {
        using var proveedor = Construir();
        var identity = proveedor.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Identity);
        var discovery = proveedor.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery);
        var engagement = proveedor.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement);

        var marca = ObjectId.GenerateNewId().ToString();
        await identity.GetCollection<BsonDocument>("sonda").InsertOneAsync(new BsonDocument { ["marca"] = marca });

        var filtro = Builders<BsonDocument>.Filter.Eq("marca", marca);
        Assert.Equal(1, await identity.GetCollection<BsonDocument>("sonda").CountDocumentsAsync(filtro));
        Assert.Equal(0, await discovery.GetCollection<BsonDocument>("sonda").CountDocumentsAsync(filtro));
        Assert.Equal(0, await engagement.GetCollection<BsonDocument>("sonda").CountDocumentsAsync(filtro));
    }
}
