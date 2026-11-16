namespace Nexo.Engagement.Application.Conversations;

/// <summary>One conversation per client–provider pair. The provider side is identified by its account, like orders.</summary>
public sealed record Conversation(Guid Id, Guid ClientId, Guid ProviderId, Guid ProviderAccountId, Guid? RequestId, DateTime CreatedAt,
    DateTime SortAt, string? LastPreview, DateTime? LastMessageAt, int UnreadClient, int UnreadProvider, Guid[] BlockedBy)
{
    public bool IsClient(Guid account) => account == ClientId;
    public int UnreadFor(Guid account) => IsClient(account) ? UnreadClient : UnreadProvider;
}

public sealed record ChatMessage(Guid Id, Guid ConversationId, Guid SenderId, string Kind, string? Text, string? ImageUrl, string? ThumbUrl,
    int? Width, int? Height, DateTime CreatedAt, DateTime? ReadAt);

public sealed record PairOrder(Guid OrderId, Guid ClientId, Guid ProviderId, string Status);
public sealed record ChatParty(string DisplayName, string? PhotoUrl);
public sealed record ChatImage(string Url, string? ThumbUrl, int? Width, int? Height);

public interface IConversationStore
{
    Task<(Conversation Conversation, bool Created)> OpenAsync(Guid clientId, Guid providerId, Guid providerAccountId, Guid? requestId, DateTime now, CancellationToken ct);
    /// <summary>Participation is part of the filter: a non-participant gets null, same as a missing id.</summary>
    Task<Conversation?> FindAsync(Guid id, Guid account, CancellationToken ct);
    Task<Guid?> PairAsync(Guid clientId, Guid providerId, CancellationToken ct);
    Task<Conversation[]> ListAsync(Guid account, string role, bool unreadOnly, (DateTime At, Guid Id)? before, int limit, CancellationToken ct);
    Task<int> TotalUnreadAsync(Guid account, string role, CancellationToken ct);
    Task<ChatMessage[]> MessagesAsync(Guid conversationId, Guid? before, Guid? after, int limit, CancellationToken ct);
    /// <summary>Persists the message and bumps the recipient's unread; null when the conversation got blocked meanwhile.
    /// Retrying with the same idempotency key returns the first message.</summary>
    Task<ChatMessage?> SendAsync(Conversation conversation, ChatMessage message, string preview, Guid idempotencyKey, CancellationToken ct);
    Task<(int Marked, int Unread)> MarkReadAsync(Conversation conversation, Guid reader, Guid? upTo, DateTime now, CancellationToken ct);
    Task<Conversation?> BlockAsync(Guid id, Guid actor, bool blocked, CancellationToken ct);
    /// <summary>Orders in the given statuses between the pairs, newest first per pair.</summary>
    Task<PairOrder[]> PairOrdersAsync((Guid ClientId, Guid ProviderId)[] pairs, string[] statuses, CancellationToken ct);
    /// <summary>The order a provider account holds on a request: how a chosen provider finds the client.</summary>
    Task<PairOrder?> ProviderOrderOnRequestAsync(Guid providerAccountId, Guid requestId, CancellationToken ct);
}

/// <summary>Names/photos from Discovery and Identity in batch, and approved chat images from Media.</summary>
public interface IConversationDirectory
{
    Task<IReadOnlyDictionary<Guid, ChatParty>> ProvidersAsync(Guid[] providerIds, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, ChatParty>> ClientsAsync(Guid[] accountIds, CancellationToken ct);
    Task<ChatImage?> ImageAsync(Guid owner, Guid uploadId, CancellationToken ct);
}

public sealed class ConversationUnavailableException(Exception? inner = null) : Exception("Conversations are temporarily unavailable.", inner);
