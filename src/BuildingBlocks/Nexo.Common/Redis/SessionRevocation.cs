using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Nexo.Common.Redis;

/// <summary>Revocacion compartida por jti; MongoDB sigue siendo la autoridad de la sesion.</summary>
public interface ISessionRevocationStore
{
    Task RevokeAsync(string sessionId, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken = default);
}

public sealed class RedisSessionRevocationStore(
    IConnectionMultiplexer redis,
    ILogger<RedisSessionRevocationStore> logger) : ISessionRevocationStore
{
    public async Task RevokeAsync(string sessionId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync($"session:revoked:{sessionId}", "1", ttl);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "No se pudo publicar la revocacion de la sesion {SessionId}.", sessionId);
            throw;
        }
    }

    public async Task<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await redis.GetDatabase().KeyExistsAsync($"session:revoked:{sessionId}");
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Revocacion no disponible para {SessionId}; se consulta MongoDB.", sessionId);
            return false;
        }
    }
}
