using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Nexo.Identity.Domain.Accounts;
using Nexo.Identity.Domain.Sessions;
using Nexo.Identity.Domain.Verification;

namespace Nexo.Identity.Infrastructure.Persistence;

/// <summary>
/// Colecciones propias de S01 y su mapeo. El dominio no conoce MongoDB: el mapa vive aqui.
/// Nombres conforme a docs/modelo-datos.md.
/// </summary>
public sealed class IdentityCollections
{
    public const string AccountsName = "accounts";
    public const string SessionsName = "sessions";
    public const string ChallengesName = "otp_challenges";
    public const string ProofsName = "otp_proofs";

    private static readonly object MapLock = new();
    private static bool mapped;

    public IdentityCollections(IMongoDatabase database)
    {
        EnsureClassMaps();
        Accounts = database.GetCollection<Account>(AccountsName);
        Sessions = database.GetCollection<Session>(SessionsName);
        Challenges = database.GetCollection<EmailChallenge>(ChallengesName);
        Proofs = database.GetCollection<EmailProof>(ProofsName);
    }

    public IMongoCollection<Account> Accounts { get; }

    public IMongoCollection<Session> Sessions { get; }

    public IMongoCollection<EmailChallenge> Challenges { get; }

    public IMongoCollection<EmailProof> Proofs { get; }

    /// <summary>Creacion idempotente de indices y TTL (nv-22). Repetirla no cambia nada.</summary>
    public async Task EnsureIndexesAsync(CancellationToken ct)
    {
        await Accounts.Indexes.CreateOneAsync(
            new CreateIndexModel<Account>(
                Builders<Account>.IndexKeys.Ascending(a => a.Email),
                new CreateIndexOptions { Name = "ux_accounts_email", Unique = true }),
            cancellationToken: ct);

        await Sessions.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Session>(
                    Builders<Session>.IndexKeys.Ascending(s => s.AccessTokenHash),
                    new CreateIndexOptions { Name = "ix_sessions_access" }),
                new CreateIndexModel<Session>(
                    Builders<Session>.IndexKeys.Ascending(s => s.RefreshTokenHash),
                    new CreateIndexOptions { Name = "ux_sessions_refresh", Unique = true }),
                new CreateIndexModel<Session>(
                    Builders<Session>.IndexKeys.Ascending(s => s.PreviousRefreshTokenHash),
                    new CreateIndexOptions { Name = "ix_sessions_refresh_previo" }),
                new CreateIndexModel<Session>(
                    Builders<Session>.IndexKeys.Ascending(s => s.AccountId),
                    new CreateIndexOptions { Name = "ix_sessions_account" }),
                new CreateIndexModel<Session>(
                    Builders<Session>.IndexKeys.Ascending(s => s.FamilyId),
                    new CreateIndexOptions { Name = "ix_sessions_family" }),

                // La sesion desaparece cuando su refresh ya no puede usarse.
                new CreateIndexModel<Session>(
                    Builders<Session>.IndexKeys.Ascending(s => s.RefreshExpiresAt),
                    new CreateIndexOptions { Name = "ttl_sessions_refresh", ExpireAfter = TimeSpan.Zero })
            ],
            cancellationToken: ct);

        await Challenges.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<EmailChallenge>(
                    Builders<EmailChallenge>.IndexKeys
                        .Ascending(c => c.Email)
                        .Ascending(c => c.Purpose)
                        .Descending(c => c.CreatedAt),
                    new CreateIndexOptions { Name = "ix_challenges_email_purpose" }),
                new CreateIndexModel<EmailChallenge>(
                    Builders<EmailChallenge>.IndexKeys.Ascending(c => c.ExpiresAt),
                    new CreateIndexOptions { Name = "ttl_challenges", ExpireAfter = TimeSpan.Zero })
            ],
            cancellationToken: ct);

        await Proofs.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<EmailProof>(
                    Builders<EmailProof>.IndexKeys.Ascending(p => p.TokenHash),
                    new CreateIndexOptions { Name = "ux_proofs_token", Unique = true }),
                new CreateIndexModel<EmailProof>(
                    Builders<EmailProof>.IndexKeys.Ascending(p => p.ExpiresAt),
                    new CreateIndexOptions { Name = "ttl_proofs", ExpireAfter = TimeSpan.Zero })
            ],
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
                "nexolocal-identity",
                new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
                type => type.Namespace?.StartsWith("Nexo.Identity.Domain", StringComparison.Ordinal) == true);

            BsonClassMap.TryRegisterClassMap<Account>(map =>
            {
                map.AutoMap();
                map.MapIdMember(a => a.Id);
            });

            BsonClassMap.TryRegisterClassMap<Session>(map =>
            {
                map.AutoMap();
                map.MapIdMember(s => s.Id);
            });

            BsonClassMap.TryRegisterClassMap<EmailChallenge>(map =>
            {
                map.AutoMap();
                map.MapIdMember(c => c.Id);

                // Proposito legible en la base: facilita auditoria y migracion.
                map.MapMember(c => c.Purpose).SetSerializer(new EnumSerializer<ChallengePurpose>(BsonType.String));
            });

            BsonClassMap.TryRegisterClassMap<EmailProof>(map =>
            {
                map.AutoMap();
                map.MapIdMember(p => p.Id);
                map.MapMember(p => p.Purpose).SetSerializer(new EnumSerializer<ChallengePurpose>(BsonType.String));
            });

            mapped = true;
        }
    }
}
