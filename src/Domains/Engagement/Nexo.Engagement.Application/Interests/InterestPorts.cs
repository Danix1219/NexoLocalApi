using Nexo.Common.Results;
namespace Nexo.Engagement.Application.Interests;

public sealed record InterestPoint(double Lat, double Lng);
public sealed record InterestRequestContext(Guid RequestId, Guid ClientId, string Title, string Status, string Contact,
    InterestPoint PublicPoint, string ZoneLabel, DateTime CreatedAt);
public sealed record InterestRequestAccess(bool ProviderProfileExists, Guid? ProviderId, InterestRequestContext? Request);
public sealed record InterestProviderCard(Guid ProviderId, string Name, string Kind, string? PhotoUrl, string[] Categories,
    string ZoneLabel, string AttentionText, string DistanceText, bool Open, string StatusText, double Rating, int RatingCount,
    int Recs, int? MinPriceCents, string VisitMode, bool EmailVerified);
public sealed record InterestEntry(Guid ProviderId, string Note, DateTime CreatedAt);
public sealed record ExpressInterestResult(bool WasActive, int Count, DateTime CreatedAt);
public sealed record WithdrawInterestResult(int Count, bool WasActive, bool HasActiveOrder);
public sealed record InterestHttpResponse(object Body, int StatusCode = 200);

public interface IInterestRequestAccess
{
    Task<InterestRequestAccess> ForProviderAsync(Guid requestId, Guid providerId, CancellationToken ct);
    Task<InterestRequestContext?> OwnedAsync(Guid requestId, Guid clientId, CancellationToken ct);
}

public interface IInterestDirectory
{
    Task<IReadOnlyDictionary<Guid, InterestProviderCard>> ReadAsync(Guid[] providerIds, InterestPoint from, CancellationToken ct);
}

public interface IInterestStore
{
    Task<ExpressInterestResult> ExpressAsync(Guid requestId, Guid clientId, Guid providerId, string note, DateTime now, CancellationToken ct);
    Task<WithdrawInterestResult> WithdrawAsync(Guid requestId, Guid providerId, DateTime now, CancellationToken ct);
    Task<InterestEntry[]> ActiveAsync(Guid requestId, Guid clientId, CancellationToken ct);
    Task<(Guid? ConversationId, string? OrderStatus, bool CanOrder)> ActionsAsync(Guid requestId, Guid clientId, Guid providerId, CancellationToken ct);
}

public interface IInterestSafety
{
    Task<Result<bool>> ModerateAsync(string text, CancellationToken ct);
}

public sealed class InterestUnavailableException(Exception? inner = null) : Exception("Engagement is temporarily unavailable.", inner);
