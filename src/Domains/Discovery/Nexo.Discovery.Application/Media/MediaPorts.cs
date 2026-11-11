using Nexo.Common.Results;
namespace Nexo.Discovery.Application.Media;

public sealed record UploadDraft(string Purpose, string ContentType, int Size, int? Width = null, int? Height = null);
public sealed record UploadTicket(Guid UploadId, string UploadUrl, Dictionary<string, string> Headers, string Method = "PUT", int ExpiresInSeconds = 900, int MaxSizeBytes = 5242880);
public sealed record UploadState(Guid UploadId, string Status, string? Url = null, string? ThumbUrl = null, int? Width = null, int? Height = null, string? Code = null);
public sealed record MediaFile(byte[] Bytes, string ContentType);
public interface IMediaService
{
    Task<Result<UploadTicket>> CreateAsync(Guid owner, UploadDraft draft, CancellationToken ct);
    Task<Result<UploadState>> CompleteAsync(Guid owner, Guid id, CancellationToken ct);
    Task<Result<UploadState>> StateAsync(Guid owner, Guid id, CancellationToken ct);
    Task<MediaFile?> FileAsync(Guid id, bool thumb, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid owner, Guid id, CancellationToken ct);
    Task<string?> CoverAsync(Guid owner, CancellationToken ct);
    Task<Result<string>> SetCoverAsync(Guid owner, Guid id, CancellationToken ct);
}
public static class MediaRules
{
    public static string? Validate(UploadDraft d) => d.Purpose is not ("avatar" or "cover" or "work" or "portfolio" or "request") ? "validation_failed" :
        d.ContentType is not ("image/jpeg" or "image/png" or "image/webp") ? "upload.content_type_invalid" :
        d.Size > 5242880 ? "upload.too_large" : d.Size < 1 ? "validation_failed" :
        d.Width is < 1 or > 4096 || d.Height is < 1 or > 4096 ? "upload.dimensions_too_large" : null;
}
