using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Nexo.Common.Redis;

/// <summary>Readiness de Redis: un fallo devuelve 503 en /health/ready.</summary>
public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await redis.GetDatabase().PingAsync();
            return latency > TimeSpan.FromSeconds(2)
                ? HealthCheckResult.Degraded($"Redis respondio en {latency.TotalMilliseconds:0} ms.")
                : HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis no respondio.", exception);
        }
    }
}
