using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Nexo.Identity.Endpoints.Http;

/// <summary>
/// `POST /connect/token` con los grants `password` y `refresh_token`
/// (01-auth/post-connect-token-password.md y post-connect-token-refresh.md).
/// </summary>
public static class TokenEndpoint
{
    public const string ModeClaim = "mode";
    public const string HasProviderClaim = "has_provider";

    public static IEndpointRouteBuilder MapIdentityTokenEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/connect/token", async (
            HttpContext http,
            SignInService signIn,
            IAccountRepository accounts,
            CancellationToken ct) =>
        {
            var request = http.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("La peticion de OpenIddict no esta disponible.");

            if (request.IsPasswordGrantType())
            {
                var result = await signIn.AuthenticateAsync(request.Username, request.Password, ct);
                if (result.IsFailure)
                {
                    return OAuthError(result.Error!.Code, result.Error.Message);
                }

                return SignInWith(BuildIdentity(result.Value!, request.GetScopes()));
            }

            if (request.IsRefreshTokenGrantType())
            {
                var stored = await http.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var subject = stored.Principal?.GetClaim(OpenIddictConstants.Claims.Subject);

                if (!Guid.TryParse(subject, out var accountId))
                {
                    return OAuthError(NexoCodes.AuthTokenRevoked, "La sesion ya no es valida.");
                }

                // Los permisos se releen de la base: el token no es la fuente de verdad, asi que
                // un cambio de modo o la creacion del perfil se reflejan en esta renovacion.
                var account = await accounts.FindByIdAsync(accountId, ct);
                if (account is null || account.IsDeleted)
                {
                    return OAuthError(NexoCodes.AuthTokenRevoked, "La sesion ya no es valida.");
                }

                if (account.Status == AccountStatus.PendingDeletion)
                {
                    return OAuthError(NexoCodes.AccountPendingDeletion, "La cuenta esta en proceso de baja.");
                }

                return SignInWith(BuildIdentity(account, stored.Principal!.GetScopes()));
            }

            return OAuthError(NexoCodes.ValidationFailed, "El grant_type no esta permitido.");
        })
        .RequireRateLimiting(NexoRateLimits.Auth)
        .WithTags("Identity");

        return app;
    }

    private static ClaimsPrincipal BuildIdentity(Account account, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, account.Id.ToString());
        identity.SetClaim(ModeClaim, account.CurrentMode);
        identity.SetClaim(HasProviderClaim, account.HasProvider.ToString().ToLowerInvariant());

        // Sin offline_access no hay refresh token: lo decide el cliente al pedir el scope.
        identity.SetScopes(scopes);

        // `mode` y `has_provider` son una pista para la app; la autorizacion real se comprueba
        // por recurso en cada peticion.
        identity.SetDestinations(_ => [OpenIddictConstants.Destinations.AccessToken]);

        return new ClaimsPrincipal(identity);
    }

    private static IResult SignInWith(ClaimsPrincipal principal) =>
        Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

    /// <summary>
    /// Error en formato OAuth con el `code` de Nexo como descripcion estable. Un correo inexistente
    /// y una contrasena incorrecta devuelven exactamente lo mismo.
    /// </summary>
    private static IResult OAuthError(string code, string description)
    {
        var error = code switch
        {
            NexoCodes.AuthInvalidCredentials => OpenIddictConstants.Errors.InvalidGrant,
            NexoCodes.AuthAccountLocked => OpenIddictConstants.Errors.AccessDenied,
            NexoCodes.AccountPendingDeletion => OpenIddictConstants.Errors.AccessDenied,
            NexoCodes.AuthTokenRevoked => OpenIddictConstants.Errors.InvalidGrant,
            _ => OpenIddictConstants.Errors.InvalidRequest
        };

        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorUri] = "nexo:" + code
        });

        return Results.Forbid(properties, [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }
}
