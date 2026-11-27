using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Engagement.Application.Conversations;

namespace Nexo.Engagement.Endpoints;

public static class ChatKeyEndpoints
{
    public static void MapChatKeyEndpoints(this RouteGroupBuilder parent)
    {
        var group = parent.MapGroup("/chat-keys").RequireAuthorization().RequireRateLimiting(NexoRateLimits.Chat);
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "private, no-store";
            // Bootstrap surface stays off until the complete client and ciphertext transport are validated.
            if (!context.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("Chat:E2eeEnabled"))
                return Results.NotFound();
            try { return await next(context); }
            catch (Exception e) when (DependencyErrors.IsUnavailable(e))
            { return NexoProblem.From(NexoCodes.ServerDependencyUnavailable, null, context.HttpContext); }
        });
        group.MapPut("/conversations/{conversationId:guid}/attachments/{attachmentId:guid}", async
            (Guid conversationId, Guid attachmentId, IConversationStore conversations, IPrivateChatAttachments attachments,
                IChatAttachmentOwners owners, TimeProvider clock, HttpContext http, CancellationToken ct) =>
        {
            var conversation = await conversations.FindAsync(conversationId, Me(http.User), ct);
            if (conversation is null) return Results.NotFound();
            if (!conversation.Encrypted || conversation.BlockedBy.Length > 0) return Results.StatusCode(403);
            if (http.Request.ContentType != "application/octet-stream" || http.Request.ContentLength is not (> 16 and <= IPrivateChatAttachments.MaxBytes))
                return Results.BadRequest();
            using var buffer = new MemoryStream();
            var chunk = new byte[65536];
            int read;
            while ((read = await http.Request.Body.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > IPrivateChatAttachments.MaxBytes) return Results.StatusCode(413);
                await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
            }
            if (buffer.Length != http.Request.ContentLength) return Results.BadRequest();
            // El autor se anota antes de subir: ningún objeto queda sin poder borrarse con su cuenta.
            if (!await owners.ClaimAsync(new(conversationId, attachmentId), Me(http.User), clock.GetUtcNow().UtcDateTime, ct))
                return Results.Conflict();
            await attachments.PutAsync(conversationId, attachmentId, buffer.ToArray(), ct);
            return Results.NoContent();
        });
        group.MapGet("/conversations/{conversationId:guid}/attachments/{attachmentId:guid}", async
            (Guid conversationId, Guid attachmentId, IConversationStore conversations, IPrivateChatAttachments attachments, HttpContext http, CancellationToken ct) =>
        {
            var conversation = await conversations.FindAsync(conversationId, Me(http.User), ct);
            if (conversation is null) return Results.NotFound();
            if (!conversation.Encrypted || conversation.BlockedBy.Length > 0) return Results.StatusCode(403);
            var bytes = await attachments.GetAsync(conversationId, attachmentId, ct);
            return bytes is null ? Results.NotFound() : Results.Bytes(bytes, "application/octet-stream");
        });
        static Guid Me(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);
        group.MapGet("/devices", async (ChatKeyService service, HttpContext http, CancellationToken ct) =>
            Results.Ok(await service.OwnDevicesAsync(Me(http.User), ct)));
        group.MapPost("/devices/{deviceId:int}/claim", async (int deviceId, ChatKeyService service, HttpContext http, CancellationToken ct) =>
        {
            var bundle = await service.ClaimOwnAsync(Me(http.User), deviceId, ct);
            return bundle is null ? Results.NotFound() : Results.Ok(bundle);
        });
        group.MapPut("/devices", async (ChatDeviceKeys body, ChatKeyService service, HttpContext http, CancellationToken ct) =>
            (await service.RegisterAsync(Me(http.User), body, ct)).Match<IResult>(_ => Results.NoContent(), e => NexoProblem.From(e, http)));
        group.MapPost("/conversations/{conversationId:guid}/enable", async
            (Guid conversationId, EncryptedChatService service, HttpContext http, CancellationToken ct) =>
            (await service.EnableAsync(Me(http.User), conversationId, ct)).Match<IResult>(Results.Ok, e => NexoProblem.From(e, http)));
        group.MapPost("/conversations/{conversationId:guid}/messages", async
            (Guid conversationId, EncryptedChatBody body, EncryptedChatService service, HttpContext http, CancellationToken ct) =>
            (await service.SendAsync(Me(http.User), conversationId, http.Request.Headers["Idempotency-Key"].FirstOrDefault(), body, ct))
                .Match<IResult>(v => Results.Json(v, statusCode: 201), e => NexoProblem.From(e, http)));
        group.MapDelete("/devices/{deviceId:int}", async (int deviceId, ChatKeyService service, HttpContext http, CancellationToken ct) =>
        {
            if (deviceId is < 1 or > 5) return Results.BadRequest();
            await service.RevokeAsync(Me(http.User), deviceId, ct);
            return Results.NoContent();
        });
        group.MapPatch("/conversations/{conversationId:guid}/messages/{messageId:guid}", async
            (Guid conversationId, Guid messageId, EncryptedChatBody body, EncryptedChatService service, HttpContext http, CancellationToken ct) =>
            (await service.MutateAsync(Me(http.User), conversationId, messageId, http.Request.Headers["Idempotency-Key"].FirstOrDefault(), body, false, ct))
                .Match<IResult>(Results.Ok, e => NexoProblem.From(e, http)));
        group.MapDelete("/conversations/{conversationId:guid}/messages/{messageId:guid}", async
            (Guid conversationId, Guid messageId, EncryptedChatService service, HttpContext http, CancellationToken ct) =>
            (await service.MutateAsync(Me(http.User), conversationId, messageId, http.Request.Headers["Idempotency-Key"].FirstOrDefault(), null, true, ct))
                .Match<IResult>(Results.Ok, e => NexoProblem.From(e, http)));
        group.MapGet("/conversations/{conversationId:guid}/devices", async (Guid conversationId, ChatKeyService service, HttpContext http, CancellationToken ct) =>
            (await service.DevicesAsync(Me(http.User), conversationId, ct)).Match<IResult>(Results.Ok, e => NexoProblem.From(e, http)));
        group.MapPost("/conversations/{conversationId:guid}/devices/{deviceId:int}/claim", async
            (Guid conversationId, int deviceId, ChatKeyService service, HttpContext http, CancellationToken ct) =>
            (await service.ClaimAsync(Me(http.User), conversationId, deviceId, ct)).Match<IResult>(Results.Ok, e => NexoProblem.From(e, http)));
    }
}
