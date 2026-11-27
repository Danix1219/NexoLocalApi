using Nexo.Common.Results;
using Nexo.Common.Errors;

namespace Nexo.Engagement.Application.Conversations;

public sealed record ChatPreKey(int Id, string PublicKey);
public sealed record ChatDeviceKeys(int DeviceId, int RegistrationId, string IdentityKey,
    int SignedPreKeyId, string SignedPreKey, string Signature, ChatPreKey[] OneTimeKeys,
    int KyberPreKeyId, string KyberPreKey, string KyberSignature);
public sealed record ChatKeyBundle(int DeviceId, int RegistrationId, string IdentityKey,
    int SignedPreKeyId, string SignedPreKey, string Signature, ChatPreKey? OneTimeKey,
    int KyberPreKeyId, string KyberPreKey, string KyberSignature);

/// <summary>Only public Signal material; private identity/session keys never cross this port.</summary>
public interface IChatKeyStore
{
    Task<bool> RegisterAsync(Guid account, ChatDeviceKeys keys, CancellationToken ct);
    Task<int[]> DevicesAsync(Guid account, CancellationToken ct);
    Task<ChatKeyBundle?> ClaimAsync(Guid account, int deviceId, CancellationToken ct);
    Task RevokeAsync(Guid account, int deviceId, CancellationToken ct);
}

public static class ChatKeyValidation
{
    public static bool Valid(ChatDeviceKeys? keys) => keys is not null && keys.DeviceId is >= 1 and <= 5 &&
        keys.RegistrationId is >= 1 and <= 16380 && keys.SignedPreKeyId >= 0 &&
        Bytes(keys.IdentityKey, 33) && Bytes(keys.SignedPreKey, 33) && Bytes(keys.Signature, 64) &&
        keys.KyberPreKeyId >= 0 && Bytes(keys.KyberPreKey, 1569) && Bytes(keys.KyberSignature, 64) &&
        keys.OneTimeKeys is { Length: >= 1 and <= 100 } &&
        keys.OneTimeKeys.All(k => k is not null && k.Id >= 0 && Bytes(k.PublicKey, 33)) &&
        keys.OneTimeKeys.Select(k => k.Id).Distinct().Count() == keys.OneTimeKeys.Length;

    private static bool Bytes(string? value, int size)
    {
        if (value is null || value.Length != ((size + 2) / 3) * 4) return false;
        Span<byte> buffer = stackalloc byte[1569];
        return Convert.TryFromBase64String(value, buffer, out var written) && written == size &&
            (size != 33 || buffer[0] == 5);
    }
}

/// <summary>Key lookup is scoped to an existing, unblocked conversation.</summary>
public sealed class ChatKeyService(IChatKeyStore keys, IConversationStore conversations)
{
    public Task<int[]> OwnDevicesAsync(Guid account, CancellationToken ct) => keys.DevicesAsync(account, ct);
    public Task<ChatKeyBundle?> ClaimOwnAsync(Guid account, int deviceId, CancellationToken ct) => keys.ClaimAsync(account, deviceId, ct);
    public async Task<Result<bool>> RegisterAsync(Guid account, ChatDeviceKeys? body, CancellationToken ct)
    {
        if (!ChatKeyValidation.Valid(body)) return Fail<bool>(NexoCodes.ValidationFailed);
        return await keys.RegisterAsync(account, body!, ct)
            ? Result<bool>.Success(true) : Fail<bool>(NexoCodes.ValidationFailed);
    }

    public async Task<Result<int[]>> DevicesAsync(Guid me, Guid conversationId, CancellationToken ct)
    {
        var conversation = await conversations.FindAsync(conversationId, me, ct);
        if (conversation is null) return Fail<int[]>(NexoCodes.ConversationNotFound);
        if (conversation.BlockedBy.Length > 0) return Fail<int[]>(NexoCodes.ConversationBlocked);
        var peer = conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId;
        return Result<int[]>.Success(await keys.DevicesAsync(peer, ct));
    }

    public async Task<Result<ChatKeyBundle>> ClaimAsync(Guid me, Guid conversationId, int deviceId, CancellationToken ct)
    {
        if (deviceId is < 1 or > 5) return Fail<ChatKeyBundle>(NexoCodes.ValidationFailed);
        var conversation = await conversations.FindAsync(conversationId, me, ct);
        if (conversation is null) return Fail<ChatKeyBundle>(NexoCodes.ConversationNotFound);
        if (conversation.BlockedBy.Length > 0) return Fail<ChatKeyBundle>(NexoCodes.ConversationBlocked);
        var peer = conversation.IsClient(me) ? conversation.ProviderAccountId : conversation.ClientId;
        var bundle = await keys.ClaimAsync(peer, deviceId, ct);
        return bundle is null ? Fail<ChatKeyBundle>(NexoCodes.ResourceNotFound) : Result<ChatKeyBundle>.Success(bundle);
    }

    public Task RevokeAsync(Guid account, int deviceId, CancellationToken ct) => keys.RevokeAsync(account, deviceId, ct);


    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación de claves del chat.");
}
