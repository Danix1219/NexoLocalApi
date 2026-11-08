namespace Nexo.Common.Results;

/// <summary>Limites de paginacion por cursor fijados por la arquitectura.</summary>
public static class Pagination
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    public static int Normalize(int? limit)
    {
        var value = limit ?? DefaultLimit;
        if (value is < 1 or > MaxLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), value, $"limit debe estar entre 1 y {MaxLimit}.");
        }

        return value;
    }
}

/// <summary>Peticion de pagina por cursor; `afterId` es opaco y null significa primera pagina.</summary>
public sealed record PageRequest(string? AfterId, int Limit)
{
    public static PageRequest Create(string? afterId = null, int? limit = null) =>
        new(string.IsNullOrWhiteSpace(afterId) ? null : afterId.Trim(), Pagination.Normalize(limit));
}

/// <summary>Pagina generica con cursor siguiente y bandera explicita; nunca pagina por offset.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore)
{
    public static PagedResult<T> Empty { get; } = new(Array.Empty<T>(), null, false);
}
