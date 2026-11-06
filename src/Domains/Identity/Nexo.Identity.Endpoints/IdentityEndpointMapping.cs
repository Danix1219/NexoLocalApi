using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nexo.Identity.Endpoints;

public static class IdentityEndpointMapping
{
    /// <summary>
    /// Superficie HTTP de Identity en `/v1/identity/*`; el host la monta sin conocer sus rutas.
    /// Sin operaciones: las anteriores se retiraron por estar mal elegidas. Las nuevas se
    /// cuelgan de este grupo, sin tocar el host.
    /// </summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/v1/identity").WithTags("Identity");
        return app;
    }
}
