using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Common.Results;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Application.Features;

public sealed record CodeSent(int ExpiresInSeconds, int ResendAfterSeconds, int CodeLength);

public sealed record TicketIssued(string VerificationTicket, int ExpiresInSeconds, bool AccountExists);

/// <summary>
/// Envio y confirmacion del codigo de correo (01-auth/post-email-verification*.md).
/// Redis guarda el HMAC; el outbox conserva el codigo hasta su envio. No se escribe en logs.
/// </summary>
public sealed class EmailVerificationService(
    IEmailVerificationStore codes,
    IVerificationTicketStore tickets,
    IOtpQuota quota,
    IVerificationCodeFactory factory,
    IAccountRepository accounts,
    IIdentityEvents events)
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(30);
    public const int CodeLength = 6;
    public const int MaxAttempts = 5;

    /// <summary>
    /// Envia un codigo. La respuesta es la misma exista o no la cuenta: delatar que un correo
    /// esta registrado permitiria enumerar cuentas.
    /// </summary>
    public async Task<Result<CodeSent>> SendAsync(string? email, string? purpose, string ip, CancellationToken ct)
    {
        if (!EmailAddress.IsValid(email))
        {
            return Result<CodeSent>.Failure(NexoCodes.EmailInvalid, "Revisa el correo que escribiste.");
        }

        if (!VerificationPurpose.Exists(purpose))
        {
            return Result<CodeSent>.Failure(NexoCodes.ValidationFailed, "El proposito no es valido.");
        }

        var normalized = EmailAddress.Normalize(email!);
        var emailHash = NexoRateLimits.HashEmail(normalized);

        if (!await quota.TryConsumeAsync(emailHash, ip, ct))
        {
            return Result<CodeSent>.Failure(
                NexoCodes.EmailTooManyCodes, "Pediste varios codigos seguidos. Intenta mas tarde.");
        }

        // La ventana de reenvio no es un error de validacion: se responde igual que un envio nuevo
        // para no revelar nada por la diferencia de respuesta.
        if (await codes.TryOpenResendWindowAsync(emailHash, NexoRateLimits.ResendWindow, ct))
        {
            var code = factory.NewCode();
            await codes.SaveCodeAsync(emailHash, factory.Protect(code), purpose!, CodeLifetime, ct);

            // El envio se encola: un SMTP lento no debe cambiar el tiempo de respuesta, porque ese
            // tiempo delataria si la cuenta existe.
            await events.EmailCodeRequestedAsync(normalized, code, purpose!, ct);
        }

        if (purpose == VerificationPurpose.ChangeEmail)
        {
            await events.AuditAsync("email.change_requested", null, emailHash, ct);
        }

        return Result<CodeSent>.Success(new CodeSent(
            (int)CodeLifetime.TotalSeconds,
            (int)NexoRateLimits.ResendWindow.TotalSeconds,
            CodeLength));
    }

    /// <summary>
    /// Confirma el codigo y entrega el ticket. `accountExists` solo se revela aqui: en este punto
    /// quien pregunta ya demostro que controla el buzon.
    /// </summary>
    public async Task<Result<TicketIssued>> ConfirmAsync(string? email, string? code, CancellationToken ct)
    {
        if (!EmailAddress.IsValid(email))
        {
            return Result<TicketIssued>.Failure(NexoCodes.EmailInvalid, "Revisa el correo que escribiste.");
        }

        if (string.IsNullOrEmpty(code) || code.Length != CodeLength || !code.All(char.IsAsciiDigit))
        {
            return Result<TicketIssued>.Failure(NexoCodes.ValidationFailed, "El codigo son 6 digitos.");
        }

        var normalized = EmailAddress.Normalize(email!);
        var emailHash = NexoRateLimits.HashEmail(normalized);

        var (check, stored) = await codes.CheckAndConsumeAsync(emailHash, factory.Protect(code), MaxAttempts, ct);

        if (check is not CodeCheck.Ok || stored is null)
        {
            return Result<TicketIssued>.Failure(check switch
            {
                CodeCheck.Expired => new NexoError(NexoCodes.EmailCodeExpired, "El codigo caduco; pide uno nuevo."),
                CodeCheck.TooManyAttempts => new NexoError(
                    NexoCodes.EmailTooManyAttempts, "Demasiados intentos; pide un codigo nuevo."),
                _ => new NexoError(NexoCodes.EmailCodeInvalid, "El codigo no coincide.")
            });
        }

        var ticket = await tickets.IssueAsync(normalized, stored.Purpose, TicketLifetime, ct);
        var account = await accounts.FindByEmailAsync(normalized, ct);

        return Result<TicketIssued>.Success(new TicketIssued(
            ticket,
            (int)TicketLifetime.TotalSeconds,
            account is not null && !account.IsDeleted));
    }
}
