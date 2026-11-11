using System.Globalization;
using System.Text;
using Nexo.Discovery.Application.Providers;

namespace Nexo.Discovery.Application.Directory;

public sealed record Specialty(string Slug, string Name);
public sealed record CategoryGradient(string From, string To);
public sealed record DirectoryCategory(string Slug, string Name, string Icon, CategoryGradient Gradient,
    Specialty[] Specialties, string BusinessLabel, string TradeLabel);
public sealed record CatalogResponse(DirectoryCategory[] Categories, string Version);
public interface IDirectoryCatalog
{
    Task<CatalogResponse> ReadAsync(CancellationToken ct);
}
public sealed class DirectoryUnavailableException : Exception;
public static class DirectoryCatalog
{
    public const string Version = "2026-11-10";
    private static DirectoryCategory Category(string slug, string name, string icon, string from, string to, string business, string trade, params string[] labels) =>
        new(slug, name, icon, new(from, to), ProviderRules.Specialties[slug].Zip(labels, (s, n) => new Specialty(s, n)).ToArray(), business, trade);
    public static CatalogResponse Current => new([
        Category("plomeria", "Plomería", "drop", "#46A3F0", "#2B72D1", "Plomería y fontanería", "Plomero/a",
            "Fugas y reparaciones", "Instalación de boilers y calentadores", "Destapes y drenaje", "Tinacos y cisternas", "Instalación de baños y cocinas"),
        Category("electricidad", "Electricidad", "bolt", "#FFC043", "#F08A1C", "Instalaciones eléctricas", "Electricista",
            "Instalaciones residenciales", "Instalaciones comerciales", "Centros de carga y tableros", "Reparación de cortos", "Iluminación y alumbrado exterior"),
        Category("carpinteria", "Carpintería", "hammer", "#C79062", "#94623B", "Carpintería y mueblería", "Carpintero/a",
            "Closets y cocinas integrales", "Puertas y ventanas", "Muebles a la medida", "Reparación de muebles", "Restauración y barnizado"),
        Category("pintura", "Pintura", "brush", "#F27AA8", "#CC4C86", "Pintura e impermeabilizantes", "Pintor/a",
            "Pintura de interiores", "Pintura de exteriores y fachadas", "Impermeabilizado", "Acabados decorativos", "Preparación de muros"),
        Category("jardineria", "Jardinería", "leaf", "#5CC593", "#2F9468", "Jardinería y paisajismo", "Jardinero/a",
            "Mantenimiento de jardines", "Poda y retiro de árboles", "Diseño de áreas verdes", "Sistemas de riego", "Instalación de pasto"),
        Category("cerrajeria", "Cerrajería", "key", "#8A8FF2", "#5C63D6", "Cerrajería y seguridad", "Cerrajero/a",
            "Apertura de puertas", "Cambio de chapas", "Duplicado de llaves", "Chapas multipunto y de seguridad", "Cerrajería automotriz"),
        Category("otros", "Otros servicios", "dots", "#9AA7AD", "#6B7A82", "Otro giro", "Otro oficio")
    ], Version);
    public static string Normalize(string value) => string.Join(' ', string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant()
        .Normalize(NormalizationForm.FormC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
