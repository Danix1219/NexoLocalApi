using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Requests;

public sealed record PublishRequest(string Category, string Title, string Description, string Urgency, string Contact, ProviderPoint Location, Guid? ImageUploadId = null, Guid[]? ImageUploadIds = null);
public sealed record EditRequest(string Title, string Description, string Urgency, string Contact, Guid[]? ImageUploadIds = null);
public sealed record CloseRequest(string Outcome = "noLongerNeeded", Guid? PickedProviderId = null);
public sealed record OpportunityQuery(string? Cat = null, bool Today = false, bool InRadius = true, bool MyServiceOnly = true, string Q = "", string? Cursor = null, int Limit = 20)
{
    public string? Error => Limit is < 1 or > 50 ? NexoCodes.RequestLimitExceeded : Q.Length > 80 || Q.Any(char.IsControl) ||
        Cat != null && !DirectoryCatalog.Current.Categories.Any(c => c.Slug == Cat) ? NexoCodes.ValidationFailed : null;
}
public static class RequestRules
{
    public static string? Validate(PublishRequest? body)
    {
        static bool Text(string? text, int min, int max) => text != null && text.Trim().Length >= min && text.Length <= max &&
            !text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
        return body == null || !Text(body.Title, 5, 90) || !Text(body.Description, 10, 600) || body.Location?.IsValid != true ||
            !DirectoryCatalog.Current.Categories.Any(c => c.Slug == body.Category) || body.Urgency is not ("today" or "week" or "noRush") ||
            body.Contact is not ("chat" or "chatCall") || body.ImageUploadId == Guid.Empty ? NexoCodes.ValidationFailed : ValidateImages(body.ImageUploadIds, body.ImageUploadId);
    }
    public static string? ValidateImages(Guid[]? ids, Guid? legacy = null) => ids is { Length: > 6 } ? NexoCodes.ContentImagesLimit :
        ids != null && (ids.Any(i => i == Guid.Empty) || ids.Distinct().Count() != ids.Length || legacy != null) ? NexoCodes.ValidationFailed : null;
    public static string? ValidateEdit(EditRequest? body)
    {
        if (body == null) return NexoCodes.ValidationFailed;
        return Validate(new PublishRequest("otros", body.Title, body.Description, body.Urgency, body.Contact,
            new ProviderPoint(20, -99), ImageUploadIds: body.ImageUploadIds));
    }
    public static bool Valid(CloseRequest body) => body.Outcome is "solvedHere" or "solvedElsewhere" or "noLongerNeeded" &&
        (body.PickedProviderId == null || body.Outcome == "solvedHere" && body.PickedProviderId != Guid.Empty);
    public static int Rank(string urgency) => urgency switch { "today" => 0, "week" => 1, _ => 2 };
    public static byte[] Hash(object filters) => SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(filters))[..16];
    public static string Cursor(Guid id, byte[] hash, int rank = 0)
    {
        var bytes = new byte[33]; id.TryWriteBytes(bytes); hash.CopyTo(bytes, 16); bytes[32] = (byte)rank; return Base64Url.EncodeToString(bytes);
    }
    public static bool TryCursor(string? text, byte[] hash, out Guid? id) => TryCursor(text, hash, out id, out _);
    public static bool TryCursor(string? text, byte[] hash, out Guid? id, out int rank)
    {
        id = null; rank = 0; if (string.IsNullOrEmpty(text)) return true; if (text.Length > 64) return false;
        try
        {
            var bytes = Base64Url.DecodeFromChars(text);
            if (bytes.Length != 33 || bytes[32] > 2 || !CryptographicOperations.FixedTimeEquals(bytes.AsSpan(16, 16), hash)) return false;
            rank = bytes[32]; id = new Guid(bytes.AsSpan(0, 16)); return id != Guid.Empty;
        }
        catch (Exception e) when (e is FormatException or ArgumentException) { return false; }
    }
    public static string DistanceText(double km) => km < 1 ? "a menos de 1 km" : "a ~" +
        (Math.Round(km * 2, MidpointRounding.AwayFromZero) / 2).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " km";
}
