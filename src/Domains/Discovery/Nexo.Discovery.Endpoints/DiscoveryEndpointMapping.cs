using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nexo.Discovery.Endpoints;

public static class DiscoveryEndpointMapping
{
    /// <summary>
    /// Superficie HTTP de Discovery en `/v1/discovery/*` como fija nv-00.
    /// Sin operaciones: el feed anterior se retiro por estar mal elegido.
    /// </summary>
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/v1/discovery").WithTags("Discovery");
        return app;
    }
}
