namespace Nexo.Common.Mongo;

/// <summary>
/// Un solo cluster MongoDB con una base por dominio. La cadena llega por secreto o
/// variable de entorno; los valores por defecto solo sirven para desarrollo local.
/// </summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017/?replicaSet=rs0";

    public int MaxPoolSize { get; set; } = 40;

    public string IdentityDatabase { get; set; } = "nexo_identity";

    public string DiscoveryDatabase { get; set; } = "nexo_discovery";

    public string EngagementDatabase { get; set; } = "nexo_engagement";

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return $"{SectionName}:ConnectionString ausente. Cargarla por user-secrets o variable {SectionName}__ConnectionString; no se versiona.";
        }

        if (!ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase)
            && !ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
        {
            return $"{SectionName}:ConnectionString debe empezar por mongodb:// o mongodb+srv://.";
        }

        if (MaxPoolSize is < 1 or > 500)
        {
            return $"{SectionName}:MaxPoolSize debe estar entre 1 y 500.";
        }

        return string.IsNullOrWhiteSpace(IdentityDatabase)
            || string.IsNullOrWhiteSpace(DiscoveryDatabase)
            || string.IsNullOrWhiteSpace(EngagementDatabase)
            ? $"{SectionName}: las tres bases por dominio son obligatorias."
            : null;
    }
}

/// <summary>Claves estables con las que el host entrega la base de cada dominio.</summary>
public static class NexoDatabaseKeys
{
    public const string Identity = "identity";
    public const string Discovery = "discovery";
    public const string Engagement = "engagement";
}
