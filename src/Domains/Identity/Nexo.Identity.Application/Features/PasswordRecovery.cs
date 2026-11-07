using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Common.Results;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Application.Features;

public sealed record ResetPassword(string? VerificationTicket, string? Password, string? PasswordConfirm);

/// <summary>
/// Recuperacion de contrasena (01-auth/post-password-forgot.md y post-password-reset.md).
/// La respuesta del primer paso es identica exista o no la cuenta.
/// </summary>
public sealed class PasswordRecoveryService(
    IAccountRepository accounts,
    IEmailVerificationStore codes,
    IVerificationTicketStore tickets,
    IOtpQuota quota,
    IVerificationCodeFactory factory,
    IPasswordHasher hasher,
    ISessionTerminator sessions,
    ILoginLockStore locks,
    IIdentityEvents events,
    IClock clock)
{
    public async Task<Result<CodeSent>> ForgotAsync(string? email, string ip, CancellationToken ct)
    {
        if (!EmailAddress.IsValid(email))
        {
            return Result<CodeSent>.Failure(NexoCodes.EmailInvalid, "Revisa el correo que escribiste.");
        }

        var normalized = EmailAddress.Normalize(email!);
        var emailHash = NexoRateLimits.HashEmail(normalized);

        // La cuota se consume exista o no la cuenta: si no, el 429 revelaria cuales existen.
        if (!await quota.TryConsumeAsync(emailHash, ip, ct))
        {
            return Result<CodeSent>.Failure(
                NexoCodes.EmailTooManyCodes, "Pediste varios codigos seguidos. Intenta mas tarde.");
        }

        if (await codes.TryOpenResendWindowAsync(emailHash, NexoRateLimits.ResendWindow, ct))
        {
            var account = await accounts.FindByEmailAsync(normalized, ct);
            var code = factory.NewCode();

            // La clave se escribe siempre; el correo solo sale si hay cuenta. Asi el trabajo y el
            // tiempo de respuesta son equivalentes en ambos casos.
            await codes.SaveCodeAsync(
                emailHash, factory.Protect(code), VerificationPurpose.ResetPassword, EmailVerificationService.CodeLifetime, ct);

            if (account is not null && account.CanSignIn)
            {
                await events.EmailCodeRequestedAsync(normalized, code, VerificationPurpose.ResetPassword, ct);
                await events.AuditAsync("password.reset_requested", account.Id, emailHash, ct);
            }
        }

        return Result<CodeSent>.Success(new CodeSent(
            (int)EmailVerificationService.CodeLifetime.TotalSeconds,
            (int)NexoRateLimits.ResendWindow.TotalSeconds,
            EmailVerificationService.CodeLength));
    }

    /// <summary>
    /// Fija la contrasena nueva y cierra todas las sesiones: sin eso, cambiarla no expulsa a quien
    /// ya estaba dentro. No devuelve tokens a proposito.
    /// </summary>
    public async Task<Result<bool>> ResetAsync(ResetPassword request, CancellationToken ct)
    {
        if (PasswordPolicy.Describe(request.Password) is { } problem)
        {
            return Result<bool>.Failure(NexoCodes.PasswordTooWeak, problem);
        }

        if (!string.Equals(request.Password, request.PasswordConfirm, StringComparison.Ordinal))
        {
            return Result<bool>.Failure(NexoCodes.PasswordMismatch, "Las contrasenas no coinciden.");
        }

        var ticket = await tickets.ConsumeAsync(request.VerificationTicket ?? string.Empty, ct);
        if (ticket is null || ticket.Purpose != VerificationPurpose.ResetPassword)
        {
            return Result<bool>.Failure(
                NexoCodes.EmailTicketInvalid, "La verificacion caduco; repite el paso del codigo.");
        }

        var account = await accounts.FindByEmailAsync(ticket.Email, ct);
        if (account is null || account.IsDeleted)
        {
            return Result<bool>.Failure(NexoCodes.ResourceNotFound, "No encontrado.");
        }

        // No se guarda historial de contrasenas: basta comparar con el hash vigente.
        if (hasher.Verify(request.Password!, account.PasswordHash))
        {
            return Result<bool>.Failure(NexoCodes.PasswordSameAsCurrent, "Esa ya es tu contrasena actual.");
        }

        var now = clock.UtcNow;
        await accounts.UpdatePasswordAsync(account.Id, hasher.Hash(request.Password!), now, ct);
        await sessions.RevokeAllForAccountAsync(account.Id, ct);

        // Tras un restablecimiento legitimo seria absurdo seguir bloqueado por intentos fallidos.
        await locks.ClearAsync(NexoRateLimits.HashEmail(account.Email), ct);

        // El aviso es obligatorio: es la unica forma de que el titular note un cambio que no hizo.
        await events.PasswordChangedAsync(account.Id, ct);
        await events.AuditAsync("password.reset_completed", account.Id, null, ct);

        return Result<bool>.Success(true);
    }
}
