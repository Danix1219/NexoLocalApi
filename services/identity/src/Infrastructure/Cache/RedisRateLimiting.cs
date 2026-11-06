using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace NexoLocal.Identity.Infrastructure.Cache;

/// <summary>
/// Contador de limite compartido entre instancias. El limite en memoria del host sigue aplicando por instancia;
/// este agrega el techo global, necesario desde que el servicio corre con mas de una replica.
/// </summary>
public static class RedisRateLimiting
{
    public static IServiceCollection AddSharedRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Sin Redis configurado no se registra nada: desarrollo, CI y una sola instancia funcionan igual.
            return services;
        }

        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.ClientName = "NexoLocal.Identity";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
        services.AddSingleton<SharedRateLimiter>();
        return services;
    }
}

/// <summary>Ventana fija por particion, contada en Redis con INCR + EXPIRE.</summary>
public sealed class SharedRateLimiter(IConnectionMultiplexer redis, ILogger<SharedRateLimiter> logger)
{
    public async Task<bool> TryAcquireAsync(string partition, int permitLimit, TimeSpan window)
    {
        try
        {
            // La ventana va en la clave: al cambiar de ventana la cuenta arranca sola y la vieja expira.
            var slot = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)window.TotalSeconds;
            var key = $"rl:{partition}:{slot}";
            var database = redis.GetDatabase();
            var count = await database.StringIncrementAsync(key);
            if (count == 1)
            {
                // Margen de una ventana: si EXPIRE no llega, la clave muere igual y no se queda contando para siempre.
                await database.KeyExpireAsync(key, window + window);
            }

            return count <= permitLimit;
        }
        catch (RedisException error)
        {
            // Falla abierta a proposito: Redis caido no debe tumbar la autenticacion. El limite en memoria sigue activo.
            logger.LogWarning(error, "Limite compartido no disponible; se aplica solo el limite por instancia.");
            return true;
        }
    }
}
