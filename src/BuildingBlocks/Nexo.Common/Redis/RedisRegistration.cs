using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Nexo.Common.Redis;

public static class RedisRegistration
{
    /// <summary>Registra una sola conexion multiplexada para rate limit, revocacion, cache e idempotencia.</summary>
    public static IServiceCollection AddSharedRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Redis:ConnectionString es obligatorio.")
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value;
            var config = ConfigurationOptions.Parse(options.ConnectionString);
            config.AbortOnConnectFail = false;
            config.ConnectTimeout = options.ConnectTimeoutMilliseconds;
            config.SyncTimeout = options.SyncTimeoutMilliseconds;
            config.ConnectRetry = 1;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<SharedRateLimiter>();
        services.AddSingleton<ISessionRevocationStore, RedisSessionRevocationStore>();
        services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        services.AddSingleton<ICacheStore, RedisCacheStore>();
        services.AddSingleton<IRealtimeBackplane, RedisRealtimeBackplane>();
        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

        return services;
    }
}
