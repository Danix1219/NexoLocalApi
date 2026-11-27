using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

namespace Nexo.Engagement.Endpoints;

/// <summary>Redeems a short-lived ticket before Azure issues its connection token.</summary>
public sealed class ChatNegotiationMiddleware(RequestDelegate next)
{
    public const string AccountClaim = "nexo_chat_account";

    public async Task InvokeAsync(HttpContext context, IConnectionMultiplexer redis)
    {
        if (context.Request.Path != ChatHub.Path + "/negotiate")
        {
            await next(context);
            return;
        }

        context.Response.Headers.CacheControl = "private, no-store";
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        var ticket = context.Request.Query["ticket"].ToString();
        var value = string.IsNullOrEmpty(ticket) || ticket.Length > 64
            ? RedisValue.Null
            : await redis.GetDatabase().StringGetDeleteAsync(ChatHub.TicketKey(ticket));
        if (!Guid.TryParse(value.ToString(), out var account) || account == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Ignore caller-supplied identities: only GETDEL establishes this claim.
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AccountClaim, account.ToString("D"))], "ChatTicket"));
        await next(context);
    }
}
