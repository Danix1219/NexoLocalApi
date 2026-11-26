namespace Nexo.Engagement.Application.Conversations;

/// <summary>Opaque, authenticated ciphertext only. Never shared with the public media pipeline.</summary>
public interface IPrivateChatAttachments
{
    const int MaxBytes = 8 * 1024 * 1024;
    Task PutAsync(Guid conversation, Guid attachment, byte[] ciphertext, CancellationToken ct);
    Task<byte[]?> GetAsync(Guid conversation, Guid attachment, CancellationToken ct);
}
