namespace Nexo.Host;

/// <summary>
/// Carga un `.env` global en las variables de entorno antes de construir la configuracion.
///
/// .NET no lee `.env` de forma nativa: sin esto, el archivo existiria pero nadie lo miraria.
/// Son pocas lineas y evita meter una dependencia solo para leer pares clave igual valor.
///
/// Reglas: **nunca** sobrescribe una variable que ya exista en el entorno, porque en Azure App
/// Service las de verdad llegan por configuracion del sitio y deben ganar siempre.
/// </summary>
public static class DotEnv
{
    private const string FileName = ".env";

    /// <summary>Busca el archivo desde el directorio dado hacia arriba y lo carga si aparece.</summary>
    public static string? Load(string startDirectory)
    {
        var path = Find(startDirectory);
        if (path is null)
        {
            return null;
        }

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            // Comillas opcionales: una cadena de conexion suele llevar caracteres raros.
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (key.Length > 0 && Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        return path;
    }

    private static string? Find(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        // Hacia arriba hasta la raiz del repositorio: un solo .env para toda la solucion.
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
