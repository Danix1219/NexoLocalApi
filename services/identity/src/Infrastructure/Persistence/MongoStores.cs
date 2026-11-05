using MongoDB.Driver;
using NexoLocal.Identity.Application.Ports;
using NexoLocal.Identity.Domain.Accounts;
using NexoLocal.Identity.Domain.Sessions;
using NexoLocal.Identity.Domain.Verification;

namespace NexoLocal.Identity.Infrastructure.Persistence;

public sealed class MongoAccountStore(IdentityCollections collections) : IAccountStore
{
    public Task<Account?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        collections.Accounts.Find(a => a.Email == normalizedEmail).FirstOrDefaultAsync(ct)!;

    public Task<Account?> FindByIdAsync(string id, CancellationToken ct) =>
        collections.Accounts.Find(a => a.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task<bool> TryInsertAsync(Account account, CancellationToken ct)
    {
        try
        {
            await collections.Accounts.InsertOneAsync(account, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // La unicidad del correo la decide el indice, no una lectura previa.
            return false;
        }
    }

    public Task UpdatePasswordAsync(string accountId, string passwordHash, DateTime updatedAt, CancellationToken ct) =>
        collections.Accounts.UpdateOneAsync(
            a => a.Id == accountId,
            Builders<Account>.Update.Set(a => a.PasswordHash, passwordHash).Set(a => a.UpdatedAt, updatedAt),
            cancellationToken: ct);
}

public sealed class MongoSessionStore(IdentityCollections collections) : ISessionStore
{
    public Task InsertAsync(Session session, CancellationToken ct) =>
        collections.Sessions.InsertOneAsync(session, cancellationToken: ct);

    public Task<Session?> FindByAccessHashAsync(string accessTokenHash, CancellationToken ct) =>
        collections.Sessions.Find(s => s.AccessTokenHash == accessTokenHash).FirstOrDefaultAsync(ct)!;

    public Task<Session?> FindByRefreshHashAsync(string refreshTokenHash, CancellationToken ct) =>
        collections.Sessions
            .Find(Builders<Session>.Filter.Or(
                Builders<Session>.Filter.Eq(s => s.RefreshTokenHash, refreshTokenHash),
                Builders<Session>.Filter.Eq(s => s.PreviousRefreshTokenHash, refreshTokenHash)))
            .FirstOrDefaultAsync(ct)!;

    public Task<Session?> FindByIdAsync(string sessionId, CancellationToken ct) =>
        collections.Sessions.Find(s => s.Id == sessionId).FirstOrDefaultAsync(ct)!;

    /// <summary>Rotacion atomica: solo reemplaza si el refresh sigue siendo el que se leyo.</summary>
    public async Task ReplaceAsync(Session session, CancellationToken ct)
    {
        var result = await collections.Sessions.UpdateOneAsync(
            s => s.Id == session.Id && s.RevokedAt == null,
            Builders<Session>.Update
                .Set(s => s.AccessTokenHash, session.AccessTokenHash)
                .Set(s => s.RefreshTokenHash, session.RefreshTokenHash)
                .Set(s => s.PreviousRefreshTokenHash, session.PreviousRefreshTokenHash)
                .Set(s => s.AccessExpiresAt, session.AccessExpiresAt)
                .Set(s => s.RefreshExpiresAt, session.RefreshExpiresAt),
            cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            throw new InvalidOperationException("La sesion dejo de estar activa durante la rotacion.");
        }
    }

    public Task RevokeAsync(string sessionId, DateTime revokedAt, CancellationToken ct) =>
        collections.Sessions.UpdateOneAsync(
            s => s.Id == sessionId && s.RevokedAt == null,
            Builders<Session>.Update.Set(s => s.RevokedAt, revokedAt),
            cancellationToken: ct);

    public Task RevokeFamilyAsync(string familyId, DateTime revokedAt, CancellationToken ct) =>
        collections.Sessions.UpdateManyAsync(
            s => s.FamilyId == familyId && s.RevokedAt == null,
            Builders<Session>.Update.Set(s => s.RevokedAt, revokedAt),
            cancellationToken: ct);

    public Task RevokeAllForAccountAsync(string accountId, DateTime revokedAt, CancellationToken ct) =>
        collections.Sessions.UpdateManyAsync(
            s => s.AccountId == accountId && s.RevokedAt == null,
            Builders<Session>.Update.Set(s => s.RevokedAt, revokedAt),
            cancellationToken: ct);
}

public sealed class MongoVerificationStore(IdentityCollections collections) : IVerificationStore
{
    public Task InsertChallengeAsync(EmailChallenge challenge, CancellationToken ct) =>
        collections.Challenges.InsertOneAsync(challenge, cancellationToken: ct);

    public Task<EmailChallenge?> FindChallengeAsync(string challengeId, CancellationToken ct) =>
        collections.Challenges.Find(c => c.Id == challengeId).FirstOrDefaultAsync(ct)!;

    public Task<EmailChallenge?> FindActiveChallengeAsync(
        string normalizedEmail, ChallengePurpose purpose, DateTime now, CancellationToken ct) =>
        collections.Challenges
            .Find(c => c.Email == normalizedEmail && c.Purpose == purpose && c.ConsumedAt == null && c.ExpiresAt > now)
            .SortByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct)!;

    /// <summary>Guarda el intento y el consumo solo si el desafio seguia sin consumir.</summary>
    public Task ReplaceChallengeAsync(EmailChallenge challenge, CancellationToken ct) =>
        collections.Challenges.UpdateOneAsync(
            c => c.Id == challenge.Id && c.ConsumedAt == null,
            Builders<EmailChallenge>.Update
                .Set(c => c.Attempts, challenge.Attempts)
                .Set(c => c.ConsumedAt, challenge.ConsumedAt),
            cancellationToken: ct);

    public Task InsertProofAsync(EmailProof proof, CancellationToken ct) =>
        collections.Proofs.InsertOneAsync(proof, cancellationToken: ct);

    public Task<EmailProof?> FindProofAsync(string tokenHash, CancellationToken ct) =>
        collections.Proofs.Find(p => p.TokenHash == tokenHash).FirstOrDefaultAsync(ct)!;

    public async Task<bool> TryConsumeProofAsync(string proofId, DateTime consumedAt, CancellationToken ct)
    {
        // Un solo uso garantizado por la base: dos peticiones simultaneas no la gastan dos veces.
        var result = await collections.Proofs.UpdateOneAsync(
            p => p.Id == proofId && p.ConsumedAt == null,
            Builders<EmailProof>.Update.Set(p => p.ConsumedAt, consumedAt),
            cancellationToken: ct);

        return result.ModifiedCount == 1;
    }
}
