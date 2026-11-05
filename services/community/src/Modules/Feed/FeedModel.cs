using System.Globalization;

namespace NexoLocal.Community.Modules.Feed;

/// <summary>Trabajo publicado, propiedad de S03. Solo los campos que la tarjeta publica necesita.</summary>
public sealed class Post
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required string CategoryId { get; init; }
    public required string Zone { get; init; }
    public required string ProviderId { get; init; }
    public required string ProviderName { get; init; }
    public string? CoverMediaId { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }

    public const string Published = "published";
}

/// <summary>DTO publico del feed: sin coordenadas exactas ni datos de contacto.</summary>
public sealed record FeedCard(
    string PostId,
    string Title,
    string Summary,
    string CategoryId,
    string Zone,
    string ProviderId,
    string ProviderName,
    string? CoverMediaId,
    DateTime CreatedAt);

public sealed record FeedPage(IReadOnlyList<FeedCard> Items, string? NextCursor, bool Partial);

/// <summary>
/// Cursor opaco y estable: fecha de creacion mas id, para que dos trabajos con la misma
/// fecha no se repitan ni se pierdan entre paginas.
/// </summary>
public readonly record struct FeedCursor(DateTime CreatedAt, string PostId)
{
    private const char Separator = '|';

    public string Encode() =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) + Separator + PostId));

    public static bool TryDecode(string? value, out FeedCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (Exception error) when (error is FormatException or ArgumentException or System.Text.DecoderFallbackException)
        {
            return false;
        }

        var separator = decoded.IndexOf(Separator);
        if (separator <= 0 || separator == decoded.Length - 1)
        {
            return false;
        }

        // RoundtripKind no se combina con AdjustToUniversal: el formato "O" ya trae la zona.
        if (!DateTime.TryParse(
                decoded[..separator],
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var createdAt))
        {
            return false;
        }

        cursor = new FeedCursor(createdAt.ToUniversalTime(), decoded[(separator + 1)..]);
        return true;
    }
}

/// <summary>Filtros del inicio. `limit` acotado para que una peticion no pida paginas enormes.</summary>
public sealed record FeedQuery(string? Mode, string? Zone, string? CategoryId, string? Tab, string? Cursor, int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    public static FeedQuery Create(string? mode, string? zone, string? categoryId, string? tab, string? cursor, int? limit) =>
        new(
            Trim(mode),
            Trim(zone),
            Trim(categoryId),
            Trim(tab),
            Trim(cursor),
            Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit));

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
