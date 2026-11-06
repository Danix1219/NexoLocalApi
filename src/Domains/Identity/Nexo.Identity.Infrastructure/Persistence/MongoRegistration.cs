using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Infrastructure.Email;
using Nexo.Identity.Infrastructure.Security;

namespace Nexo.Identity.Infrastructure.Persistence;

/// <summary>Composicion de S01: base keyed propia, adaptadores y casos de uso.</summary>
public static class MongoRegistration
{
    /// <summary>El host entrega la base `identity` ya resuelta; el dominio nunca abre otra.</summary>
    public static IServiceCollection AddIdentityMongo(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
            new IdentityCollections(provider.GetRequiredKeyedService<IMongoDatabase>("identity")));
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
