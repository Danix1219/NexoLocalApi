namespace Nexo.Engagement.Application.Conversations;

/// <summary>Opaque, authenticated ciphertext only. Never shared with the public media pipeline.</summary>
public interface IPrivateChatAttachments
{
    const int MaxBytes = 8 * 1024 * 1024;
    Task PutAsync(Guid conversation, Guid attachment, byte[] ciphertext, CancellationToken ct);
    Task<byte[]?> GetAsync(Guid conversation, Guid attachment, CancellationToken ct);
    /// <summary>Borra el objeto; no falla si ya no existe.</summary>
    Task DeleteAsync(Guid conversation, Guid attachment, CancellationToken ct);
}

public sealed record ChatAttachmentRef(Guid ConversationId, Guid AttachmentId);

/// <summary>Quién subió cada adjunto: el contenido va cifrado, así que sin esto no se podría borrar con su cuenta.</summary>
public interface IChatAttachmentOwners
{
    /// <summary>Anota al autor antes de subir. Devuelve false si ese identificador ya es de otra cuenta.</summary>
    Task<bool> ClaimAsync(ChatAttachmentRef attachment, Guid uploader, DateTime now, CancellationToken ct);
    Task<IReadOnlyList<ChatAttachmentRef>> OfAsync(Guid uploader, CancellationToken ct);
    Task ForgetAsync(ChatAttachmentRef attachment, CancellationToken ct);
}
