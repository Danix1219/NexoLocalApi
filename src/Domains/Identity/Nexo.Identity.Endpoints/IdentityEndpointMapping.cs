using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Identity.Endpoints.Http;

namespace Nexo.Identity.Endpoints;

public static class IdentityEndpointMapping
{
    /// <summary>Superficie HTTP de Identity; el host la monta sin conocer sus rutas internas.</summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/identity").WithTags("Identity");
        group.MapAuthEndpoints();
        return app;
    }
}
