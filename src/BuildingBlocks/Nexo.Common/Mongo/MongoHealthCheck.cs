using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Nexo.Common.Mongo;

/// <summary>Readiness de MongoDB: no valida esquema ni lee datos de usuario.</summary>
public sealed class MongoHealthCheck(IMongoClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetDatabase("admin")
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Mongo responde.");
        }
        catch (Exception error)
        {
            return HealthCheckResult.Unhealthy("Mongo no responde.", error);
        }
    }
}
