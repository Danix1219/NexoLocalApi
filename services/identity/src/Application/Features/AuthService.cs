using NexoLocal.Identity.Application.Ports;
using NexoLocal.Identity.Domain.Accounts;
using NexoLocal.Identity.Domain.Sessions;
using NexoLocal.Identity.Domain.Verification;

namespace NexoLocal.Identity.Application.Features;

public enum AuthError
{
    InvalidInput,
    InvalidCredentials,
    InvalidProof,
    ChallengeNotUsable,
    EmailTaken,
    NotFound
}

public sealed record AuthResult<T>(T? Value, AuthError? Error, string? Detail = null)
{
    public bool Ok => Error is null;

    public static AuthResult<T> Success(T value) => new(value, null);

    public static AuthResult<T> Fail(AuthError error, string? detail = null) => new(default, error, detail);
}

public sealed record ChallengeIssued(string ChallengeId, int RetryAfterSeconds);

public sealed record ProofIssued(string ProofToken, DateTime ExpiresAt);

public sealed record SessionIssued(string SessionId, string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt);

public sealed record AccountRegistered(string AccountId, SessionIssued Session);

public sealed record AccountView(string AccountId, string Email, string DisplayName, DateTime CreatedAt, string ConsentVersion);

/// <summary>
/// Casos de uso de identidad con el correo como identificador (ep-02 a ep-07).
/// No conoce HTTP ni MongoDB: todo pasa por puertos.
/// </summary>
public sealed class AuthService(
    IAccountStore accounts,
    ISessionStore sessions,
    IVerificationStore verification,
    IPasswordHasher passwordHasher,
    ISecretGenerator secrets,
    IEmailCodeSender codeSender,
    IClock clock)
{
    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ProofLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan ResendWindow = TimeSpan.FromSeconds(60);

    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    // ep-03 / EP-001: solicitar codigo de verificacion por correo.
    public async Task<AuthResult<ChallengeIssued>> RequestCodeAsync(string? email, ChallengePurpose purpose, CancellationToken ct)
    {
        if (!Account.IsEmailShaped(email))
        {
            return AuthResult<ChallengeIssued>.Fail(AuthError.InvalidInput, "Correo con formato invalido.");
        }

        var normalized = Account.NormalizeEmail(email!);
        var now = clock.UtcNow;

        // Un desafio vigente no se duplica: se reutiliza con el tiempo de espera restante.
        var active = await verification.FindActiveChallengeAsync(normalized, purpose, now, ct);
        if (active is not null && now - active.CreatedAt < ResendWindow)
        {
            var remaining = (int)Math.Ceiling((active.CreatedAt + ResendWindow - now).TotalSeconds);
            return AuthResult<ChallengeIssued>.Success(new ChallengeIssued(active.Id, Math.Max(remaining, 1)));
        }

        var code = secrets.NewCode();
        var challenge = new EmailChallenge
        {
            Id = secrets.NewId(),
            Email = normalized,
            CodeHash = secrets.HashToken(code),
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now + ChallengeLifetime
        };

        await verification.InsertChallengeAsync(challenge, ct);

        // Respuesta neutral: el envio ocurre exista o no la cuenta, y el resultado no lo revela.
        await codeSender.SendAsync(normalized, code, purpose, ct);

        return AuthResult<ChallengeIssued>.Success(new ChallengeIssued(challenge.Id, (int)ResendWindow.TotalSeconds));
    }

    // ep-03 / EP-002: comprobar el codigo y emitir prueba de control.
    public async Task<AuthResult<ProofIssued>> VerifyCodeAsync(string? challengeId, string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
        {
            return AuthResult<ProofIssued>.Fail(AuthError.InvalidInput);
        }

        var now = clock.UtcNow;
        var challenge = await verification.FindChallengeAsync(challengeId, ct);
        if (challenge is null || !challenge.CanAttempt(now))
        {
            return AuthResult<ProofIssued>.Fail(AuthError.ChallengeNotUsable);
        }

        challenge.Attempts++;
        var matches = FixedTimeEquals(challenge.CodeHash, secrets.HashToken(code));
        if (matches)
        {
            challenge.ConsumedAt = now;
        }

        await verification.ReplaceChallengeAsync(challenge, ct);
        if (!matches)
        {
            return AuthResult<ProofIssued>.Fail(AuthError.ChallengeNotUsable);
        }

        var token = secrets.NewToken();
        var proof = new EmailProof
        {
            Id = secrets.NewId(),
            Email = challenge.Email,
            TokenHash = secrets.HashToken(token),
            Purpose = challenge.Purpose,
            ExpiresAt = now + ProofLifetime
        };

        await verification.InsertProofAsync(proof, ct);
        return AuthResult<ProofIssued>.Success(new ProofIssued(token, proof.ExpiresAt));
    }

    // ep-02 / EP-003: crear la cuenta a partir de la prueba del correo.
    public async Task<AuthResult<AccountRegistered>> RegisterAsync(
        string? proofToken,
        string? password,
        string? displayName,
        string? consentVersion,
        string? deviceId,
        CancellationToken ct)
    {
        if (PasswordProblem(password) is { } problem)
        {
            return AuthResult<AccountRegistered>.Fail(AuthError.InvalidInput, problem);
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length is < 2 or > 60)
        {
            return AuthResult<AccountRegistered>.Fail(AuthError.InvalidInput, "Nombre visible invalido.");
        }

        if (string.IsNullOrWhiteSpace(consentVersion))
        {
            return AuthResult<AccountRegistered>.Fail(AuthError.InvalidInput, "Falta la version de consentimiento aceptada.");
        }

        var proof = await ConsumeProofAsync(proofToken, ChallengePurpose.Registration, ct);
        if (proof is null)
        {
            return AuthResult<AccountRegistered>.Fail(AuthError.InvalidProof);
        }

        var now = clock.UtcNow;
        var account = new Account
        {
            Id = secrets.NewId(),
            Email = proof.Email,
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHasher.Hash(password!),
            ConsentVersion = consentVersion.Trim(),
            CreatedAt = now
        };

        if (!await accounts.TryInsertAsync(account, ct))
        {
            return AuthResult<AccountRegistered>.Fail(AuthError.EmailTaken);
        }

        var session = await StartSessionAsync(account.Id, deviceId, ct);
        return AuthResult<AccountRegistered>.Success(new AccountRegistered(account.Id, session));
    }

    // ep-04 / EP-004: iniciar sesion con correo y contrasena.
    public async Task<AuthResult<SessionIssued>> LoginAsync(string? email, string? password, string? deviceId, CancellationToken ct)
    {
        if (!Account.IsEmailShaped(email) || string.IsNullOrEmpty(password))
        {
            return AuthResult<SessionIssued>.Fail(AuthError.InvalidCredentials);
        }

        var account = await accounts.FindByEmailAsync(Account.NormalizeEmail(email!), ct);

        // Mismo trabajo con cuenta inexistente que con contrasena incorrecta: no revela si el correo existe.
        var hash = account?.PasswordHash ?? passwordHasher.Hash("cuenta-inexistente");
        if (!passwordHasher.Verify(password, hash) || account is null)
        {
            return AuthResult<SessionIssued>.Fail(AuthError.InvalidCredentials);
        }

        return AuthResult<SessionIssued>.Success(await StartSessionAsync(account.Id, deviceId, ct));
    }

    // ep-05 / EP-005: rotar la sesion.
    public async Task<AuthResult<SessionIssued>> RefreshAsync(string? refreshToken, string? deviceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthResult<SessionIssued>.Fail(AuthError.InvalidCredentials);
        }

        var now = clock.UtcNow;
        var presented = secrets.HashToken(refreshToken);
        var session = await sessions.FindByRefreshHashAsync(presented, ct);
        if (session is null)
        {
            return AuthResult<SessionIssued>.Fail(AuthError.InvalidCredentials);
        }

        // Replay: el refresh ya rotado, la sesion revocada o otro dispositivo invalidan toda la familia.
        var isCurrent = string.Equals(session.RefreshTokenHash, presented, StringComparison.Ordinal);
        var deviceMatches = string.IsNullOrWhiteSpace(deviceId)
            || string.Equals(session.DeviceId, deviceId.Trim(), StringComparison.Ordinal);
        if (!isCurrent || !session.IsActive(now) || !deviceMatches)
        {
            await sessions.RevokeFamilyAsync(session.FamilyId, now, ct);
            return AuthResult<SessionIssued>.Fail(AuthError.InvalidCredentials);
        }

        var access = secrets.NewToken();
        var refresh = secrets.NewToken();
        session.PreviousRefreshTokenHash = session.RefreshTokenHash;
        session.AccessTokenHash = secrets.HashToken(access);
        session.RefreshTokenHash = secrets.HashToken(refresh);
        session.AccessExpiresAt = now + AccessLifetime;
        session.RefreshExpiresAt = now + RefreshLifetime;
        await sessions.ReplaceAsync(session, ct);

        return AuthResult<SessionIssued>.Success(new SessionIssued(
            session.Id,
            access,
            session.AccessExpiresAt,
            refresh,
            session.RefreshExpiresAt));
    }

    // ep-05 / EP-007: cerrar sesion, idempotente.
    public Task LogoutAsync(string sessionId, CancellationToken ct) =>
        sessions.RevokeAsync(sessionId, clock.UtcNow, ct);

    // ep-06 / EP-008: cuenta propia del titular del token.
    public async Task<AuthResult<AccountView>> GetAccountAsync(string accountId, CancellationToken ct)
    {
        var account = await accounts.FindByIdAsync(accountId, ct);
        return account is null
            ? AuthResult<AccountView>.Fail(AuthError.NotFound)
            : AuthResult<AccountView>.Success(new AccountView(
                account.Id,
                account.Email,
                account.DisplayName,
                account.CreatedAt,
                account.ConsentVersion));
    }

    // ep-07 / EP-006: restablecer contrasena y revocar sesiones previas.
    public async Task<AuthResult<bool>> ResetPasswordAsync(string? proofToken, string? newPassword, CancellationToken ct)
    {
        if (PasswordProblem(newPassword) is { } problem)
        {
            return AuthResult<bool>.Fail(AuthError.InvalidInput, problem);
        }

        var proof = await ConsumeProofAsync(proofToken, ChallengePurpose.Recovery, ct);
        if (proof is null)
        {
            return AuthResult<bool>.Fail(AuthError.InvalidProof);
        }

        var account = await accounts.FindByEmailAsync(proof.Email, ct);
        var now = clock.UtcNow;
        if (account is not null)
        {
            await accounts.UpdatePasswordAsync(account.Id, passwordHasher.Hash(newPassword!), now, ct);
            await sessions.RevokeAllForAccountAsync(account.Id, now, ct);
        }

        // Respuesta identica exista o no la cuenta; la prueba ya quedo consumida.
        return AuthResult<bool>.Success(true);
    }

    /// <summary>Resuelve la sesion de un token de acceso vigente; null si no sirve.</summary>
    public async Task<Session?> AuthenticateAsync(string accessToken, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var session = await sessions.FindByAccessHashAsync(secrets.HashToken(accessToken), ct);
        return session is not null && session.RevokedAt is null && session.AccessExpiresAt > now ? session : null;
    }

    private async Task<EmailProof?> ConsumeProofAsync(string? token, ChallengePurpose expected, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var proof = await verification.FindProofAsync(secrets.HashToken(token), ct);
        if (proof is null || !proof.IsUsable(clock.UtcNow, expected))
        {
            return null;
        }

        return await verification.TryConsumeProofAsync(proof.Id, clock.UtcNow, ct) ? proof : null;
    }

    private async Task<SessionIssued> StartSessionAsync(string accountId, string? deviceId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var access = secrets.NewToken();
        var refresh = secrets.NewToken();
        var session = new Session
        {
            Id = secrets.NewId(),
            AccountId = accountId,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? "desconocido" : deviceId.Trim(),
            FamilyId = secrets.NewId(),
            AccessTokenHash = secrets.HashToken(access),
            RefreshTokenHash = secrets.HashToken(refresh),
            AccessExpiresAt = now + AccessLifetime,
            RefreshExpiresAt = now + RefreshLifetime,
            CreatedAt = now
        };

        await sessions.InsertAsync(session, ct);
        return new SessionIssued(session.Id, access, session.AccessExpiresAt, refresh, session.RefreshExpiresAt);
    }

    private static string? PasswordProblem(string? password) => password switch
    {
        null or "" => "Falta la contrasena.",
        { Length: < MinPasswordLength } => $"La contrasena necesita al menos {MinPasswordLength} caracteres.",
        { Length: > MaxPasswordLength } => $"La contrasena excede {MaxPasswordLength} caracteres.",
        _ => null
    };

    /// <summary>Comparacion sin fuga de tiempo sobre hashes hexadecimales.</summary>
    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var difference = 0;
        for (var i = 0; i < left.Length; i++)
        {
            difference |= left[i] ^ right[i];
        }

        return difference == 0;
    }
}
