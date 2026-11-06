using StackExchange.Redis;

namespace Nexo.Common.Redis;

/// <summary>Reserva atomica de idempotencia; la primera escritura gana durante el TTL.</summary>
public interface IIdempotencyStore
{
    Task<bool> TryReserveAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);
}

public sealed class RedisIdempotencyStore(IConnectionMultiplexer redis) : IIdempotencyStore
{
    public async Task<bool> TryReserveAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        await redis.GetDatabase().StringSetAsync($"idem:{key}", "1", ttl, When.NotExists);
}
