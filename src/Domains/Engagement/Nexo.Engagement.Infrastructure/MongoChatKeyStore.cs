using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Conversations;

namespace Nexo.Engagement.Infrastructure;

/// <summary>One immutable identity per device slot; revocation is a tombstone, never key reuse.</summary>
public sealed class MongoChatKeyStore([FromKeyedServices(NexoDatabaseKeys.Engagement)] IMongoDatabase database) : IChatKeyStore
{
    private readonly IMongoCollection<BsonDocument> devices = database.GetCollection<BsonDocument>("chat_public_keys");
    private static string Id(Guid account, int device) => $"{account:D}:{device}";

    public async Task<bool> RegisterAsync(Guid account, ChatDeviceKeys keys, CancellationToken ct)
    {
        var doc = new BsonDocument
        {
            ["_id"] = Id(account, keys.DeviceId),
            ["account"] = account.ToString("D"),
            ["deviceId"] = keys.DeviceId,
            ["registrationId"] = keys.RegistrationId,
            ["identity"] = keys.IdentityKey,
            ["signedId"] = keys.SignedPreKeyId,
            ["signedKey"] = keys.SignedPreKey,
            ["signature"] = keys.Signature,
            ["kyberId"] = keys.KyberPreKeyId,
            ["kyberKey"] = keys.KyberPreKey,
            ["kyberSignature"] = keys.KyberSignature,
            ["revoked"] = false,
            ["createdAt"] = DateTime.UtcNow,
            ["preKeys"] = new BsonArray(keys.OneTimeKeys.Select(k => new BsonDocument { ["id"] = k.Id, ["key"] = k.PublicKey }))
        };
        try { await devices.InsertOneAsync(doc, cancellationToken: ct); return true; }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Idempotent registration must never refill consumed one-time keys or replace identity.
            var old = await devices.Find(new BsonDocument("_id", doc["_id"])).FirstOrDefaultAsync(ct);
            return old is not null && !old["revoked"].AsBoolean &&
                old["identity"] == doc["identity"] && old["signedId"] == doc["signedId"] &&
                old["signedKey"] == doc["signedKey"] && old["signature"] == doc["signature"] &&
                old["registrationId"] == doc["registrationId"] && old["kyberId"] == doc["kyberId"] &&
                old["kyberKey"] == doc["kyberKey"] && old["kyberSignature"] == doc["kyberSignature"];
        }
    }

    public async Task<int[]> DevicesAsync(Guid account, CancellationToken ct) =>
        (await devices.Find(new BsonDocument { ["account"] = account.ToString("D"), ["revoked"] = false }).ToListAsync(ct))
        .Select(d => d["deviceId"].AsInt32).Order().ToArray();

    public async Task<ChatKeyBundle?> ClaimAsync(Guid account, int deviceId, CancellationToken ct)
    {
        // Return the previous document from a single atomic pop: a prekey cannot be claimed twice.
        var doc = await devices.FindOneAndUpdateAsync(
            new BsonDocument { ["_id"] = Id(account, deviceId), ["revoked"] = false, ["preKeys.0"] = new BsonDocument("$exists", true) },
            new BsonDocument("$pop", new BsonDocument("preKeys", -1)),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.Before }, ct);
        if (doc is null) return null;
        var key = doc["preKeys"][0].AsBsonDocument;
        return new(deviceId, doc["registrationId"].AsInt32, doc["identity"].AsString,
            doc["signedId"].AsInt32, doc["signedKey"].AsString, doc["signature"].AsString,
            new(key["id"].AsInt32, key["key"].AsString),
            doc["kyberId"].AsInt32, doc["kyberKey"].AsString, doc["kyberSignature"].AsString);
    }

    public async Task RevokeAsync(Guid account, int deviceId, CancellationToken ct) =>
        await devices.UpdateOneAsync(new BsonDocument("_id", Id(account, deviceId)),
            new BsonDocument("$set", new BsonDocument { ["revoked"] = true, ["preKeys"] = new BsonArray() }), cancellationToken: ct);
}
