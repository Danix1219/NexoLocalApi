using Nexo.Common.Results;

namespace Nexo.Engagement.Application.Orders;

public sealed record OrderPoint(double Lat, double Lng);
public sealed record OrderProvider(Guid ProviderId, Guid OwnerId, string Name, string[] Categories, string Attention, bool Published, string Zone);
public sealed record OrderClient(string Name, string? PhotoUrl);
public sealed record OrderAddress(string Address, string? Note, OrderPoint Point);
public sealed record OrderDraft(Guid Id, Guid ClientId, Guid ProviderId, Guid ProviderAccountId, Guid? RequestId, string Origin, string Description,
    string Attention, string PlaceZone, string? AddressCipher, OrderPoint? Point, DateTime CreatedAt);
public sealed record OrderSnapshot(Guid Id, Guid ClientId, Guid ProviderId, Guid? RequestId, string Origin, string Category,
    string Description, string Attention, string PlaceZone, string? AddressCipher, OrderPoint? Point, string Status,
    DateTime CreatedAt, DateTime? AcceptedAt, DateTime? CancelledAt, string? CancelledBy, DateTime? ResolvedAt,
    int Version, int? PriceCents, string? PriceIncludes);
public sealed record OrderEvent(int Version, string Type, Guid ActorId, DateTime At, string? Data);
public sealed record OrderMutation(string Status, int Version, DateTime At, string? ActorRole = null, int? PriceCents = null,
    string? PriceIncludes = null, string? Reason = null, string? Note = null, OrderAddress? Address = null);
public sealed record OrderPage(OrderSnapshot[] Items, Dictionary<string, int> Counts, bool HasMore);
public sealed record OrderResult(object Body, int StatusCode = 200);

public interface IOrderProviderAccess
{
    Task<OrderProvider?> GetAsync(Guid providerId, CancellationToken ct);
    Task<OrderClient?> GetClientAsync(Guid accountId, CancellationToken ct);
    Task<Guid?> ProviderForAccountAsync(Guid accountId, CancellationToken ct);
}

public interface IOrderStore
{
    Task<OrderSnapshot?> FindAsync(Guid orderId, Guid participantId, CancellationToken ct);
    Task<OrderSnapshot?> FindByIdAsync(Guid orderId, CancellationToken ct);
    Task<OrderSnapshot?> CreateAsync(OrderDraft draft, Guid idempotencyKey, string payloadHash, CancellationToken ct);
    Task<OrderPage> ListAsync(Guid actorId, Guid? providerId, string role, string status, Guid? before, int limit, CancellationToken ct);
    Task<OrderMutationResult> TransitionAsync(Guid id, Guid actorId, Guid? providerId, string target, int? expectedVersion,
        OrderMutation mutation, CancellationToken ct);
    Task<OrderMutationResult> ChangeAddressAsync(Guid id, Guid clientId, int? expectedVersion, OrderAddress address,
        string cipher, string placeZone, CancellationToken ct);
    Task<OrderEvent[]> EventsAsync(Guid id, CancellationToken ct);
}

public sealed record OrderMutationResult(OrderSnapshot? Order, bool NotFound, bool InvalidTransition, bool VersionConflict, bool ActiveExists = false);

public interface IOrderAddressProtection
{
    Task<string> EncryptAsync(OrderAddress address, CancellationToken ct);
    Task<OrderAddress> DecryptAsync(string cipher, CancellationToken ct);
}

public interface IOrderSafety
{
    Task<Result<bool>> ModerateAsync(string text, CancellationToken ct);
}

public sealed class OrderUnavailableException(Exception? inner = null) : Exception("Orders are temporarily unavailable.", inner);
