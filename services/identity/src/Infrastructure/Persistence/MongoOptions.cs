namespace NexoLocal.Identity.Infrastructure.Persistence;

/// <summary>Conexion de la base propia de S01. La cadena llega por secreto, nunca por el repositorio.</summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    /// <summary>Devuelve el motivo del rechazo, o null si la configuracion sirve.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return $"{SectionName}:ConnectionString ausente. Cargarla por user-secrets o variable {SectionName}__ConnectionString; no se versiona.";
        }

        if (!ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase)
            && !ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
        {
            return $"{SectionName}:ConnectionString debe empezar por mongodb:// o mongodb+srv:// (Atlas).";
        }

        return string.IsNullOrWhiteSpace(Database)
            ? $"{SectionName}:Database ausente. S01 solo usa su propia base."
            : null;
    }
}
