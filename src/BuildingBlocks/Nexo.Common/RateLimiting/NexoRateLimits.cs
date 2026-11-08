using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.Common.RateLimiting;

/// <summary>
/// Politicas de 00-convenciones/rate-limiting.md.
///
/// Dos almacenes a proposito: los limites de abuso (por minuto) toleran ser aproximados y van en
/// memoria por replica, que es mas rapido y no depende de Redis; los limites de negocio (por dia)
/// tienen que ser exactos entre las dos replicas y van a Redis.
///
/// Aqui viven los nombres, los topes y las claves. El registro de las politicas en memoria lo hace
/// el host, que es quien compone el pipeline HTTP.
/// </summary>
public static class NexoRateLimits
{
    public const string Public = "public";
    public const string User = "user";
    public const string Auth = "auth";
    public const string Otp = "otp";
    public const string Publish = "publish";
    public const string Contact = "contact";
    public const string Chat = "chat";
    public const string Upload = "upload";
    public const string Ai = "ai";

    public const int PublicPerMinute = 60;
    public const int UserPerMinute = 120;
    public const int AuthPerMinute = 20;
    public const int ChatPerMinute = 60;

    // Politica otp: particion correo + IP.
    public const int OtpPerWindow = 3;
    public const int OtpPerDay = 10;
    public static readonly TimeSpan OtpWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendWindow = TimeSpan.FromSeconds(60);

    /// <summary>Correo en claro nunca: ni en claves, ni en logs, ni en trazas.</summary>
    public static string HashEmail(string normalizedEmail) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)))[..16].ToLowerInvariant();

    /// <summary>Ventana truncada en UTC para que ambas replicas cuenten lo mismo.</summary>
    public static string WindowStamp(DateTime utcNow, TimeSpan window) => window >= TimeSpan.FromDays(1)
        ? utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
        : utcNow.AddMinutes(-(utcNow.Minute % Math.Max(1, (int)window.TotalMinutes)))
            .ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);

    public static string Key(string policy, string partition, string window) =>
        $"nexo:rl:{policy}:{partition}:{window}";

}
