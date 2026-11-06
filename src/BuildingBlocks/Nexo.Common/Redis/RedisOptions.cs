namespace Nexo.Common.Redis;

/// <summary>Configuracion unica de Redis compartido por host y dominios.</summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    public string InstanceName { get; set; } = "nexo";

    public int ConnectTimeoutMilliseconds { get; set; } = 2000;

    public int SyncTimeoutMilliseconds { get; set; } = 2000;
}
