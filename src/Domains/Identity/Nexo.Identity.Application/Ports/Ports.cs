using Nexo.Identity.Domain.Accounts;
using Nexo.Identity.Domain.Sessions;
using Nexo.Identity.Domain.Verification;

namespace Nexo.Identity.Application.Ports;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IAccountStore
{
    Task<Account?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<Account?> FindByIdAsync(string id, CancellationToken ct);

    /// <summary>Devuelve false si el correo ya existe; la unicidad la garantiza el indice, no una lectura previa.</summary>
    Task<bool> TryInsertAsync(Account account, CancellationToken ct);
    Task UpdatePasswordAsync(string accountId, string passwordHash, DateTime updatedAt, CancellationToken ct);
}

public interface ISessionStore
{
    Task InsertAsync(Session session, CancellationToken ct);
    Task<Session?> FindByAccessHashAsync(string accessTokenHash, CancellationToken ct);
    /// <summary>Busca por el refresh vigente o por el anterior, para poder detectar un replay.</summary>
    Task<Session?> FindByRefreshHashAsync(string refreshTokenHash, CancellationToken ct);
    Task<Session?> FindByIdAsync(string sessionId, CancellationToken ct);
    Task ReplaceAsync(Session session, CancellationToken ct);
    Task RevokeAsync(string sessionId, DateTime revokedAt, CancellationToken ct);
    Task RevokeFamilyAsync(string familyId, DateTime revokedAt, CancellationToken ct);
    Task RevokeAllForAccountAsync(string accountId, DateTime revokedAt, CancellationToken ct);
}

public interface IVerificationStore
{
    Task InsertChallengeAsync(EmailChallenge challenge, CancellationToken ct);
    Task<EmailChallenge?> FindChallengeAsync(string challengeId, CancellationToken ct);
    Task<EmailChallenge?> FindActiveChallengeAsync(string normalizedEmail, ChallengePurpose purpose, DateTime now, CancellationToken ct);
    Task ReplaceChallengeAsync(EmailChallenge challenge, CancellationToken ct);
    Task InsertProofAsync(EmailProof proof, CancellationToken ct);
    Task<EmailProof?> FindProofAsync(string tokenHash, CancellationToken ct);

    /// <summary>Marca la prueba como usada solo si seguia sin usar; evita que dos peticiones la gasten a la vez.</summary>
    Task<bool> TryConsumeProofAsync(string proofId, DateTime consumedAt, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ISecretGenerator
{
    string NewId();

    /// <summary>Token opaco de alta entropia; solo se almacena su hash.</summary>
    string NewToken();

    /// <summary>Codigo numerico de verificacion; no se registra ni se devuelve en la respuesta.</summary>
    string NewCode();

    string HashToken(string token);
}

public interface IEmailCodeSender
{
    Task SendAsync(string email, string code, ChallengePurpose purpose, CancellationToken ct);
}
