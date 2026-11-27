using System.Globalization;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Engagement.Application.Orders;
using Nexo.Engagement.Application.Reviews;

namespace Nexo.Engagement.Application.Conversations;

public sealed class ConversationService(IConversationStore store, IOrderProviderAccess providers, IConversationDirectory directory,
    IOrderSafety safety, IChatRealtime live, TimeProvider clock)
{
    private static readonly string[] Active = ["pending", "accepted"];
    private static readonly string[] Chosen = ["pending", "accepted", "resolved"];
    private static readonly string[] ClientReplies = ["¿Cuándo podrías venir?", "¿Cuánto costaría?", "¿Atiendes en mi zona?"];
    private static readonly string[] ProviderReplies = ["Puedo pasar mañana", "¿Me compartes una foto?", "Gracias por escribir"];
    private static readonly string[] ThreadReplies = ["De acuerdo", "¿A qué hora?", "Gracias"];

    public async Task<Result<(object Body, int Status)>> OpenAsync(Guid me, OpenBody? body, CancellationToken ct)
    {
        if (body == null || body.ProviderId == null && body.RequestId == null && body.ClientId == null) return Fail<(object, int)>(NexoCodes.ValidationFailed);
        Guid clientId, providerId, providerAccount;
        if (body.ProviderId is { } wanted)
        {
            // Client side: anyone may start a chat with a published provider.
            var provider = await providers.GetAsync(wanted, ct);
            if (provider == null) return Fail<(object, int)>(NexoCodes.ProviderNotFound);
            if (provider.OwnerId == me) return Fail<(object, int)>(NexoCodes.ConversationSelf);
            (clientId, providerId, providerAccount) = (me, provider.ProviderId, provider.OwnerId);
        }
        else
        {
            // Provider side: only once chosen, i.e. there is an order with that client (constitution: no unsolicited messages).
            var myProfile = await providers.ProviderForAccountAsync(me, ct);
            if (myProfile == null) return Fail<(object, int)>(NexoCodes.ConversationNotAllowed);
            PairOrder? order = null;
            if (body.RequestId is { } requestId) order = await store.ProviderOrderOnRequestAsync(me, requestId, ct);
            else if (body.ClientId is { } client)
                order = (await store.PairOrdersAsync([(client, myProfile.Value)], Chosen, ct)).FirstOrDefault();
            if (order == null || !Chosen.Contains(order.Status)) return Fail<(object, int)>(NexoCodes.ConversationNotAllowed);
            if (order.ClientId == me) return Fail<(object, int)>(NexoCodes.ConversationSelf);
            (clientId, providerId, providerAccount) = (order.ClientId, myProfile.Value, me);
        }

        var (conversation, created) = await store.OpenAsync(clientId, providerId, providerAccount, body.RequestId, clock.GetUtcNow().UtcDateTime, ct);
        var activeOrder = (await store.PairOrdersAsync([(clientId, providerId)], Active, ct)).FirstOrDefault();
        var counterpart = await CounterpartAsync([conversation], me, activeOrder == null ? [] : [activeOrder], ct);
        return Result<(object, int)>.Success((new
        {
            conversationId = conversation.Id,
            counterpart = counterpart[conversation.Id],
            requestId = conversation.RequestId,
            activeOrder = OrderBadge(activeOrder),
            created,
            canSend = conversation.BlockedBy.Length == 0,
            encrypted = conversation.Encrypted,
            quickReplies = conversation.IsClient(me) ? ClientReplies : ProviderReplies
        }, created ? 201 : 200));
    }

    public async Task<Result<object>> InboxAsync(Guid me, string? role, string? filter, string? cursor, int limit, CancellationToken ct)
    {
        role ??= "client"; filter ??= "all";
        if (role is not ("client" or "provider") || filter is not ("all" or "unread")) return Fail<object>(NexoCodes.ValidationFailed);
        if (limit is < 1 or > 50) return Fail<object>(NexoCodes.RequestLimitExceeded);
        (DateTime, Guid)? before = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            if (DecodeCursor(cursor) is not { } parsed) return Fail<object>(NexoCodes.RequestCursorInvalid);
            before = parsed;
        }
        if (role == "provider" && await providers.ProviderForAccountAsync(me, ct) == null) return Fail<object>(NexoCodes.ProviderProfileRequired);
        var rows = await store.ListAsync(me, role, filter == "unread", before, limit + 1, ct);
        var page = rows.Take(limit).ToArray();
        var orders = await store.PairOrdersAsync(page.Select(c => (c.ClientId, c.ProviderId)).Distinct().ToArray(), Chosen, ct);
        var names = await CounterpartAsync(page, me, orders, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        return Result<object>.Success(new
        {
            items = page.Select(c =>
            {
                var active = orders.FirstOrDefault(o => o.ClientId == c.ClientId && o.ProviderId == c.ProviderId && Active.Contains(o.Status));
                return new
                {
                    conversationId = c.Id,
                    counterpart = names[c.Id],
                    lastPreview = c.LastPreview,
                    lastMessageAt = c.LastMessageAt,
                    ago = c.LastMessageAt is { } at ? Ago(now - at) : null,
                    unread = c.UnreadFor(me),
                    requestId = c.RequestId,
                    activeOrder = OrderBadge(active),
                    blocked = c.BlockedBy.Length > 0,
                    blockedByMe = c.BlockedBy.Contains(me)
                };
            }),
            nextCursor = rows.Length > limit ? EncodeCursor(page[^1]) : null,
            hasMore = rows.Length > limit,
            totalUnread = await store.TotalUnreadAsync(me, role, ct)
        });
    }

    public async Task<Result<object>> ThreadAsync(Guid me, Guid id, string? before, string? after, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 50 || !string.IsNullOrEmpty(before) && !string.IsNullOrEmpty(after)) return Fail<object>(NexoCodes.ValidationFailed);
        Guid? beforeId = null, afterId = null;
        if (!string.IsNullOrEmpty(before)) { if (!Guid.TryParse(before, out var b)) return Fail<object>(NexoCodes.RequestCursorInvalid); beforeId = b; }
        if (!string.IsNullOrEmpty(after)) { if (!Guid.TryParse(after, out var a)) return Fail<object>(NexoCodes.RequestCursorInvalid); afterId = a; }
        var conversation = await store.FindAsync(id, me, ct);
        if (conversation == null) return Fail<object>(NexoCodes.ConversationNotFound);
        var rows = await store.MessagesAsync(id, beforeId, afterId, limit + 1, ct);
        var page = rows.Take(limit).ToArray();
        var orders = await store.PairOrdersAsync([(conversation.ClientId, conversation.ProviderId)], Chosen, ct);
        var active = orders.FirstOrDefault(o => Active.Contains(o.Status));
        var names = await CounterpartAsync([conversation], me, orders, ct);
        return Result<object>.Success(new
        {
            conversationId = id,
            counterpart = names[id],
            encrypted = conversation.Encrypted,
            items = page.Select(m => Message(m, me)),
            peerAccountId = conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId,
            nextCursor = rows.Length > limit ? page[^1].Id.ToString("D") : null,
            hasMore = rows.Length > limit,
            activeOrder = OrderBadge(active),
            blocked = conversation.BlockedBy.Length > 0,
            canSend = conversation.BlockedBy.Length == 0,
            quickReplies = ThreadReplies
        });
    }

    public async Task<Result<object>> SendAsync(Guid me, Guid id, string? idempotencyKey, SendBody? body, CancellationToken ct)
    {
        if (body == null || !Guid.TryParse(idempotencyKey, out var key) || body.Kind is not ("text" or "image")) return Fail<object>(NexoCodes.ValidationFailed);
        var text = body.Text?.Trim();
        if (body.Kind == "text" && (string.IsNullOrEmpty(text) || text.Length > 1000)) return Fail<object>(NexoCodes.ValidationFailed);
        if (body.Kind == "image" && body.ImageUploadId is null) return Fail<object>(NexoCodes.ValidationFailed);
        var conversation = await store.FindAsync(id, me, ct);
        if (conversation == null) return Fail<object>(NexoCodes.ConversationNotFound);
        if (conversation.BlockedBy.Length > 0) return Fail<object>(NexoCodes.ConversationBlocked);

        if (conversation.Encrypted) return Fail<object>(NexoCodes.ValidationFailed);

        ChatImage? image = null;
        if (body.Kind == "text")
        {
            var moderated = await safety.ModerateAsync(text!, ct);
            if (moderated.IsFailure) return Result<object>.Failure(moderated.Error!);
            if (!moderated.Value) return Fail<object>(NexoCodes.ContentInappropriate);
        }
        else if ((image = await directory.ImageAsync(me, body.ImageUploadId!.Value, ct)) == null) return Fail<object>(NexoCodes.UploadNotFound);

        var message = new ChatMessage(Guid.CreateVersion7(), id, me, body.Kind, body.Kind == "text" ? text : null, image?.Url, image?.ThumbUrl,
            image?.Width, image?.Height, clock.GetUtcNow().UtcDateTime, null);
        var preview = body.Kind == "image" ? "Envió una foto" : text!.Length > 120 ? text[..120] : text;
        var saved = await store.SendAsync(conversation, message, preview, key, ct);
        if (saved == null) return Fail<object>(NexoCodes.ConversationBlocked);
        // Each side gets its own `mine`; the sender's other devices see it too.
        var other = conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId;
        var theirs = Message(saved, other); theirs["conversationId"] = id;
        var mine = Message(saved, me); mine["conversationId"] = id;
        await live.SendAsync(other, "messageReceived", theirs, ct);
        await live.SendAsync(me, "messageReceived", mine, ct);
        var sent = Message(saved, me);
        // shortcut: we do not track presence, so this cannot say whether the other side was connected; push covers offline.
        sent["deliveredToSocket"] = false;
        return Result<object>.Success(sent);
    }

    public async Task<Result<object>> DeliveredAsync(Guid me, Guid id, Guid upTo, CancellationToken ct)
    {
        var conversation = await store.FindAsync(id, me, ct);
        if (conversation is null) return Fail<object>(NexoCodes.ConversationNotFound);
        var now = clock.GetUtcNow().UtcDateTime;
        var marked = await store.MarkDeliveredAsync(conversation, me, upTo, now, ct);
        if (marked > 0)
            await live.SendAsync(conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId,
                "messageDelivered", new { conversationId = id, upToMessageId = upTo, deliveredAt = now }, ct);
        return Result<object>.Success(new { marked });
    }

    public async Task<Result<object>> ReadAsync(Guid me, Guid id, Guid? upTo, CancellationToken ct)
    {
        var conversation = await store.FindAsync(id, me, ct);
        if (conversation == null) return Fail<object>(NexoCodes.ConversationNotFound);
        var now = clock.GetUtcNow().UtcDateTime;
        var (marked, unread) = await store.MarkReadAsync(conversation, me, upTo, now, ct);
        if (marked > 0)
            await live.SendAsync(conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId, "messageRead",
                new { conversationId = id, upToMessageId = upTo, readAt = now }, ct);
        return Result<object>.Success(new
        {
            conversationId = id,
            unread,
            markedCount = marked,
            totalUnread = await store.TotalUnreadAsync(me, conversation.IsClient(me) ? "client" : "provider", ct)
        });
    }

    public async Task<Result<object>> BlockAsync(Guid me, Guid id, BlockBody? body, CancellationToken ct)
    {
        if (body?.Blocked is not { } blocked) return Fail<object>(NexoCodes.ValidationFailed);
        var conversation = await store.FindAsync(id, me, ct);
        if (conversation == null) return Fail<object>(NexoCodes.ConversationNotFound);
        // Each side lifts only its own block.
        if (!blocked && !conversation.BlockedBy.Contains(me) && conversation.BlockedBy.Length > 0) return Fail<object>(NexoCodes.ConversationBlockedByOther);
        var updated = await store.BlockAsync(id, me, blocked, ct);
        if (updated == null) return Fail<object>(NexoCodes.ConversationNotFound);
        // Neutral signal only: it says sending is closed, never who blocked.
        foreach (var party in new[] { updated.ClientId, updated.ProviderAccountId })
            await live.SendAsync(party, "conversationBlocked", new { conversationId = id, canSend = updated.BlockedBy.Length == 0 }, ct);
        var accepted = (await store.PairOrdersAsync([(updated.ClientId, updated.ProviderId)], ["accepted"], ct)).Length > 0;
        return Result<object>.Success(new
        {
            conversationId = id,
            blocked = updated.BlockedBy.Length > 0,
            blockedByMe = updated.BlockedBy.Contains(me),
            canSend = updated.BlockedBy.Length == 0,
            activeOrderWarning = blocked && accepted ? "Tienes un pedido aceptado con esta persona. Bloquear no lo cancela." : null
        });
    }

    /// <summary>Provider profile name for the client; for the provider, the client's full name only once an order was accepted, otherwise "Nombre I.".</summary>
    private async Task<Dictionary<Guid, object>> CounterpartAsync(Conversation[] rows, Guid me, PairOrder[] orders, CancellationToken ct)
    {
        var providerIds = rows.Where(c => c.IsClient(me)).Select(c => c.ProviderId).Distinct().ToArray();
        var clientIds = rows.Where(c => !c.IsClient(me)).Select(c => c.ClientId).Distinct().ToArray();
        var providerNames = providerIds.Length == 0 ? new Dictionary<Guid, ChatParty>() : await directory.ProvidersAsync(providerIds, ct);
        var clientNames = clientIds.Length == 0 ? new Dictionary<Guid, ChatParty>() : await directory.ClientsAsync(clientIds, ct);
        return rows.ToDictionary(c => c.Id, c =>
        {
            if (c.IsClient(me))
            {
                var p = providerNames.GetValueOrDefault(c.ProviderId);
                return (object)new { id = c.ProviderId, providerId = c.ProviderId, displayName = p?.DisplayName ?? "Prestador", photoUrl = p?.PhotoUrl, kind = "provider" };
            }
            var known = orders.Any(o => o.ClientId == c.ClientId && o.ProviderId == c.ProviderId && o.Status is "accepted" or "resolved");
            var person = clientNames.GetValueOrDefault(c.ClientId);
            return new { id = c.ClientId, displayName = known ? person?.DisplayName ?? "Cliente" : ReviewService.ShortName(person?.DisplayName), photoUrl = person?.PhotoUrl, kind = "client" };
        });
    }

    private static object? OrderBadge(PairOrder? o) => o == null ? null : new
    {
        orderId = o.OrderId,
        status = o.Status,
        statusText = o.Status switch { "pending" => "Esperando respuesta", "accepted" => "Aceptado", "resolved" => "Resuelto", _ => o.Status }
    };

    private static Dictionary<string, object?> Message(ChatMessage m, Guid me)
    {
        var mine = m.SenderId == me;
        var row = new Dictionary<string, object?>
        {
            ["messageId"] = m.Id,
            ["kind"] = m.Kind,
            ["text"] = m.Kind == "encrypted" ? null : m.Text,
            ["mine"] = mine,
            ["senderId"] = m.SenderId,
            ["createdAt"] = m.CreatedAt,
            ["editedAt"] = m.EditedAt,
            ["readAt"] = mine ? m.ReadAt : null,
            ["deliveredAt"] = mine ? m.DeliveredAt : null
        };
        if (m.Kind == "encrypted") row["encrypted"] = System.Text.Json.JsonSerializer.Deserialize<EncryptedChatBody>(m.Text!);
        if (m.Kind == "image") { row["imageUrl"] = m.ImageUrl; row["thumbUrl"] = m.ThumbUrl; row["w"] = m.Width; row["h"] = m.Height; }
        return row;
    }

    private static string Ago(TimeSpan d) => d.TotalMinutes < 1 ? "ahora" : d.TotalHours < 1 ? $"hace {(int)d.TotalMinutes} min"
        : d.TotalDays < 1 ? $"hace {(int)d.TotalHours} h" : $"hace {(int)d.TotalDays} días";

    public static string EncodeCursor(Conversation c) => $"{c.SortAt.Ticks.ToString(CultureInfo.InvariantCulture)}_{c.Id:N}";
    public static (DateTime, Guid)? DecodeCursor(string cursor)
    {
        var parts = cursor.Split('_');
        return cursor.Length <= 64 && parts.Length == 2 && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) &&
            ticks > 0 && ticks < DateTime.MaxValue.Ticks && Guid.TryParseExact(parts[1], "N", out var id)
            ? (new DateTime(ticks, DateTimeKind.Utc), id) : null;
    }

    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación del chat.");
    public sealed record OpenBody(Guid? ProviderId = null, Guid? ClientId = null, Guid? RequestId = null);
    public sealed record SendBody(string? Kind = null, string? Text = null, Guid? ImageUploadId = null);
    public sealed record BlockBody(bool? Blocked = null, bool AlsoReport = false);
    public sealed record ReadBody(Guid? UpToMessageId = null);
}
