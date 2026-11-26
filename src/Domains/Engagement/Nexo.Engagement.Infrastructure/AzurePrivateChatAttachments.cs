using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Nexo.Engagement.Application.Conversations;

namespace Nexo.Engagement.Infrastructure;

public sealed class AzurePrivateChatAttachments(IConfiguration configuration) : IPrivateChatAttachments
{
    private async Task<BlobClient> BlobAsync(Guid conversation, Guid attachment, CancellationToken ct)
    {
        var connection = configuration["Chat:StorageConnectionString"];
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Private chat storage is not configured.");
        var container = new BlobServiceClient(connection).GetBlobContainerClient("chat-private");
        // Provisioned separately. A public container is rejected, never silently reused.
        var properties = (await container.GetPropertiesAsync(cancellationToken: ct)).Value;
        if (properties.PublicAccess != PublicAccessType.None) throw new InvalidOperationException("Private chat container must deny public access.");
        return container.GetBlobClient($"{conversation:D}/{attachment:D}");
    }

    public async Task PutAsync(Guid conversation, Guid attachment, byte[] ciphertext, CancellationToken ct)
    {
        if (ciphertext.Length is < 16 or > IPrivateChatAttachments.MaxBytes) throw new InvalidDataException("Invalid ciphertext size.");
        var blob = await BlobAsync(conversation, attachment, ct);
        await blob.UploadAsync(BinaryData.FromBytes(ciphertext), new BlobUploadOptions
        {
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/octet-stream", CacheControl = "private, no-store" }
        }, ct);
    }

    public async Task<byte[]?> GetAsync(Guid conversation, Guid attachment, CancellationToken ct)
    {
        var blob = await BlobAsync(conversation, attachment, ct);
        try
        {
            var properties = (await blob.GetPropertiesAsync(cancellationToken: ct)).Value;
            if (properties.ContentLength is < 16 or > IPrivateChatAttachments.MaxBytes) throw new InvalidDataException("Invalid ciphertext size.");
            var result = await blob.DownloadContentAsync(new BlobDownloadOptions
            { Conditions = new BlobRequestConditions { IfMatch = properties.ETag } }, ct);
            return result.Value.Content.ToArray();
        }
        catch (RequestFailedException e) when (e.Status == 404) { return null; }
    }
}
