using Microsoft.AspNetCore.Http;

namespace Nexo.Common.Security;

/// <summary>Titular de la peticion; la autorizacion nunca se infiere de la pantalla.</summary>
public interface ICurrentUser
{
    string? AccountId { get; }

    bool IsAuthenticated { get; }
}

/// <summary>Adaptador HTTP del titular autenticado por el host.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? AccountId =>
        accessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? accessor.HttpContext?.User.FindFirst("account_id")?.Value;

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
