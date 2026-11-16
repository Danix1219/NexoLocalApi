namespace Nexo.Engagement.Application.Reviews;

public sealed record StoredReview(Guid Id, Guid OrderId, Guid ProviderId, Guid ClientId, Guid IdempotencyKey, string Category,
    int Stars, string? Text, bool Recommend, DateTime CreatedAt);
public sealed record ReviewSummary(double Average, int Count, int RecommendCount, IReadOnlyDictionary<string, int> Breakdown);
public sealed record ReviewAuthor(string DisplayName, string? PhotoUrl);

public interface IReviewStore
{
    Task<StoredReview?> ByOrderAsync(Guid orderId, CancellationToken ct);
    Task<int> CountSinceAsync(Guid clientId, DateTime since, CancellationToken ct);
    /// <summary>Inserts the review and its outbox row; false when the order already has one (unique index).</summary>
    Task<bool> InsertAsync(StoredReview review, CancellationToken ct);
    Task<ReviewSummary> SummaryAsync(Guid providerId, CancellationToken ct);
    Task<StoredReview[]> PageAsync(Guid providerId, Guid? before, int limit, CancellationToken ct);
}

/// <summary>Other modules: author names (Identity) and the provider rating read model (Discovery).</summary>
public interface IReviewDirectory
{
    Task<IReadOnlyDictionary<Guid, ReviewAuthor>> AuthorsAsync(Guid[] accountIds, CancellationToken ct);
    Task SetRatingAsync(Guid providerId, double average, int count, CancellationToken ct);
}

public sealed class ReviewUnavailableException(Exception? inner = null) : Exception("Reviews are temporarily unavailable.", inner);
