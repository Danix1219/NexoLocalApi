using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Infrastructure.Persistence;

/// <summary>Colecciones propias de Identity en `nexo_identity`. Ningun otro dominio las escribe.</summary>
public sealed class IdentityCollections
{
    public const string AccountsName = "accounts";
    public const string DevicesName = "devices";
    public const string RevokedTokensName = "revoked_tokens";

    private static readonly Lock MapLock = new();
    private static bool mapped;

    public IdentityCollections(IMongoDatabase database)
    {
        EnsureClassMaps();
        Accounts = database.GetCollection<Account>(AccountsName);
        Devices = database.GetCollection<BsonDocument>(DevicesName);
        RevokedTokens = database.GetCollection<BsonDocument>(RevokedTokensName);
    }

    public IMongoCollection<Account> Accounts { get; }

    public IMongoCollection<BsonDocument> Devices { get; }

    public IMongoCollection<BsonDocument> RevokedTokens { get; }

    /// <summary>Indices y TTL, creacion idempotente: repetirlo no cambia nada.</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct)
    {
        await Accounts.Indexes.CreateOneAsync(
            new CreateIndexModel<Account>(
                Builders<Account>.IndexKeys.Ascending(account => account.Email),
                new CreateIndexOptions { Name = "ux_accounts_email", Unique = true }),
            cancellationToken: ct);

        await Devices.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("fcmToken"),
                    new CreateIndexOptions { Name = "ux_devices_fcm", Unique = true }),
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("accountId"),
                    new CreateIndexOptions { Name = "ix_devices_account" })
            ],
            cancellationToken: ct);

        // Respaldo auditable de la lista negra; el TTL la limpia sola cuando el token ya caduco.
        await RevokedTokens.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                new CreateIndexOptions { Name = "ttl_revoked_tokens", ExpireAfter = TimeSpan.Zero }),
            cancellationToken: ct);
    }

    private static void EnsureClassMaps()
    {
        lock (MapLock)
        {
            if (mapped)
            {
                return;
            }

            ConventionRegistry.Register(
                "nexo-identity",
                new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
                type => type.Namespace?.StartsWith("Nexo.Identity.Domain", StringComparison.Ordinal) == true);

            BsonClassMap.TryRegisterClassMap<Account>(map =>
            {
                map.AutoMap();
                map.MapIdMember(account => account.Id);
            });

            mapped = true;
        }
    }
}

public sealed class MongoAccountRepository(IdentityCollections collections) : IAccountRepository
{
    public Task<Account?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        collections.Accounts.Find(account => account.Email == normalizedEmail).FirstOrDefaultAsync(ct)!;

    public Task<Account?> FindByIdAsync(Guid id, CancellationToken ct) =>
        collections.Accounts.Find(account => account.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task<bool> TryInsertAsync(Account account, CancellationToken ct)
    {
        try
        {
            await collections.Accounts.InsertOneAsync(account, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // El indice unico resuelve la carrera de dos registros simultaneos.
            return false;
        }
    }

    public Task UpdatePasswordAsync(Guid accountId, string passwordHash, DateTime changedAt, CancellationToken ct) =>
        collections.Accounts.UpdateOneAsync(
            account => account.Id == accountId,
            Builders<Account>.Update
                .Set(account => account.PasswordHash, passwordHash)
                .Set(account => account.PasswordChangedAt, changedAt)
                .Set(account => account.UpdatedAt, changedAt),
            cancellationToken: ct);

    public Task UpdateLastLoginAsync(Guid accountId, DateTime at, CancellationToken ct) =>
        collections.Accounts.UpdateOneAsync(
            account => account.Id == accountId,
            Builders<Account>.Update.Set(account => account.LastLoginAt, at),
            cancellationToken: ct);
}

/// <summary>Baja del dispositivo: tras cerrar sesion no deben seguir llegando notificaciones.</summary>
public sealed class MongoDeviceRegistry(IdentityCollections collections) : IDeviceRegistry
{
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
}
