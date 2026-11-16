using Nexo.Common.Errors;
using Nexo.Common.Results;

namespace Nexo.Engagement.Application.Notifications;

/// <summary>A notice for the bell. <see cref="GroupKey"/> makes repeats update one row instead of piling up.</summary>
public sealed record NotificationDraft(Guid AccountId, string Type, string GroupKey, string Title, string Body, string? DeepLink,
    string? ActionLabel, string Icon, DateTime At);
public sealed record StoredNotification(Guid Id, string Type, string Title, string Body, string? DeepLink, string? ActionLabel,
    string Icon, DateTime? ReadAt, DateTime CreatedAt);

public interface INotificationStore
{
    /// <summary>Upsert by (account, type, group): idempotent for outbox retries and turns back to unread.</summary>
    Task UpsertAsync(NotificationDraft draft, CancellationToken ct);
    Task<StoredNotification[]> ListAsync(Guid account, bool unreadOnly, (DateTime At, Guid Id)? before, int limit, CancellationToken ct);
    Task<int> UnreadAsync(Guid account, CancellationToken ct);
    Task<int> MarkReadAsync(Guid account, Guid[]? ids, DateTime now, CancellationToken ct);
}

/// <summary>Phone push. Off (no-op) until Firebase credentials are configured; the bell works either way.</summary>
public interface IPushSender
{
    Task SendAsync(Guid account, string title, string body, string? deepLink, CancellationToken ct);
}

public sealed class NotificationService(INotificationStore store, TimeProvider clock)
{
    public async Task<Result<object>> ListAsync(Guid me, string? filter, string? cursor, int limit, CancellationToken ct)
    {
        filter ??= "all";
        if (filter is not ("all" or "unread")) return Fail<object>(NexoCodes.ValidationFailed);
        if (limit is < 1 or > 50) return Fail<object>(NexoCodes.RequestLimitExceeded);
        (DateTime, Guid)? before = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            if (Conversations.ConversationService.DecodeCursor(cursor) is not { } parsed) return Fail<object>(NexoCodes.RequestCursorInvalid);
            before = parsed;
        }
        var rows = await store.ListAsync(me, filter == "unread", before, limit + 1, ct);
        var page = rows.Take(limit).ToArray();
        var now = clock.GetUtcNow().UtcDateTime;
        return Result<object>.Success(new
        {
            items = page.Select(n => new
            {
                notificationId = n.Id,
                type = n.Type,
                title = n.Title,
                body = n.Body,
                deepLink = n.DeepLink,
                actionLabel = n.ActionLabel,
                icon = n.Icon,
                readAt = n.ReadAt,
                ago = Ago(now - n.CreatedAt),
                createdAt = n.CreatedAt
            }),
            nextCursor = rows.Length > limit ? $"{page[^1].CreatedAt.Ticks}_{page[^1].Id:N}" : null,
            hasMore = rows.Length > limit,
            unreadCount = await store.UnreadAsync(me, ct)
        });
    }

    public async Task<Result<object>> ReadAsync(Guid me, ReadBody? body, CancellationToken ct)
    {
        var ids = body?.NotificationIds;
        var all = body?.All == true;
        if (body == null || all == (ids is { Length: > 0 }) || ids is { Length: > 50 }) return Fail<object>(NexoCodes.ValidationFailed);
        var marked = await store.MarkReadAsync(me, all ? null : ids, clock.GetUtcNow().UtcDateTime, ct);
        return Result<object>.Success(new { markedCount = marked, unreadCount = await store.UnreadAsync(me, ct) });
    }

    private static string Ago(TimeSpan d) => d.TotalMinutes < 1 ? "ahora" : d.TotalHours < 1 ? $"hace {(int)d.TotalMinutes} min"
        : d.TotalDays < 1 ? $"hace {(int)d.TotalHours} h" : $"hace {(int)d.TotalDays} días";
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación.");
    public sealed record ReadBody(Guid[]? NotificationIds = null, bool? All = null);
}
