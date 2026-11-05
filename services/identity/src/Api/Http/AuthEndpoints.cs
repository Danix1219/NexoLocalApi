using Microsoft.AspNetCore.Http.HttpResults;
using NexoLocal.Identity.Application.Features;
using NexoLocal.Identity.Domain.Verification;

namespace NexoLocal.Identity.Api.Http;

// Cuerpos de peticion. El correo es el identificador de cuenta (ep-02 a ep-07).
public sealed record RequestCodeBody(string? Email, string? Purpose);

public sealed record VerifyCodeBody(string? ChallengeId, string? Code);

public sealed record RegisterBody(string? ProofToken, string? Password, string? DisplayName, string? ConsentVersion, string? DeviceId);

public sealed record LoginBody(string? Email, string? Password, string? DeviceId);

public sealed record RefreshBody(string? RefreshToken, string? DeviceId);

public sealed record PasswordResetBody(string? ProofToken, string? NewPassword);

// Respuestas publicas. No exponen hashes, familia de sesion ni estado interno.
public sealed record ChallengeResponse(string ChallengeId, int RetryAfter);

public sealed record ProofResponse(string ProofToken, DateTime ExpiresAt);

public sealed record SessionResponse(string SessionId, string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt);

public sealed record RegisteredResponse(string AccountId, SessionResponse Session);

public sealed record MeResponse(string AccountId, string Email, string DisplayName, DateTime CreatedAt, string ConsentVersion);

/// <summary>Superficie HTTP de S01 para los primeros siete endpoints del solicitante.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // ep-03 / EP-001
        app.MapPost("/auth/otp", async (RequestCodeBody body, AuthService auth, CancellationToken ct) =>
        {
            if (!TryParsePurpose(body.Purpose, out var purpose))
            {
                return Problem("purpose debe ser registration o recovery.");
            }

            var result = await auth.RequestCodeAsync(body.Email, purpose, ct);
            return result.Ok
                ? Results.Accepted(value: new ChallengeResponse(result.Value!.ChallengeId, result.Value.RetryAfterSeconds))
                : Translate(result.Error!.Value, result.Detail);
        });

        // ep-03 / EP-002
        app.MapPost("/auth/otp/verify", async (VerifyCodeBody body, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.VerifyCodeAsync(body.ChallengeId, body.Code, ct);
            return result.Ok
                ? Results.Ok(new ProofResponse(result.Value!.ProofToken, result.Value.ExpiresAt))
                : Translate(result.Error!.Value, result.Detail);
        });

        // ep-02 / EP-003
        app.MapPost("/auth/register", async (RegisterBody body, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RegisterAsync(body.ProofToken, body.Password, body.DisplayName, body.ConsentVersion, body.DeviceId, ct);
            if (!result.Ok)
            {
                return Translate(result.Error!.Value, result.Detail);
            }

            var registered = result.Value!;
            return Results.Created("/api/v1/me", new RegisteredResponse(registered.AccountId, ToResponse(registered.Session)));
        });

        // ep-04 / EP-004
        app.MapPost("/auth/login", async (LoginBody body, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(body.Email, body.Password, body.DeviceId, ct);
            return result.Ok
                ? Results.Ok(ToResponse(result.Value!))
                : Translate(result.Error!.Value, result.Detail);
        });

        // ep-05 / EP-005
        app.MapPost("/auth/refresh", async (RefreshBody body, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(body.RefreshToken, body.DeviceId, ct);
            return result.Ok
                ? Results.Ok(ToResponse(result.Value!))
                : Translate(result.Error!.Value, result.Detail);
        });

        // ep-05 / EP-007: idempotente, siempre 204 para quien presenta un token valido.
        app.MapPost("/auth/logout", async (HttpContext context, AuthService auth, CancellationToken ct) =>
        {
            var session = await Authenticate(context, auth, ct);
            if (session is null)
            {
                return Results.Unauthorized();
            }

            await auth.LogoutAsync(session.Id, ct);
            return Results.NoContent();
        });

        // ep-06 / EP-008
        app.MapGet("/me", async (HttpContext context, AuthService auth, CancellationToken ct) =>
        {
            var session = await Authenticate(context, auth, ct);
            if (session is null)
            {
                return Results.Unauthorized();
            }

            var result = await auth.GetAccountAsync(session.AccountId, ct);
            if (!result.Ok)
            {
                return Translate(result.Error!.Value, result.Detail);
            }

            var account = result.Value!;
            return Results.Ok(new MeResponse(
                account.AccountId,
                account.Email,
                account.DisplayName,
                account.CreatedAt,
                account.ConsentVersion));
        });

        // ep-07 / EP-006
        app.MapPost("/auth/password-reset", async (PasswordResetBody body, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.ResetPasswordAsync(body.ProofToken, body.NewPassword, ct);
            return result.Ok ? Results.NoContent() : Translate(result.Error!.Value, result.Detail);
        });

        return app;
    }

    private static async Task<Domain.Sessions.Session?> Authenticate(HttpContext context, AuthService auth, CancellationToken ct)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header["Bearer ".Length..].Trim();
        return token.Length == 0 ? null : await auth.AuthenticateAsync(token, ct);
    }

    private static SessionResponse ToResponse(SessionIssued session) => new(
        session.SessionId,
        session.AccessToken,
        session.AccessExpiresAt,
        session.RefreshToken,
        session.RefreshExpiresAt);

    private static bool TryParsePurpose(string? value, out ChallengePurpose purpose)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "registration":
                purpose = ChallengePurpose.Registration;
                return true;
            case "recovery":
                purpose = ChallengePurpose.Recovery;
                return true;
            default:
                purpose = default;
                return false;
        }
    }

    private static ProblemHttpResult Problem(string detail, int status = StatusCodes.Status400BadRequest) =>
        TypedResults.Problem(detail: detail, statusCode: status);

    /// <summary>Errores publicos estables y neutrales: no revelan si un correo existe.</summary>
    private static IResult Translate(AuthError error, string? detail) => error switch
    {
        AuthError.InvalidInput => Problem(detail ?? "Entrada invalida."),
        AuthError.InvalidCredentials => Problem("Credenciales invalidas.", StatusCodes.Status401Unauthorized),
        AuthError.InvalidProof => Problem("Prueba de correo invalida o ya usada.", StatusCodes.Status401Unauthorized),
        AuthError.ChallengeNotUsable => Problem("Codigo invalido, vencido o agotado.", StatusCodes.Status400BadRequest),
        AuthError.EmailTaken => Problem("No se pudo completar el registro.", StatusCodes.Status409Conflict),
        AuthError.NotFound => Results.NotFound(),
        _ => Problem("Solicitud invalida.")
    };
}
