using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Directory;

public sealed record DirectoryQuery(string Q = "", string? Cat = null, double? Lat = null, double? Lng = null,
    int RadiusKm = 10, bool Open = false, bool Free = false, bool Prices = false, bool Urgent = false,
    string Attention = "all", string Sort = "relevance", bool Expanded = false, string? Cursor = null, int Limit = 20)
{
    public ProviderPoint? Point => Lat is { } lat && Lng is { } lng ? new(lat, lng) : null;
    public string? Error => Limit is < 1 or > 50 ? NexoCodes.RequestLimitExceeded :
        Q.Length > 80 || Q.Any(char.IsControl) || Cat is { Length: > 20 } || (Cat != null && !DirectoryCatalog.Current.Categories.Any(c => c.Slug == Cat)) ||
        Lat.HasValue != Lng.HasValue || Point?.IsValid == false || RadiusKm is not (2 or 5 or 10 or 20) ||
        Attention is not ("all" or "home" or "shop") || Sort is not ("relevance" or "near" or "recs") ||
        (Point == null && (Sort == "near" || Attention == "shop" || Expanded)) ? NexoCodes.ValidationFailed : null;
    public byte[] FilterHash => SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        q = DirectoryCatalog.Normalize(Q), Cat, Lat, Lng, RadiusKm, Open, Free, Prices, Urgent, Attention, Sort, Expanded
    }))[..16];
}

public static class DirectoryCursor
{
    public static string Encode(Guid id, DirectoryQuery query)
    {
        var bytes = new byte[32];
        id.TryWriteBytes(bytes);
        query.FilterHash.CopyTo(bytes, 16);
        return Base64Url.EncodeToString(bytes);
    }
    public static bool TryDecode(string? text, DirectoryQuery query, out Guid? id)
    {
        id = null;
        if (text == null) return true;
        if (text.Length is 0 or > 64) return false;
        try
        {
            var bytes = Base64Url.DecodeFromChars(text);
            if (bytes.Length != 32 || !CryptographicOperations.FixedTimeEquals(bytes.AsSpan(16), query.FilterHash)) return false;
            id = new Guid(bytes.AsSpan(0, 16));
            return id != Guid.Empty;
        }
        catch (Exception e) when (e is FormatException or ArgumentException) { return false; }
    }
}
