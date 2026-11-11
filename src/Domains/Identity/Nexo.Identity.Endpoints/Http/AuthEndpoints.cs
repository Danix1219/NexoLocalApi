using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Common.Redis;
using Nexo.Common.Results;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Domain.Accounts;
using OpenIddict.Abstractions;

namespace Nexo.Identity.Endpoints.Http;

// Cuerpos de peticion de 01-auth. El correo es la identidad de la cuenta.
public sealed record SendEmailCodeBody(string? Email, string? Purpose);

public sealed record ConfirmEmailCodeBody(string? Email, string? Code);

public sealed record SearchLocationBody(double Lat, double Lng);

public sealed record CreateAccountBody(
    string? VerificationTicket,
    string? Name,
    string? Password,
    bool AcceptedTerms,
    SearchLocationBody? SearchLocation,
    string? SearchZoneLabel,
    int? SearchRadiusKm,
    string? Intent);

public sealed record LogoutBody(string? RefreshToken, string? FcmToken, bool? AllDevices);

public sealed record ForgotPasswordBody(string? Email);

public sealed record ResetPasswordBody(string? VerificationTicket, string? Password, string? PasswordConfirm);

// Respuestas publicas: nunca llevan hash, ni el codigo, ni datos de cuentas ajenas.
public sealed record CodeSentResponse(int ExpiresInSeconds, int ResendAfterSeconds, int CodeLength);

public sealed record TicketResponse(string VerificationTicket, int ExpiresInSeconds, bool AccountExists);

public sealed record AccountCreatedResponse(
    string AccountId, string Email, string Name, string CurrentMode, bool HasProvider, string NextStep);

/// <summary>Superficie HTTP de 01-auth, montada por el grupo `/v1/identity`.</summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        // post-email-verification.md · respuesta identica exista o no la cuenta.
        group.MapPost("/email/verification", async (
            SendEmailCodeBody body, HttpContext http, EmailVerificationService service, CancellationToken ct) =>
        {
            if (!VerificationPurpose.IsPublicRequest(body.Purpose))
            {
                return NexoProblem.From(NexoCodes.ValidationFailed, "El proposito no es valido.", http);
            }

            // Cambiar el correo exige una sesion; registro y recuperacion siguen siendo publicos.
            if (body.Purpose == VerificationPurpose.ChangeEmail &&
                (http.User.Identity?.IsAuthenticated != true ||
                 !Guid.TryParse(http.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value, out _)))
            {
                return NexoProblem.From(NexoCodes.AuthTokenExpired, "Inicia sesion para cambiar el correo.", http);
            }

            var result = await service.SendAsync(body.Email, body.Purpose, ClientIp(http), ct);
            return result.Match(
                value => Results.Accepted(
                    value: new CodeSentResponse(value.ExpiresInSeconds, value.ResendAfterSeconds, value.CodeLength)),
                error => NexoProblem.From(error, http));
        }).RequireRateLimiting(NexoRateLimits.Auth);

        // post-email-verification-confirm.md · `accountExists` solo se revela aqui.
        group.MapPost("/email/verification/confirm", async (
            ConfirmEmailCodeBody body, HttpContext http, EmailVerificationService service, CancellationToken ct) =>
        {
            var result = await service.ConfirmAsync(body.Email, body.Code, ct);
            return result.Match(
                value => Results.Ok(
                    new TicketResponse(value.VerificationTicket, value.ExpiresInSeconds, value.AccountExists)),
                error => NexoProblem.From(error, http));
        }).RequireRateLimiting(NexoRateLimits.Auth);

        // post-accounts.md · Idempotency-Key obligatorio.
        group.MapPost("/accounts", async (
            CreateAccountBody body,
            HttpContext http,
            AccountRegistrationService service,
            IIdempotencyStore idempotency,
            CancellationToken ct) =>
        {
            var key = http.Request.Headers["Idempotency-Key"].ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                return NexoProblem.From(
                    NexoCodes.RequestIdempotencyKeyMissing, "Falta la cabecera Idempotency-Key.", http);
            }

            if (!await idempotency.TryReserveAsync($"nexo:idem:{key}", TimeSpan.FromHours(24), ct))
            {
                return NexoProblem.From(
                    NexoCodes.RequestIdempotencyKeyReused, "Esa llave ya se uso.", http);
            }

            var location = body.SearchLocation is null
                ? null
                : new SearchLocation(
                    body.SearchLocation.Lat,
                    body.SearchLocation.Lng,
                    body.SearchZoneLabel ?? string.Empty,
                    body.SearchRadiusKm ?? 10);

            var result = await service.CreateAsync(
                new RegisterAccount(
                    body.VerificationTicket, body.Name, body.Password, body.AcceptedTerms, location, body.Intent),
                ct);

            return result.Match(
                value => Results.Created(
                    "/v1/identity/me",
                    new AccountCreatedResponse(
                        value.AccountId.ToString(),
                        value.Email,
                        value.Name,
                        value.CurrentMode,
                        value.HasProvider,
                        value.NextStep)),
                error => NexoProblem.From(error, http));
        }).RequireRateLimiting(NexoRateLimits.Auth);

        // post-password-forgot.md · no existe 404 aqui: seria enumeracion de cuentas.
        group.MapPost("/password/forgot", async (
            ForgotPasswordBody body, HttpContext http, PasswordRecoveryService service, CancellationToken ct) =>
        {
            var result = await service.ForgotAsync(body.Email, ClientIp(http), ct);
            return result.Match(
                value => Results.Accepted(
                    value: new CodeSentResponse(value.ExpiresInSeconds, value.ResendAfterSeconds, value.CodeLength)),
                error => NexoProblem.From(error, http));
        }).RequireRateLimiting(NexoRateLimits.Auth);

        // post-password-reset.md · sin tokens a proposito: hay que iniciar sesion con la nueva.
        group.MapPost("/password/reset", async (
            ResetPasswordBody body, HttpContext http, PasswordRecoveryService service, CancellationToken ct) =>
        {
            var result = await service.ResetAsync(
                new ResetPassword(body.VerificationTicket, body.Password, body.PasswordConfirm), ct);

            return result.Match(
                _ => Results.NoContent(),
                error => NexoProblem.From(error, http));
        }).RequireRateLimiting(NexoRateLimits.Auth);

        // post-logout.md · idempotente: nunca falla por un token ya revocado.
        group.MapPost("/logout", async (
            LogoutBody? body, HttpContext http, SignOutService service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(http.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value, out var accountId))
            {
                return NexoProblem.From(NexoCodes.AuthTokenExpired, "La sesion ya no es valida.", http);
            }

            var jti = http.User.FindFirst(OpenIddictConstants.Claims.JwtId)?.Value;
            var expires = http.User.FindFirst(OpenIddictConstants.Claims.ExpiresAt)?.Value;
            var remaining = long.TryParse(expires, out var epoch)
                ? DateTimeOffset.FromUnixTimeSeconds(epoch) - DateTimeOffset.UtcNow
                : TimeSpan.Zero;

            await service.ExecuteAsync(
                new SignOutRequest(accountId, jti, remaining, body?.RefreshToken, body?.FcmToken, body?.AllDevices ?? false),
                ct);

            return Results.NoContent();
        })
        .RequireAuthorization()
        .RequireRateLimiting(NexoRateLimits.Auth);

        return group;
    }

    private static string ClientIp(HttpContext http) =>
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
}
