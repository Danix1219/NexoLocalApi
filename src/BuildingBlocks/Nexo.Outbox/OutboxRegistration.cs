using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Nexo.Outbox;

public static class OutboxRegistration
{
    /// <summary>Registra el outbox sobre la base keyed del dominio; el host decide si arranca el dispatcher.</summary>
    public static IServiceCollection AddOutbox(this IServiceCollection services, string databaseKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseKey);

        services.AddSingleton<IOutboxRepository>(provider =>
            new MongoOutboxRepository(provider.GetRequiredKeyedService<IMongoDatabase>(databaseKey)));
        services.TryAddSingleton<IOutboxHandler, LoggingOutboxHandler>();
        services.AddSingleton<IOutboxService, OutboxService>();
        return services;
    }
}
