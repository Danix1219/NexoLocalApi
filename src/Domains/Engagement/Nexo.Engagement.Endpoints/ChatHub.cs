using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nexo.Common.RateLimiting;
using Nexo.Engagement.Application.Conversations;
using StackExchange.Redis;

namespace Nexo.Engagement.Endpoints;

/// <summary>Server→client only. Sending stays on POST /messages so moderation, idempotency and rate limit have one path.
/// Each connection joins `user:{accountId}`; the server picks recipients, so no client-side join needs authorising.</summary>
public sealed class ChatHub(IConnectionMultiplexer redis, IConfiguration configuration) : Hub
{
    public const string Path = "/hubs/chat";
    internal static string TicketKey(string ticket) => "nexo:ws:" + ticket;
    internal static string UserGroup(Guid account) => "user:" + account.ToString("N");

    public override async Task OnConnectedAsync()
    {
        if (!string.IsNullOrWhiteSpace(configuration["Azure:SignalR:ConnectionString"]))
        {
            if (!Guid.TryParse(Context.User?.FindFirst(ChatNegotiationMiddleware.AccountClaim)?.Value, out var identity))
            {
                Context.Abort();
                return;
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(identity));
            await base.OnConnectedAsync();
            return;
        }
        var ticket = Context.GetHttpContext()?.Request.Query["ticket"].ToString();
        // Single use: GETDEL consumes it, so a ticket leaked in a proxy log is already dead.
        var value = string.IsNullOrEmpty(ticket) || ticket.Length > 64 ? RedisValue.Null : await redis.GetDatabase().StringGetDeleteAsync(TicketKey(ticket));
        if (value.IsNullOrEmpty || !Guid.TryParse(value.ToString(), out var account))
        {
            Context.Abort();
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(account));
        await base.OnConnectedAsync();
    }

    /// <summary>`POST /v1/engagement/hubs/chat/ticket`: a 60-second single-use ticket instead of the JWT in the WebSocket URL.</summary>
    public static void MapTicket(RouteGroupBuilder group) =>
        group.MapPost("/hubs/chat/ticket", async (HttpContext http, IConnectionMultiplexer redis, IConfiguration configuration) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            if (!Guid.TryParse(http.User.FindFirst("sub")?.Value, out var account)) return Results.Unauthorized();
            var ticket = "ws_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            await redis.GetDatabase().StringSetAsync(TicketKey(ticket), account.ToString("D"), TimeSpan.FromSeconds(60));
            return Results.Ok(new { ticket, expiresInSeconds = 60, url = Path, negotiate = !string.IsNullOrWhiteSpace(configuration["Azure:SignalR:ConnectionString"]) });
        }).RequireAuthorization().RequireRateLimiting(NexoRateLimits.User).WithSummary("Ticket de un solo uso para conectar al chat en vivo");
}

public sealed class SignalRChatRealtime(IHubContext<ChatHub> hub, ILogger<SignalRChatRealtime> log) : IChatRealtime
{
    public async Task SendAsync(Guid account, string method, object payload, CancellationToken ct)
    {
        // The message is already stored: a broadcast failure must never fail the request.
        try { await hub.Clients.Group(ChatHub.UserGroup(account)).SendAsync(method, payload, ct); }
        catch (Exception e) when (e is not OperationCanceledException) { log.LogWarning(e, "Aviso en vivo {Method} no entregado.", method); }
    }
}
