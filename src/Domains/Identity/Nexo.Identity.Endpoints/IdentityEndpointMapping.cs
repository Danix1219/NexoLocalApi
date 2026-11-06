using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nexo.Identity.Endpoints.Http;

namespace Nexo.Identity.Endpoints;

public static class IdentityEndpointMapping
{
    /// <summary>
    /// Superficie HTTP de Identity: `/v1/identity/*` mas el endpoint de token en `/connect/token`,
    /// que vive fuera del prefijo porque es la ruta estandar de OAuth 2.0.
    /// </summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/v1/identity").WithTags("Identity").MapAuthEndpoints();
        app.MapIdentityTokenEndpoint();
        return app;
    }
}
