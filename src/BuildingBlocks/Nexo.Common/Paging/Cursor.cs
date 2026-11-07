using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Common.Results;

namespace Nexo.Common.Paging;

/// <summary>
/// Cursor opaco de 00-convenciones/paginacion-y-cache.md: Base64Url de
/// `{ afterId, sort, filterHash, sortValue }`. Nunca offset.
///
/// Lleva `filterHash` para rechazar un cursor reusado con otros filtros: sin eso, paginar
/// cambiando el filtro devuelve resultados incoherentes en vez de un error claro.
/// </summary>
public sealed record Cursor(string AfterId, string Sort, string FilterHash, string? SortValue = null)
{
    /// <summary>Tope del campo `cursor` en el diccionario de tipos.</summary>
    public const int MaxLength = 64;

    public string Encode()
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(this, CursorJson.Options);
        return Base64Url.EncodeToString(payload);
    }

    /// <summary>
    /// Devuelve el cursor solo si decodifica y pertenece a esta misma consulta.
    /// Un cursor ausente es la primera pagina, no un error.
    /// </summary>
    public static Result<Cursor?> Decode(string? value, string expectedFilterHash)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Cursor?>.Success(null);
        }

        if (value.Length > MaxLength * 4)
        {
            return Invalid();
        }

        Cursor? cursor;
        try
        {
            cursor = JsonSerializer.Deserialize<Cursor>(Base64Url.DecodeFromChars(value), CursorJson.Options);
        }
        catch (Exception error) when (error is FormatException or JsonException or ArgumentException)
        {
            return Invalid();
        }

        if (cursor is null || string.IsNullOrEmpty(cursor.AfterId))
        {
            return Invalid();
        }

        // El cursor de otra consulta no se reaprovecha: paginaria sobre un conjunto distinto.
        return string.Equals(cursor.FilterHash, expectedFilterHash, StringComparison.Ordinal)
            ? Result<Cursor?>.Success(cursor)
            : Invalid();
    }

    /// <summary>Huella corta y estable del conjunto de filtros ya normalizado.</summary>
    public static string HashFilters(params ReadOnlySpan<string?> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            builder.Append(part ?? string.Empty).Append('\u001f');
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(digest)[..16].ToLowerInvariant();
    }

    private static Result<Cursor?> Invalid() =>
        Result<Cursor?>.Failure(NexoCodes.RequestCursorInvalid, "El cursor no corresponde a esta consulta.");
}

internal static class CursorJson
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

/// <summary>Normalizacion de `limit` que devuelve el codigo del contrato en vez de lanzar.</summary>
public static class LimitRule
{
    public static Result<int> Normalize(int? limit)
    {
        var value = limit ?? Pagination.DefaultLimit;
        return value is < 1 or > Pagination.MaxLimit
            ? Result<int>.Failure(NexoCodes.RequestLimitExceeded, $"El limite por pagina es {Pagination.MaxLimit}.")
            : Result<int>.Success(value);
    }
}
