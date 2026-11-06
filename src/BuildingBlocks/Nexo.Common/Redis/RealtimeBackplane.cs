using StackExchange.Redis;

namespace Nexo.Common.Redis;

/// <summary>Backplane de notificaciones en tiempo real para despliegue multi-instancia.</summary>
public interface IRealtimeBackplane
{
    Task PublishAsync(string channel, string payload, CancellationToken cancellationToken = default);
}

public sealed class RedisRealtimeBackplane(IConnectionMultiplexer redis) : IRealtimeBackplane
{
    public async Task PublishAsync(string channel, string payload, CancellationToken cancellationToken = default) =>
        await redis.GetDatabase().PublishAsync(RedisChannel.Literal($"realtime:{channel}"), payload);
}
