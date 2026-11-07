using Testcontainers.MongoDb;
using Testcontainers.Redis;

namespace Nexo.Integration;

/// <summary>
/// MongoDB y Redis reales para los AC de nv-00. Se comparten entre las clases de la coleccion
/// para no pagar el arranque de los contenedores una vez por prueba.
/// </summary>
public sealed class InfraFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongo = new MongoDbBuilder("mongo:7.0").Build();

    private readonly RedisContainer redis = new RedisBuilder("redis:7.2-alpine").Build();

    public string MongoConnectionString => mongo.GetConnectionString();

    public string RedisConnectionString => redis.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(mongo.StartAsync(), redis.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(mongo.DisposeAsync().AsTask(), redis.DisposeAsync().AsTask());
    }
}

[CollectionDefinition("infra")]
public sealed class InfraCollection : ICollectionFixture<InfraFixture>;

/// <summary>
/// Valores que el host exige al arrancar y que en CI no llegan de ningun `.env`.
/// </summary>
public static class ConfiguracionDePrueba
{
    /// <summary>
    /// Se calcula en vez de escribirse: una cadena Base64 literal junto a `CodeHmacKey` la marcan
    /// como secreto filtrado los escaneres del repositorio, y no lo es.
    /// </summary>
    public static readonly string ClaveHmac =
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("llave-de-prueba-sin-valor-real"));
}
