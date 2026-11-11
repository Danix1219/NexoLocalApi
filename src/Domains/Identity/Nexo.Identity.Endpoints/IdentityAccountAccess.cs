using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Common.Errors;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;
using Nexo.Identity.Endpoints.Http;

namespace Nexo.Identity.Endpoints;

public static class IdentityAccountAccess
{
    /// <summary>Estado actual por peticion: un JWT anterior no evita la baja en otra replica.</summary>
    public static IApplicationBuilder UseIdentityAccountAccess(this IApplicationBuilder app) => app.Use(async (http, next) =>
    {
        if (http.User.Identity?.IsAuthenticated != true ||
            (!http.Request.Path.StartsWithSegments("/v1") && http.Request.Path != "/connect/token"))
        {
            await next();
            return;
        }
        if (!Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id))
        {
            await NexoProblem.From(NexoCodes.AuthTokenRevoked, "Sesion invalida.", http).ExecuteAsync(http);
            return;
        }
        var store = http.RequestServices.GetRequiredService<IAccountManagementStore>();
        var account = await store.ReadAsync(id, http.RequestAborted);
        _ = long.TryParse(http.User.FindFirst(TokenEndpoint.AuthEpochClaim)?.Value, out var epoch);
        if (account is null || account.Status == AccountStatus.Deleted || epoch != account.AuthEpoch)
        {
            await NexoProblem.From(NexoCodes.AuthTokenRevoked, "Inicia sesion de nuevo.", http).ExecuteAsync(http);
            return;
        }
        var restricted = account.Status == AccountStatus.PendingDeletion || http.User.FindFirst(TokenEndpoint.RecoveryClaim)?.Value == "true";
        var allowed = (HttpMethods.IsGet(http.Request.Method) && http.Request.Path == "/v1/identity/me") ||
                      (HttpMethods.IsDelete(http.Request.Method) && http.Request.Path == "/v1/identity/me/deletion") ||
                      (HttpMethods.IsPost(http.Request.Method) && http.Request.Path == "/v1/identity/logout") ||
                      (HttpMethods.IsPost(http.Request.Method) && http.Request.Path == "/connect/token") ||
                      (account.Status == AccountStatus.PendingDeletion && HttpMethods.IsPost(http.Request.Method) && http.Request.Path == "/v1/identity/me/deletion");
        if (restricted && !allowed)
        {
            await NexoProblem.From(NexoCodes.AccountPendingDeletion, "Solo puedes consultar tu cuenta o cancelar la baja.", http).ExecuteAsync(http);
            return;
        }
        await next();
    });
}
