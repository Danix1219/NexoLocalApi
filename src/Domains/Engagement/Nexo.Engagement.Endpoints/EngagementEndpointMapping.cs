using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nexo.Engagement.Endpoints;

public static class EngagementEndpointMapping
{
    /// <summary>Superficie HTTP de Engagement; el host la monta sin conocer sus rutas internas.</summary>
    public static IEndpointRouteBuilder MapEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/v1/engagement").WithTags("Engagement");
        return app;
    }
}
