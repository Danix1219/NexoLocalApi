using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Discovery.Application.Feed;
using Nexo.Discovery.Application.Ports;

namespace Nexo.Discovery.Endpoints;

public static class DiscoveryEndpointMapping
{
    /// <summary>
    /// Superficie HTTP de Discovery. El feed ya contratado conserva `/api/v1`;
    /// las rutas nuevas usan `/v1/discovery/*` (nv-00).
    /// </summary>
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Discovery");

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
