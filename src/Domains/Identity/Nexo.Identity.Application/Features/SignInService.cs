using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Common.Results;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Application.Features;

public sealed record SignOutRequest(Guid AccountId, string? Jti, TimeSpan AccessTokenRemaining, string? RefreshToken, string? FcmToken, bool AllDevices);

/// <summary>
/// Comprobacion de credenciales para el grant `password` (01-auth/post-connect-token-password.md).
/// No emite tokens: eso es del servidor de autorizacion.
/// </summary>
public sealed class SignInService(
    IAccountRepository accounts,
    IPasswordHasher hasher,
    ILoginLockStore locks,
    IIdentityEvents events,
    IClock clock)
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Hash señuelo: con un correo inexistente se verifica igual contra este valor, para que el
    /// tiempo de respuesta no delate si la cuenta existe.
    /// </summary>
    private const string DecoyHash = "$argon2id$v=19$m=65536,t=4,p=2$ZGVjb3lzYWx0ZGVjb3lzYQ$ZGVjb3lkZWNveWRlY295ZGVjb3lkZWNveWRlY295ZGVjbw";

    public async Task<Result<Account>> AuthenticateAsync(string? email, string? password, CancellationToken ct)
    {
        if (!EmailAddress.IsValid(email) || string.IsNullOrEmpty(password))
        {
            return Invalid();
        }

        var normalized = EmailAddress.Normalize(email!);
        var emailHash = NexoRateLimits.HashEmail(normalized);

        if (await locks.GetLockAsync(emailHash, ct) is { } remaining)
        {
            return Result<Account>.Failure(
                NexoCodes.AuthAccountLocked,
                $"Demasiados intentos. Vuelve a probar en {Math.Max(1, (int)remaining.TotalMinutes)} minutos.");
        }

        var account = await accounts.FindByEmailAsync(normalized, ct);
        var matches = hasher.Verify(password, account?.PasswordHash ?? DecoyHash);

        if (account is null || account.IsDeleted || !matches)
        {
            if (await locks.RegisterFailureAsync(emailHash, MaxFailedAttempts, LockDuration, ct) is { } locked)
            {
                return Result<Account>.Failure(
                    NexoCodes.AuthAccountLocked,
                    $"Demasiados intentos. Vuelve a probar en {Math.Max(1, (int)locked.TotalMinutes)} minutos.");
            }

            return Invalid();
        }

        if (account.Status == AccountStatus.PendingDeletion)
        {
            return Result<Account>.Failure(
                NexoCodes.AccountPendingDeletion,
                "Tu cuenta esta en proceso de baja. Recupera el acceso para cancelarla.");
        }

        await locks.ClearAsync(emailHash, ct);
        await accounts.UpdateLastLoginAsync(account.Id, clock.UtcNow, ct);
        await events.AuditAsync("session.started", account.Id, emailHash, ct);

        return Result<Account>.Success(account);
    }

    /// <summary>Un solo mensaje para correo inexistente y contrasena incorrecta.</summary>
    private static Result<Account> Invalid() =>
        Result<Account>.Failure(NexoCodes.AuthInvalidCredentials, "Correo o contrasena incorrectos.");
}

/// <summary>
/// Cierre de sesion (01-auth/post-logout.md). Nunca falla por un token ya revocado: cerrar sesion
/// es una accion de seguridad y debe poder completarse siempre.
/// </summary>
public sealed class SignOutService(
    ISessionTerminator sessions,
    IDeviceRegistry devices,
    IIdentityEvents events)
{
    public async Task ExecuteAsync(SignOutRequest request, CancellationToken ct)
    {
        if (request.AllDevices || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await sessions.RevokeAllForAccountAsync(request.AccountId, ct);
        }
        else
        {
            await sessions.RevokeFamilyAsync(request.RefreshToken, ct);
        }

        // El access token sigue siendo valido hasta su `exp`: sin la lista negra, el logout no
        // surtiria efecto durante los minutos que le queden.
        if (!string.IsNullOrWhiteSpace(request.Jti) && request.AccessTokenRemaining > TimeSpan.Zero)
        {
            await sessions.BlacklistAccessTokenAsync(request.Jti, request.AccessTokenRemaining, ct);
        }

        await devices.RemoveAsync(request.AccountId, request.FcmToken, request.AllDevices, ct);
        await events.AuditAsync("session.logout", request.AccountId, null, ct);
    }
}
