using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Engagement.Application.Reviews;

public sealed class ReviewService(IReviewStore store, IOrderStore orders, IOrderProviderAccess providers, IOrderSafety safety,
    IReviewDirectory directory, TimeProvider clock)
{
    public const int DailyLimit = 20;

    public async Task<Result<(object Body, int Status)>> CreateAsync(Guid client, string? idempotencyKey, CreateReviewBody? body, CancellationToken ct)
    {
        if (body == null || !Guid.TryParse(idempotencyKey, out var key)) return Fail<(object, int)>(NexoCodes.ValidationFailed);
        if (body.Stars is not (>= 1 and <= 5)) return Fail<(object, int)>(NexoCodes.ReviewStarsInvalid);
        var text = string.IsNullOrWhiteSpace(body.Text) ? null : body.Text.Trim();
        if (text is { Length: < 10 or > 500 } || text?.Any(c => char.IsControl(c) && c is not '\n') == true) return Fail<(object, int)>(NexoCodes.ValidationFailed);

        // Only the client of a resolved order may review it; anyone else gets 404 so order ids are not probed.
        var order = await orders.FindAsync(body.OrderId, client, ct);
        if (order == null || order.ClientId != client) return Fail<(object, int)>(NexoCodes.ResourceNotFound);
        if (order.Status != "resolved") return Fail<(object, int)>(NexoCodes.ReviewOrderNotResolved);

        var existing = await store.ByOrderAsync(order.Id, ct);
        if (existing != null)
            return existing.IdempotencyKey == key && existing.Stars == body.Stars && existing.Text == text && existing.Recommend == body.Recommend
                ? Result<(object, int)>.Success((await BodyAsync(existing, ct), 200))
                : Fail<(object, int)>(NexoCodes.ReviewAlreadyExists);

        var now = clock.GetUtcNow().UtcDateTime;
        if (await store.CountSinceAsync(client, now.AddDays(-1), ct) >= DailyLimit) return Fail<(object, int)>(NexoCodes.RateLimitExceeded);
        if (text != null)
        {
            var moderated = await safety.ModerateAsync(text, ct);
            if (moderated.IsFailure) return Result<(object, int)>.Failure(moderated.Error!);
            if (!moderated.Value) return Fail<(object, int)>(NexoCodes.ContentInappropriate);
        }
        var review = new StoredReview(Guid.CreateVersion7(), order.Id, order.ProviderId, client, key, order.Category, body.Stars, text, body.Recommend, now);
        if (!await store.InsertAsync(review, ct)) return Fail<(object, int)>(NexoCodes.ReviewAlreadyExists);
        return Result<(object, int)>.Success((await BodyAsync(review, ct), 201));
    }

    public async Task<Result<object>> ListAsync(Guid providerId, string? cursor, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 50) return Fail<object>(NexoCodes.RequestLimitExceeded);
        Guid? before = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            if (cursor.Length > 64 || !Guid.TryParse(cursor, out var parsed)) return Fail<object>(NexoCodes.RequestCursorInvalid);
            before = parsed;
        }
        if (await providers.GetAsync(providerId, ct) == null) return Fail<object>(NexoCodes.ProviderNotFound);
        var summary = await store.SummaryAsync(providerId, ct);
        var rows = await store.PageAsync(providerId, before, limit + 1, ct);
        var page = rows.Take(limit).ToArray();
        var authors = await directory.AuthorsAsync(page.Select(r => r.ClientId).Distinct().ToArray(), ct);
        return Result<object>.Success(new
        {
            providerId,
            summary = new { avg = summary.Average, count = summary.Count, recommendCount = summary.RecommendCount, breakdown = summary.Breakdown },
            // clientId and orderId never leave: they would tie a public opinion to an address and a date.
            items = page.Select(r => new
            {
                reviewId = r.Id,
                stars = r.Stars,
                text = r.Text,
                recommend = r.Recommend,
                author = Author(authors, r.ClientId),
                category = r.Category,
                ago = Ago(r.CreatedAt),
                createdAt = r.CreatedAt
            }),
            nextCursor = rows.Length > limit ? page[^1].Id.ToString("D") : null,
            hasMore = rows.Length > limit,
            trustLevel = "userOpinion"
        });
    }

    private async Task<object> BodyAsync(StoredReview r, CancellationToken ct)
    {
        var summary = await store.SummaryAsync(r.ProviderId, ct);
        // Full recompute, never incremental, so moderation removals also fix the average.
        try { await directory.SetRatingAsync(r.ProviderId, summary.Average, summary.Count, ct); }
        catch (Exception e) when (DependencyErrors.IsUnavailable(e) || e is ReviewUnavailableException) { } // shortcut: retried only on the next review; no outbox consumer yet.
        var authors = await directory.AuthorsAsync([r.ClientId], ct);
        return new
        {
            reviewId = r.Id,
            orderId = r.OrderId,
            providerId = r.ProviderId,
            stars = r.Stars,
            text = r.Text,
            recommend = r.Recommend,
            author = Author(authors, r.ClientId),
            createdAt = r.CreatedAt,
            moderation = new { status = "approved", checkedAt = r.CreatedAt },
            providerRating = new { avg = summary.Average, count = summary.Count }
        };
    }

    /// <summary>"Ana López Ruiz" → "Ana L.": opinions are public and permanent.</summary>
    public static string ShortName(string? full)
    {
        var parts = (full ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "Usuario";
        var first = parts[0].Length > 30 ? parts[0][..30] : parts[0];
        return parts.Length > 1 ? $"{first} {char.ToUpperInvariant(parts[1][0])}." : first;
    }

    private static object Author(IReadOnlyDictionary<Guid, ReviewAuthor> authors, Guid id) =>
        authors.TryGetValue(id, out var a) ? new { displayName = ShortName(a.DisplayName), photoUrl = a.PhotoUrl } : new { displayName = "Usuario", photoUrl = (string?)null };

    private string Ago(DateTime at)
    {
        var d = clock.GetUtcNow().UtcDateTime - at;
        return d.TotalHours < 1 ? "hace un momento" : d.TotalDays < 1 ? $"hace {(int)d.TotalHours} h" : $"hace {(int)d.TotalDays} días";
    }

    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación de la opinión.");
    public sealed record CreateReviewBody(Guid OrderId, int Stars, string? Text = null, bool Recommend = false);
}
