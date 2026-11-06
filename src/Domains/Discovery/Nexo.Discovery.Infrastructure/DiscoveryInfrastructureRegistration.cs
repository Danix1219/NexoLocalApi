using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application.Ports;
using Nexo.Discovery.Infrastructure.Feed;

namespace Nexo.Discovery.Infrastructure;

public static class DiscoveryInfrastructureRegistration
{
    /// <summary>Adaptadores de Discovery; recibe la base keyed y nunca un cliente global.</summary>
    public static IServiceCollection AddDiscoveryInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
            new PostCollection(provider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery)));
        services.AddScoped<IFeedReader, MongoFeedReader>();
        services.AddHostedService<DiscoveryIndexBootstrap>();
        return services;
    }
}

/// <summary>Crea indices al arrancar. Idempotente: un fallo no tumba el host, queda registrado.</summary>
public sealed class DiscoveryIndexBootstrap(PostCollection collection, ILogger<DiscoveryIndexBootstrap> logger) : IHostedService
{
    private Task? running;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // En segundo plano a proposito: con Mongo caido, esperar aqui retrasaria que el host escuche.
        running = EnsureIndexesAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => running ?? Task.CompletedTask;

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await collection.EnsureIndexesAsync(cancellationToken);
            logger.LogInformation("Indices de Discovery verificados.");
        }
        catch (Exception error)
        {
            logger.LogError(error, "No se pudieron verificar los indices de Discovery; revisar la conexion antes de operar.");
        }
    }
}
