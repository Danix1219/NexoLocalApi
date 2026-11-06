using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Discovery.Application.Feed;
using Nexo.Discovery.Application.Ports;

namespace Nexo.Discovery.Endpoints;

public static class DiscoveryEndpointMapping
{
    /// <summary>
    /// Superficie HTTP de Discovery, en `/v1/discovery/*` como fija nv-00 §3.
    /// El `/api/v1` anterior venia de la arquitectura de microservicios y dejaba este
    /// dominio con un prefijo distinto al resto; nada desplegado dependia de el.
    /// </summary>
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/discovery").WithTags("Discovery");

        // ep-01 / EP-030: inicio por zona y modo. El modo de UI no concede privilegios.
        group.MapGet("/feed", async (
            string? mode,
            string? zone,
            string? category,
            string? tab,
            string? cursor,
            int? limit,
            IFeedReader reader,
            CancellationToken ct) =>
        {
            var query = FeedQuery.Create(mode, zone, category, tab, cursor, limit);
            var page = await reader.ReadAsync(query, ct);
            return Results.Ok(page);
        });

        return app;
    }
}
