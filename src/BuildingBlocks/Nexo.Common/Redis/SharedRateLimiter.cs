using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Nexo.Common.Redis;

/// <summary>Ventana fija atomica en Redis; si Redis falla se registra y se permite continuar.</summary>
public sealed class SharedRateLimiter(
    IConnectionMultiplexer redis,
    ILogger<SharedRateLimiter> logger,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<bool> IsAllowedAsync(
        string partitionKey,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            throw new ArgumentException("La clave de particion es obligatoria.", nameof(partitionKey));
        }

        var now = _timeProvider.GetUtcNow();
        var windowIndex = now.ToUnixTimeSeconds() / Math.Max(1, (long)window.TotalSeconds);
        var redisKey = $"rate:{partitionKey}:{windowIndex}";

        try
        {
            var database = redis.GetDatabase();
            var count = await database.StringIncrementAsync(redisKey);
            if (count == 1)
            {
                await database.KeyExpireAsync(redisKey, window);
            }

            return count <= limit;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Rate limit no disponible para {PartitionKey}; se permite la peticion.", partitionKey);
            return true;
        }
    }
}
