using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Common.RateLimiting;
using Nexo.Discovery.Application.Media;
namespace Nexo.Discovery.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/uploads", async (HttpContext h, UploadDraft draft, IMediaService service, CancellationToken ct) =>
            await Handle(h, async () => (await service.CreateAsync(User(h), draft, ct)).Match<IResult>(v => Results.Json(v, statusCode: 201), e => Error(e, h))))
            .RequireAuthorization().RequireRateLimiting(NexoRateLimits.User);
        group.MapPost("/uploads/{id:guid}/complete", async (HttpContext h, Guid id, IMediaService service, CancellationToken ct) =>
            await Handle(h, async () => (await service.CompleteAsync(User(h), id, ct)).Match<IResult>(v => Results.Json(new { v.UploadId, v.Status, pollUrl = $"/v1/discovery/uploads/{id}", estimatedSeconds = 4 }, statusCode: v.Status == "approved" ? 200 : 202), e => Error(e, h))))
            .RequireAuthorization().RequireRateLimiting(NexoRateLimits.User);
        group.MapGet("/uploads/{id:guid}", async (HttpContext h, Guid id, IMediaService service, CancellationToken ct) =>
            await Handle(h, async () => (await service.StateAsync(User(h), id, ct)).Match<IResult>(v => Results.Ok(v), e => Error(e, h))))
            .RequireAuthorization().RequireRateLimiting(NexoRateLimits.User);
        group.MapDelete("/uploads/{id:guid}", async (HttpContext h, Guid id, IMediaService service, CancellationToken ct) =>
            await Handle(h, async () => (await service.DeleteAsync(User(h), id, ct)).Match<IResult>(_ => Results.NoContent(), e => Error(e, h))))
            .RequireAuthorization().RequireRateLimiting(NexoRateLimits.User);
        foreach (var path in new[] { "/media/{id:guid}", "/media/{id:guid}/thumb" })
            group.MapGet(path, async (HttpContext h, Guid id, IMediaService service, CancellationToken ct) => await Handle(h, async () =>
            {
                var file = await service.FileAsync(id, h.Request.Path.Value!.EndsWith("/thumb", StringComparison.Ordinal), ct);
                h.Response.Headers.XContentTypeOptions = "nosniff";
                if (file != null) h.Response.Headers.CacheControl = "private, max-age=300";
                return file == null ? Results.NotFound() : Results.Bytes(file.Bytes, file.ContentType);
            })).AllowAnonymous().RequireRateLimiting(NexoRateLimits.Public);
        group.MapGet("/me/cover", async (HttpContext h, IMediaService service, CancellationToken ct) => await Handle(h, async () => Results.Ok(new { url = await service.CoverAsync(User(h), ct) })))
            .RequireAuthorization().RequireRateLimiting(NexoRateLimits.User);
        group.MapPut("/me/cover", async (HttpContext h, CoverBody body, IMediaService service, CancellationToken ct) => await Handle(h, async () =>
            (await service.SetCoverAsync(User(h), body.UploadId, ct)).Match<IResult>(url => Results.Ok(new { url }), e => Error(e, h))))
            .RequireAuthorization().RequireRateLimiting(NexoRateLimits.User);
    }
    public sealed record CoverBody(Guid UploadId);
    private static Guid User(HttpContext h) => Guid.Parse(h.User.FindFirst("sub")!.Value);
    private static IResult Error(NexoError e, HttpContext h) => e.Code switch
    {
        "upload.content_type_invalid" or "upload.dimensions_too_large" or "upload.blob_missing" => Results.Problem(statusCode: 400, extensions: new Dictionary<string, object?> { ["code"] = e.Code }),
        "upload.too_large" => Results.Problem(statusCode: 413, extensions: new Dictionary<string, object?> { ["code"] = e.Code }),
        "upload.expired" => Results.Problem(statusCode: 410, extensions: new Dictionary<string, object?> { ["code"] = e.Code }),
        "upload.rejected_by_moderation" or "provider.profile_required" => Results.Problem(statusCode: 403, extensions: new Dictionary<string, object?> { ["code"] = e.Code }),
        _ => NexoProblem.From(e, h)
    };
    private static async Task<IResult> Handle(HttpContext h, Func<Task<IResult>> action)
    {
        h.Response.Headers.CacheControl = "no-store";
        try { return await action(); }
        catch (Exception e) when (DependencyErrors.IsUnavailable(e) || e is InvalidOperationException or ArgumentException or FormatException)
        { return NexoProblem.From(NexoCodes.ServerDependencyUnavailable, null, h); }
    }
}
