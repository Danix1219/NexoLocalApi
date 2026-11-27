using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Engagement.Application.Conversations;

public sealed record ChatCipherEnvelope(int Version, Guid MessageId, int SenderDevice,
    Guid Recipient, int RecipientDevice, int MessageType, string Ciphertext);
public sealed record EncryptedChatBody(ChatCipherEnvelope[] Envelopes);

public sealed class EncryptedChatService(IConversationStore store, IChatKeyStore keys, IChatRealtime live, TimeProvider clock)
{
    public async Task<Result<object>> MutateAsync(Guid me, Guid conversationId, Guid target, string? operation,
        EncryptedChatBody? body, bool delete, CancellationToken ct)
    {
        if (target == Guid.Empty || !Guid.TryParse(operation, out var key)) return Fail(NexoCodes.ValidationFailed);
        var conversation = await store.FindAsync(conversationId, me, ct);
        if (conversation is null) return Fail(NexoCodes.ConversationNotFound);
        if (conversation.BlockedBy.Length != 0) return Fail(NexoCodes.ConversationBlocked);
        if (!conversation.Encrypted) return Fail(NexoCodes.ValidationFailed);
        var peer = conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId;
        if (!delete)
        {
            if (body?.Envelopes is not { Length: > 0 and <= 10 } || body.Envelopes[0] is null) return Fail(NexoCodes.ValidationFailed);
            var mine = await keys.DevicesAsync(me, ct);
            var theirs = await keys.DevicesAsync(peer, ct);
            if (mine.Length == 0 || theirs.Length == 0) return Fail(NexoCodes.ServerDependencyUnavailable);
            var expected = mine.Where(d => d != body.Envelopes[0].SenderDevice).Select(d => (me, d))
                .Concat(theirs.Select(d => (peer, d))).ToHashSet();
            var supplied = new HashSet<(Guid, int)>();
            foreach (var envelope in body.Envelopes)
                if (envelope is null || envelope.Version != 1 || envelope.MessageId != key ||
                    envelope.SenderDevice != body.Envelopes[0].SenderDevice || !mine.Contains(envelope.SenderDevice) ||
                    envelope.MessageType is not (2 or 3) || !expected.Contains((envelope.Recipient, envelope.RecipientDevice)) ||
                    !supplied.Add((envelope.Recipient, envelope.RecipientDevice)) || !ValidCiphertext(envelope.Ciphertext))
                    return Fail(NexoCodes.ValidationFailed);
            if (!supplied.SetEquals(expected)) return Fail(NexoCodes.ValidationFailed);
        }
        var saved = await store.MutateAsync(conversation, me, target, delete ? null : JsonSerializer.Serialize(body), key,
            clock.GetUtcNow().UtcDateTime, ct);
        if (saved is null) return Fail(NexoCodes.ConversationNotFound);
        var changed = new { conversationId, messageId = target };
        await live.SendAsync(peer, "messageUpdated", changed, ct);
        await live.SendAsync(me, "messageUpdated", changed, ct);
        return Result<object>.Success(new { messageId = target, editedAt = saved.EditedAt, deleted = saved.Kind == "deleted" });
    }

    public async Task<Result<object>> EnableAsync(Guid me, Guid conversationId, CancellationToken ct)
    {
        var conversation = await store.FindAsync(conversationId, me, ct);
        if (conversation is null) return Fail(NexoCodes.ConversationNotFound);
        var peer = conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId;
        if ((await keys.DevicesAsync(me, ct)).Length == 0 || (await keys.DevicesAsync(peer, ct)).Length == 0)
            return Fail(NexoCodes.ServerDependencyUnavailable);
        return await store.EnableEncryptionAsync(conversationId, me, ct)
            ? Result<object>.Success(new { encrypted = true }) : Fail(NexoCodes.ConversationBlocked);
    }
    public async Task<Result<object>> SendAsync(Guid me, Guid conversationId, string? operation, EncryptedChatBody? body, CancellationToken ct)
    {
        if (!Guid.TryParse(operation, out var key) || body?.Envelopes is not { Length: > 0 and <= 10 } || body.Envelopes[0] is null) return Fail(NexoCodes.ValidationFailed);
        var conversation = await store.FindAsync(conversationId, me, ct);
        if (conversation is null) return Fail(NexoCodes.ConversationNotFound);
        if (conversation.BlockedBy.Length != 0) return Fail(NexoCodes.ConversationBlocked);
        if (!conversation.Encrypted) return Fail(NexoCodes.ValidationFailed);
        var peer = conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId;
        var mine = await keys.DevicesAsync(me, ct);
        var theirs = await keys.DevicesAsync(peer, ct);
        if (mine.Length == 0 || theirs.Length == 0) return Fail(NexoCodes.ServerDependencyUnavailable);
        var expected = mine.Where(d => d != body.Envelopes[0].SenderDevice).Select(d => (me, d))
            .Concat(theirs.Select(d => (peer, d))).ToHashSet();
        var supplied = new HashSet<(Guid, int)>();
        foreach (var envelope in body.Envelopes)
        {
            if (envelope is null || envelope.Version != 1 || envelope.MessageId != key ||
                envelope.SenderDevice != body.Envelopes[0].SenderDevice || !mine.Contains(envelope.SenderDevice) ||
                envelope.MessageType is not (2 or 3) || !expected.Contains((envelope.Recipient, envelope.RecipientDevice)) ||
                !supplied.Add((envelope.Recipient, envelope.RecipientDevice)) || !ValidCiphertext(envelope.Ciphertext)) return Fail(NexoCodes.ValidationFailed);
        }
        // Every current device must receive a copy; no accidental downgrade or omitted peer.
        if (!supplied.SetEquals(expected)) return Fail(NexoCodes.ValidationFailed);
        var message = new ChatMessage(Guid.CreateVersion7(), conversationId, me, "encrypted", JsonSerializer.Serialize(body),
            null, null, null, null, clock.GetUtcNow().UtcDateTime, null);
        var saved = await store.SendAsync(conversation, message, "Mensaje cifrado", key, ct);
        if (saved is null) return Fail(NexoCodes.ConversationBlocked);
        if (saved.Kind != "encrypted" || saved.Text != message.Text) return Fail(NexoCodes.RequestIdempotencyKeyReused);
        var notification = new { conversationId, messageId = saved.Id };
        await live.SendAsync(peer, "messageReceived", notification, ct);
        await live.SendAsync(me, "messageReceived", notification, ct);
        return Result<object>.Success(new { messageId = saved.Id, createdAt = saved.CreatedAt });
    }

    private static bool ValidCiphertext(string? value)
    {
        if (value is null || value.Length is < 32 or > 65536) return false;
        var bytes = new byte[49152];
        return Convert.TryFromBase64String(value, bytes, out var count) && count >= 24;
    }
    private static Result<object> Fail(string code) => Result<object>.Failure(code, "No se pudo enviar el mensaje cifrado.");
}
