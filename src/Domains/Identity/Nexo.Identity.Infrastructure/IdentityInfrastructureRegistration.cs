using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Infrastructure.Messaging;
using Nexo.Identity.Infrastructure.Persistence;
using Nexo.Identity.Infrastructure.Redis;
using Nexo.Identity.Infrastructure.Security;
using Nexo.Identity.Infrastructure.Safety;
using Nexo.Outbox;

namespace Nexo.Identity.Infrastructure;

public static class IdentityInfrastructureRegistration
{
    /// <summary>Adaptadores de Identity; recibe la base keyed y nunca un cliente global.</summary>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.AddOptions<IdentitySecretsOptions>()
            .Bind(configuration.GetSection(IdentitySecretsOptions.SectionName))
            .Validate(options => options.Validate() is null, "Configuracion de Identity invalida.")
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<IdentitySecretsOptions>>(new IdentitySecretsValidator());

        services.AddSingleton(provider =>
            new IdentityCollections(provider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Identity)));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IVerificationCodeFactory, VerificationCodeFactory>();

        services.AddScoped<IAccountRepository, MongoAccountRepository>();
        services.AddScoped<IDeviceRegistry, MongoDeviceRegistry>();
        services.AddScoped<IAccountManagementStore, MongoAccountManagementStore>();
        services.AddScoped<Nexo.Identity.Contracts.IProviderIdentityContract, ProviderIdentityContract>();
        services.AddScoped<Nexo.Identity.Contracts.IRequestClientContract, RequestClientContract>();
        services.AddScoped<AccountManagement>();
        services.Configure<ContentSafetyOptions>(configuration.GetSection("ContentSafety"));
        services.AddHttpClient<IAccountProfileSafety, AccountProfileSafety>(client => client.Timeout = TimeSpan.FromSeconds(8));
        services.AddHostedService<AccountEventRelay>();
        services.AddScoped<IEmailVerificationStore, RedisEmailVerificationStore>();
        services.AddScoped<IVerificationTicketStore, RedisVerificationTicketStore>();
        services.AddScoped<IOtpQuota, RedisOtpQuota>();
        services.AddScoped<ILoginLockStore, RedisLoginLockStore>();
        services.AddScoped<IIdentityEvents, OutboxIdentityEvents>();

        services.AddScoped<EmailVerificationService>();
        services.AddScoped<AccountRegistrationService>();
        services.AddScoped<PasswordRecoveryService>();
        services.AddScoped<SignInService>();
        services.AddScoped<SignOutService>();

        // Servidor OAuth: emite y revoca los tokens de la cuenta.
        services.AddIdentityAuthorizationServer(configuration, isDevelopment);

        services.AddHostedService<IdentityIndexBootstrap>();
        return services;
    }

    /// <summary>
    /// Consumidor del outbox de Identity, registrado en el mismo host que atiende HTTP.
    /// El despachador envia los correos fuera del camino de la peticion.
    /// Va antes de AddOutbox, que registra su handler de registro con TryAdd.
    /// </summary>
    public static IServiceCollection AddIdentityOutboxHandler(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MailOptions>()
            .Bind(configuration.GetSection(MailOptions.SectionName))
            .Validate(options => options.Validate() is null, "Configuracion de Mail invalida.")
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MailOptions>>(new MailValidator());
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IOutboxHandler, IdentityOutboxHandler>();
        return services;
    }
}

/// <summary>Convierte el motivo en el fallo de arranque, sin imprimir la contrasena del SMTP.</summary>
public sealed class MailValidator : IValidateOptions<MailOptions>
{
    public ValidateOptionsResult Validate(string? name, MailOptions options) =>
        options.Validate() is { } problem ? ValidateOptionsResult.Fail(problem) : ValidateOptionsResult.Success;
}

/// <summary>Convierte el motivo de la validacion en el fallo de arranque, sin imprimir la llave.</summary>
public sealed class IdentitySecretsValidator : IValidateOptions<IdentitySecretsOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentitySecretsOptions options) =>
        options.Validate() is { } problem ? ValidateOptionsResult.Fail(problem) : ValidateOptionsResult.Success;
}

/// <summary>Crea indices y TTL al arrancar. Idempotente: un fallo no tumba el host, queda registrado.</summary>
public sealed class IdentityIndexBootstrap(IdentityCollections collections, ILogger<IdentityIndexBootstrap> logger) : IHostedService
{
    private Task? running;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // En segundo plano a proposito: con Mongo caido, esperar aqui retrasaria que el host escuche
        // y /health/ready debe poder responder 503 de inmediato.
        running = EnsureIndexesAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => running ?? Task.CompletedTask;

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await collections.EnsureIndexesAsync(cancellationToken);
            logger.LogInformation("Indices de identidad verificados.");
        }
        catch (Exception error)
        {
            logger.LogError(error, "No se pudieron verificar los indices de identidad; revisar la conexion.");
        }
    }
}
