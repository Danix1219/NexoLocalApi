using StackExchange.Redis;

namespace Nexo.Common.Redis;

/// <summary>Cache de lectura best-effort; nunca sustituye la copia autoritativa.</summary>
public interface ICacheStore
{
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public sealed class RedisCacheStore(IConnectionMultiplexer redis) : ICacheStore
{
    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default) =>
        await redis.GetDatabase().StringGetAsync($"cache:{key}");

    public async Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        await redis.GetDatabase().StringSetAsync($"cache:{key}", value, ttl);

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        await redis.GetDatabase().KeyDeleteAsync($"cache:{key}");
}
