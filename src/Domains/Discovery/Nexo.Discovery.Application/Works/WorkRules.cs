using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Nexo.Common.Errors;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Works;

public sealed record PublishWork(string Category, string Title, string Text, ProviderPoint Location,
    string? Problem = null, string? Did = null, string? Time = null, string? Tag = null, Guid[]? ImageUploadIds = null);
public sealed record EditWork(string Title, string Text, string? Tag = null, Guid[]? ImageUploadIds = null);
public static class WorkRules
{
    public static string? Validate(PublishWork? body, string[] categories)
    {
        static bool Text(string? text, int min, int max) => text != null && text.Trim().Length >= min && text.Length <= max && !text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
        if (body == null || !Text(body.Title, 5, 90) || !Text(body.Text, 20, 1000) || body.Location?.IsValid != true ||
            new[] { body.Problem, body.Did, body.Time }.Any(t => t != null && !Text(t, 0, 300)) || body.Tag != null && !Text(body.Tag, 0, 40)) return NexoCodes.ValidationFailed;
        if (string.IsNullOrEmpty(body.Category) || body.Category.Length > 20 || !categories.Contains(body.Category)) return NexoCodes.WorkCategoryNotInProfile;
        var images = body.ImageUploadIds ?? [];
        if (images.Length > 6) return NexoCodes.ContentImagesLimit;
        if (images.Any(i => i == Guid.Empty) || images.Distinct().Count() != images.Length) return NexoCodes.ValidationFailed;
        return null;
    }
    public static string? ValidateEdit(EditWork? body) => body == null ? NexoCodes.ValidationFailed :
        Validate(new PublishWork("otros", body.Title, body.Text, new(20, -99), Tag: body.Tag,
            ImageUploadIds: body.ImageUploadIds), ["otros"]);
    public static string Cursor(Guid id, Guid provider)
    {
        var bytes = new byte[32]; id.TryWriteBytes(bytes);
        SHA256.HashData(Encoding.UTF8.GetBytes("works/1:" + provider.ToString("D"))).AsSpan(0, 16).CopyTo(bytes.AsSpan(16));
        return Base64Url.EncodeToString(bytes);
    }
    public static bool TryCursor(string? text, Guid provider, out Guid? id)
    {
        id = null;
        if (string.IsNullOrEmpty(text)) return true;
        if (text.Length > 64) return false;
        try
        {
            var bytes = Base64Url.DecodeFromChars(text);
            if (bytes.Length != 32) return false;
            var candidate = new Guid(bytes.AsSpan(0, 16));
            if (candidate == Guid.Empty || Cursor(candidate, provider) != text) return false;
            id = candidate; return true;
        }
        catch (Exception e) when (e is FormatException or ArgumentException) { return false; }
    }
}
