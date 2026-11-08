using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nexo.Screens.Endpoints;

public static class ScreensEndpointMapping
{
    /// <summary>Composicion de pantallas; reexporta grupos de dominio, no define rutas de negocio.</summary>
    public static IEndpointRouteBuilder MapScreensEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/v1/screens").WithTags("Screens");
        return app;
    }
}
