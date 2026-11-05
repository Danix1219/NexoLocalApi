using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NexoLocal.Identity.Application.Features;
using NexoLocal.Identity.Application.Ports;
using NexoLocal.Identity.Infrastructure.Email;
using NexoLocal.Identity.Infrastructure.Security;

namespace NexoLocal.Identity.Infrastructure.Persistence;

/// <summary>Composicion de S01: base propia, adaptadores y casos de uso.</summary>
public static class MongoRegistration
{
    public static IServiceCollection AddIdentityMongo(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Validate(options => options.Validate() is null, "Configuracion de Mongo invalida.")
            .ValidateOnStart();

        // El motivo exacto importa al arrancar: sin el, falta saber si falla la cadena o la base.
        services.AddSingleton<IValidateOptions<MongoOptions>>(new MongoOptionsValidator());

        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ApplicationName = "NexoLocal.Identity";
            return new MongoClient(settings);
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.Database);
        });

        services.AddSingleton<IdentityCollections>();
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongo", tags: ["ready"]);
        return services;
    }

    /// <summary>Adaptadores y casos de uso de identidad (ep-02 a ep-07).</summary>
    public static IServiceCollection AddIdentityFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecretGenerator, SecretGenerator>();
        services.AddSingleton<IEmailCodeSender, LoggingEmailCodeSender>();
        services.AddScoped<IAccountStore, MongoAccountStore>();
        services.AddScoped<ISessionStore, MongoSessionStore>();
        services.AddScoped<IVerificationStore, MongoVerificationStore>();
        services.AddScoped<AuthService>();
        services.AddHostedService<IdentityIndexBootstrap>();
        return services;
    }
}

/// <summary>Convierte el motivo de MongoOptions.Validate en el fallo de arranque, sin imprimir el secreto.</summary>
public sealed class MongoOptionsValidator : IValidateOptions<MongoOptions>
{
    public ValidateOptionsResult Validate(string? name, MongoOptions options) =>
        options.Validate() is { } problem ? ValidateOptionsResult.Fail(problem) : ValidateOptionsResult.Success;
}

/// <summary>Crea indices y TTL al arrancar. Idempotente: un fallo no tumba el host, queda registrado.</summary>
public sealed class IdentityIndexBootstrap(IdentityCollections collections, ILogger<IdentityIndexBootstrap> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await collections.EnsureIndexesAsync(cancellationToken);
            logger.LogInformation("Indices de S01 verificados.");
        }
        catch (Exception error)
        {
            // Sin credenciales en el registro: solo el tipo de fallo.
            logger.LogError(error, "No se pudieron verificar los indices de S01; revisar la conexion antes de operar.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Unica comprobacion de que la conexion real responde; no valida esquema ni datos.</summary>
public sealed class MongoHealthCheck(IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Mongo responde.");
        }
        catch (Exception error)
        {
            // Sin cadena ni credenciales en la respuesta.
            return HealthCheckResult.Unhealthy("Mongo no responde.", error);
        }
    }
}
