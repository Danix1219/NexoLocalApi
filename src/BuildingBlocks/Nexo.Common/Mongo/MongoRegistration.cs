using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Nexo.Common.Mongo;

public static class MongoRegistration
{
    /// <summary>
    /// Un cliente con pool acotado y tres bases keyed. Ningun dominio recibe el cliente global
    /// ni puede resolver la base de otro dominio sin pedirla explicitamente.
    /// </summary>
    public static IServiceCollection AddMongoDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Validate(options => options.Validate() is null, "Configuracion de MongoDB invalida.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ApplicationName = "Nexo";
            settings.MaxConnectionPoolSize = options.MaxPoolSize;
            return new MongoClient(settings);
        });

        services.AddKeyedSingleton(NexoDatabaseKeys.Identity, (provider, _) =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(
                provider.GetRequiredService<IOptions<MongoOptions>>().Value.IdentityDatabase));
        services.AddKeyedSingleton(NexoDatabaseKeys.Discovery, (provider, _) =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(
                provider.GetRequiredService<IOptions<MongoOptions>>().Value.DiscoveryDatabase));
        services.AddKeyedSingleton(NexoDatabaseKeys.Engagement, (provider, _) =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(
                provider.GetRequiredService<IOptions<MongoOptions>>().Value.EngagementDatabase));

        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongo", tags: ["ready"]);
        return services;
    }
}
