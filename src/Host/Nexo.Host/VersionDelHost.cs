using System.Reflection;

namespace Nexo.Host;

/// <summary>
/// Version sellada en el ensamblado al publicar, con el SHA corto del commit.
///
/// La fija el despliegue con `-p:InformationalVersion=$(cat VERSION)+<sha>`; el archivo `VERSION`
/// de la raiz es la unica fuente. Se publica en `info.version` del documento OpenAPI para poder
/// comprobar que version esta viva sin entrar al portal.
/// </summary>
public static class VersionDelHost
{
    /// <summary>
    /// En una compilacion local sin sellar, `InformationalVersion` trae la version del ensamblado.
    /// Se devuelve igual, sin inventar nada: un `0.0.0` fijo mentiria.
    /// </summary>
    public static string Actual { get; } =
        typeof(VersionDelHost).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(VersionDelHost).Assembly.GetName().Version?.ToString()
        ?? "desconocida";
}
