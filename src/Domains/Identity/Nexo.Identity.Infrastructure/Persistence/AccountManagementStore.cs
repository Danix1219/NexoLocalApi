using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Infrastructure.Persistence;

public sealed class MongoAccountManagementStore(IdentityCollections collections) : IAccountManagementStore
{
    public Task<AccountView?> ReadAsync(Guid id, CancellationToken ct) => collections.Accounts
        .Find(a => a.Id == id)
        .Project(a => new AccountView(a.Id, a.Email, a.EmailVerifiedAt, a.Name, a.PhotoUrl,
            a.CurrentMode, a.HasProvider, a.Phone, a.Status, a.DeletionRequestedAt,
            a.CreatedAt, a.Revision, a.AuthEpoch, a.AuditHash))
        .FirstOrDefaultAsync(ct)!;

    public async Task<bool> ApplyAsync(AccountView expected, AccountWrite change, DateTime now, CancellationToken ct)
    {
        var f = Builders<Account>.Filter;
        // Los documentos previos al incremento no tienen revision; se interpretan como cero.
        var version = expected.Revision == 0
            ? f.Or(f.Eq(a => a.Revision, 0), f.Exists("revision", false))
            : f.Eq(a => a.Revision, expected.Revision);
        var filter = f.Eq(a => a.Id, expected.Id) & f.Eq(a => a.Status, expected.Status) & version &
                     f.Not(f.SizeGte(a => a.PendingEvents, 64));
        if (change.Status == AccountStatus.Active)
            filter &= f.Gt(a => a.DeletionRequestedAt, now - TimeSpan.FromDays(30));
        if (change.Mode == AccountMode.Provider)
            filter &= f.Eq(a => a.HasProvider, true);

        var u = Builders<Account>.Update;
        var updates = new List<UpdateDefinition<Account>> { u.Set(a => a.UpdatedAt, now), u.Inc(a => a.Revision, 1) };
        if (change.Name is not null) updates.Add(u.Set(a => a.Name, change.Name));
        if (change.SetPhoto) updates.Add(u.Set(a => a.PhotoUrl, change.PhotoUrl));
        if (change.Mode is not null) updates.Add(u.Set(a => a.CurrentMode, change.Mode));
        if (change.Phone is not null) updates.Add(u.Set(a => a.Phone, change.Phone));
        if (change.Status is not null)
        {
            updates.Add(u.Set(a => a.Status, change.Status));
            updates.Add(u.Set(a => a.DeletionRequestedAt, change.DeletionRequestedAt));
            updates.Add(u.Set(a => a.DeletionReason, change.DeletionReason));
            if (change.Status == AccountStatus.PendingDeletion) updates.Add(u.Inc(a => a.AuthEpoch, 1));
        }

        var payload = JsonSerializer.Serialize(new
        {
            accountId = expected.Id,
            name = change.Name ?? expected.Name,
            photoUrl = change.SetPhoto ? change.PhotoUrl : expected.PhotoUrl,
            currentMode = change.Mode ?? expected.CurrentMode,
            phone = change.Phone,
            status = change.Status ?? expected.Status,
            effectiveAt = change.DeletionRequestedAt?.AddDays(30)
        });
        var auditPayload = JsonSerializer.Serialize(new { accountId = expected.Id, type = change.EventType, at = now, previousHash = expected.AuditHash });
        var auditHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(auditPayload)));
        var pending = new List<PendingAccountEvent>
        {
            new(Guid.CreateVersion7(), change.EventType, payload, now),
            new(Guid.CreateVersion7(), "identity.audit", JsonSerializer.Serialize(new { accountId = expected.Id, type = change.EventType, at = now, previousHash = expected.AuditHash, hash = auditHash }), now)
        };
        if (change.Status is not null)
        {
            pending.Add(new PendingAccountEvent(Guid.CreateVersion7(), "identity.account_notice",
                JsonSerializer.Serialize(new AccountNotice(expected.Email, change.Status, change.DeletionRequestedAt?.AddDays(30))), now));
        }
        updates.Add(u.PushEach(a => a.PendingEvents, pending));
        updates.Add(u.Set(a => a.AuditHash, auditHash));
        var result = await collections.Accounts.UpdateOneAsync(filter, u.Combine(updates), cancellationToken: ct);
        return result.ModifiedCount == 1;
    }
}

public sealed record AccountNotice(string Email, string Status, DateTime? EffectiveAt);

/// <summary>Registro con CAS para no reasignar un token que otra peticion acaba de renovar.</summary>
public sealed class MongoDeviceRegistry(IdentityCollections collections) : IDeviceRegistry
{
    public async Task<DeviceRegistration?> RegisterAsync(Guid id, string token, string platform, string appVersion, DateTime now, CancellationToken ct)
    {
        // La unicidad y el TTL son parte de la operacion, no una carrera con el bootstrap.
        await collections.EnsureIndexesAsync(ct);
        var f = Builders<BsonDocument>.Filter;
        var existing = await collections.Devices.Find(f.Eq("fcmToken", token)).FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            var deviceId = Guid.CreateVersion7();
            try
            {
                await collections.Devices.InsertOneAsync(new BsonDocument
                {
                    ["_id"] = new BsonBinaryData(deviceId, GuidRepresentation.Standard),
                    ["accountId"] = new BsonBinaryData(id, GuidRepresentation.Standard),
                    ["fcmToken"] = token,
                    ["platform"] = platform,
                    ["appVersion"] = appVersion,
                    ["lastSeenAt"] = now
                }, cancellationToken: ct);
                return new DeviceRegistration(deviceId, now, true, false);
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return null;
            }
        }

        var owner = existing["accountId"].AsBsonBinaryData.ToGuid();
        var reassigned = owner != id;
        if (reassigned && existing["lastSeenAt"].ToUniversalTime() > now.AddMinutes(-1) &&
            await collections.Accounts.Find(a => a.Id == owner && a.Status == AccountStatus.Active).AnyAsync(ct))
            return null;
        var result = await collections.Devices.UpdateOneAsync(
            f.Eq("_id", existing["_id"]) & f.Eq("accountId", existing["accountId"]) & f.Eq("lastSeenAt", existing["lastSeenAt"]),
            Builders<BsonDocument>.Update.Set("accountId", new BsonBinaryData(id, GuidRepresentation.Standard))
                .Set("lastSeenAt", now).Set("platform", platform).Set("appVersion", appVersion), cancellationToken: ct);
        return result.MatchedCount == 1 ? new DeviceRegistration(existing["_id"].AsBsonBinaryData.ToGuid(), now, false, reassigned) : null;
    }

    public Task RemoveAsync(Guid accountId, string? fcmToken, bool allDevices, CancellationToken ct)
    {
        if (allDevices)
        {
            return collections.Devices.DeleteManyAsync(
                Builders<BsonDocument>.Filter.Eq("accountId", accountId), ct);
        }

        if (string.IsNullOrWhiteSpace(fcmToken))
        {
            return Task.CompletedTask;
        }

        return collections.Devices.DeleteOneAsync(
            Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("accountId", accountId),
                Builders<BsonDocument>.Filter.Eq("fcmToken", fcmToken)),
            ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deviceId, CancellationToken ct)
    {
        var result = await collections.Devices.DeleteOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", deviceId) & Builders<BsonDocument>.Filter.Eq("accountId", id), ct);
        return result.DeletedCount == 1;
    }
}
