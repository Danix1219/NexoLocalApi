using Nexo.Common.Errors;
using Nexo.Common.Results;

namespace Nexo.Engagement.Application.Interests;

public sealed class InterestService(IInterestStore store, IInterestRequestAccess requests, IInterestDirectory directory,
    IInterestSafety safety, TimeProvider clock)
{
    public async Task<Result<InterestHttpResponse>> ExpressAsync(Guid providerId, Guid requestId, string? note, CancellationToken ct)
    {
        if (requestId == Guid.Empty || note?.Length > 200 || note?.Any(char.IsControl) == true) return Fail<InterestHttpResponse>(NexoCodes.ValidationFailed);
        var access = await requests.ForProviderAsync(requestId, providerId, ct);
        if (!access.ProviderProfileExists) return Fail<InterestHttpResponse>(NexoCodes.ProviderProfileRequired);
        if (access.Request is not { } request) return Fail<InterestHttpResponse>(NexoCodes.RequestNotFound);
        if (request.ClientId == providerId) return Fail<InterestHttpResponse>(NexoCodes.InterestOwnRequest);
        if (request.Status == "assigned") return Fail<InterestHttpResponse>(NexoCodes.InterestRequestAssigned);
        if (request.Status != "open") return Fail<InterestHttpResponse>(NexoCodes.RequestNotFound);
        var cleanNote = (note ?? string.Empty).Trim();
        if (cleanNote.Length > 0)
        {
            var moderation = await safety.ModerateAsync(cleanNote, ct);
            if (moderation.IsFailure) return Result<InterestHttpResponse>.Failure(moderation.Error!);
            if (!moderation.Value) return Fail<InterestHttpResponse>(NexoCodes.ContentInappropriate);
        }
        var saved = await store.ExpressAsync(requestId, request.ClientId, access.ProviderId!.Value, cleanNote, clock.GetUtcNow().UtcDateTime, ct);
        return Result<InterestHttpResponse>.Success(new(new { requestId, interested = true, interestCount = saved.Count, createdAt = saved.CreatedAt, canMessage = false }, saved.WasActive ? 200 : 201));
    }

    public async Task<Result<object>> WithdrawAsync(Guid providerId, Guid requestId, CancellationToken ct)
    {
        if (requestId == Guid.Empty) return Fail<object>(NexoCodes.ValidationFailed);
        var access = await requests.ForProviderAsync(requestId, providerId, ct);
        if (!access.ProviderProfileExists) return Fail<object>(NexoCodes.ProviderProfileRequired);
        if (access.Request == null) return Fail<object>(NexoCodes.RequestNotFound);
        if (access.Request.Status == "assigned") return Fail<object>(NexoCodes.InterestCannotWithdrawAssigned);
        var result = await store.WithdrawAsync(requestId, access.ProviderId!.Value, clock.GetUtcNow().UtcDateTime, ct);
        if (result.HasActiveOrder) return Fail<object>(NexoCodes.InterestCannotWithdrawAssigned);
        return Result<object>.Success(new { requestId, interested = false, interestCount = result.Count });
    }

    public async Task<Result<object>> InterestedAsync(Guid clientId, Guid requestId, string sort, string? cursor, int limit, CancellationToken ct)
    {
        if (requestId == Guid.Empty || sort is not ("rate" or "recs" or "open" or "recent")) return Fail<object>(NexoCodes.ValidationFailed);
        if (limit is < 1 or > 50) return Fail<object>(NexoCodes.RequestLimitExceeded);
        if (cursor is { Length: > 64 }) return Fail<object>(NexoCodes.RequestCursorInvalid);
        var request = await requests.OwnedAsync(requestId, clientId, ct);
        if (request == null) return Fail<object>(NexoCodes.RequestNotFound);
        var interests = await store.ActiveAsync(requestId, clientId, ct);
        var profiles = await directory.ReadAsync(interests.Select(i => i.ProviderId).ToArray(), request.PublicPoint, ct);
        var rows = interests.Where(i => profiles.ContainsKey(i.ProviderId)).Select(i => (Interest: i, Provider: profiles[i.ProviderId], Actions: (Guid?)null, Order: (string?)null, CanOrder: false)).ToList();
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var action = await store.ActionsAsync(requestId, clientId, row.Interest.ProviderId, ct);
            rows[index] = (row.Interest, row.Provider, action.ConversationId, action.OrderStatus, action.CanOrder);
        }
        IOrderedEnumerable<(InterestEntry Interest, InterestProviderCard Provider, Guid? Actions, string? Order, bool CanOrder)> ordered = sort switch
        {
            "rate" => rows.OrderByDescending(x => x.Provider.Rating).ThenByDescending(x => x.Provider.RatingCount).ThenBy(x => x.Interest.ProviderId),
            "recs" => rows.OrderByDescending(x => x.Provider.Recs).ThenBy(x => x.Interest.ProviderId),
            "open" => rows.OrderByDescending(x => x.Provider.Open).ThenBy(x => x.Interest.ProviderId),
            _ => rows.OrderByDescending(x => x.Interest.CreatedAt).ThenBy(x => x.Interest.ProviderId)
        };
        var all = ordered.ToArray();
        var start = 0;
        if (cursor is { Length: > 0 })
        {
            if (!Guid.TryParse(cursor, out var last)) return Fail<object>(NexoCodes.RequestCursorInvalid);
            var position = Array.FindIndex(all, x => x.Interest.ProviderId == last);
            if (position < 0) return Fail<object>(NexoCodes.RequestCursorInvalid);
            start = position + 1;
        }
        var page = all.Skip(start).Take(limit).ToArray();
        var items = page.Select(x => new
        {
            providerId = x.Provider.ProviderId,
            x.Provider.Name,
            x.Provider.Kind,
            x.Provider.PhotoUrl,
            x.Provider.Categories,
            x.Provider.ZoneLabel,
            x.Provider.AttentionText,
            x.Provider.DistanceText,
            status = new { open = x.Provider.Open, text = x.Provider.StatusText },
            rating = new { avg = x.Provider.Rating, count = x.Provider.RatingCount },
            recs = x.Provider.Recs,
            minPrice = x.Provider.MinPriceCents is { } cents ? new { fromCents = cents, text = $"Desde ${cents / 100m:0.##}" } : null,
            visit = new { mode = x.Provider.VisitMode, text = x.Provider.VisitMode == "free" ? "Visita sin costo" : "Visita según perfil" },
            badges = x.Provider.EmailVerified ? new[] { "emailVerified" } : Array.Empty<string>(),
            note = x.Interest.Note,
            interestedAt = x.Interest.CreatedAt,
            conversationId = x.Actions,
            orderStatus = x.Order,
            actions = new { canMessage = request.Contact is "chat" or "both", canOrder = x.CanOrder, canCompare = true }
        }).ToArray();
        var next = start + page.Length < all.Length && page.Length > 0 ? page[^1].Interest.ProviderId.ToString("D") : null;
        return Result<object>.Success(new
        {
            requestId,
            requestTitle = request.Title,
            requestStatus = request.Status,
            items,
            nextCursor = next,
            hasMore = next != null,
            total = interests.Length
        });
    }

    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación de interés.");
}
