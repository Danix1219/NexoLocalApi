using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nexo.Engagement.Endpoints;
using StackExchange.Redis;

namespace Nexo.Seguridad.Tests;

public class ChatNegotiationTests
{
    [Fact]
    public async Task Ticket_is_consumed_once_and_caller_identity_cannot_override_owner()
    {
        var owner = Guid.NewGuid();
        var reads = 0;
        var redis = Redis(() => ++reads == 1 ? owner.ToString() : RedisValue.Null);
        var calls = 0;
        var middleware = new ChatNegotiationMiddleware(context =>
        {
            calls++;
            Assert.Equal(owner.ToString(), context.User.FindFirst(ChatNegotiationMiddleware.AccountClaim)?.Value);
            return Task.CompletedTask;
        });
        var first = Context();
        first.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ChatNegotiationMiddleware.AccountClaim, Guid.NewGuid().ToString())], "untrusted"));
        await middleware.InvokeAsync(first, redis);
        var second = Context();
        await middleware.InvokeAsync(second, redis);
        Assert.Equal(1, calls);
        Assert.Equal(401, second.Response.StatusCode);
        Assert.Equal("private, no-store", first.Response.Headers.CacheControl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("expired")]
    [InlineData("malformed")]
    public async Task Invalid_ticket_never_reaches_negotiation(string ticket)
    {
        var context = Context(ticket);
        await new ChatNegotiationMiddleware(_ => throw new Exception("Unauthorized negotiation"))
            .InvokeAsync(context, Redis(() => ticket == "malformed" ? "not-an-account" : RedisValue.Null));
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task Unrelated_requests_do_not_consume_tickets()
    {
        var context = Context();
        context.Request.Path = "/health/live";
        var called = false;
        await new ChatNegotiationMiddleware(_ => { called = true; return Task.CompletedTask; })
            .InvokeAsync(context, Redis(() => throw new Exception("Unexpected Redis access")));
        Assert.True(called);
    }

    [Fact]
    public async Task Non_post_and_oversized_tickets_never_access_redis()
    {
        var redis = Redis(() => throw new Exception("Unexpected Redis access"));
        var middleware = new ChatNegotiationMiddleware(_ => throw new Exception("Unexpected negotiation"));
        var get = Context();
        get.Request.Method = "GET";
        await middleware.InvokeAsync(get, redis);
        Assert.Equal(405, get.Response.StatusCode);
        var oversized = Context(new string('x', 65));
        await middleware.InvokeAsync(oversized, redis);
        Assert.Equal(401, oversized.Response.StatusCode);
    }

    private static DefaultHttpContext Context(string ticket = "ws_test")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/hubs/chat/negotiate";
        context.Request.QueryString = new QueryString("?ticket=" + ticket);
        return context;
    }

    private static IConnectionMultiplexer Redis(Func<RedisValue> read)
    {
        var db = DispatchProxy.Create<IDatabase, Proxy>();
        ((Proxy)(object)db).Call = method => method.Name == "StringGetDeleteAsync"
            ? Task.FromResult(read()) : throw new InvalidOperationException(method.Name);
        var redis = DispatchProxy.Create<IConnectionMultiplexer, Proxy>();
        ((Proxy)(object)redis).Call = _ => db;
        return redis;
    }

    public class Proxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Call { get; set; } = _ => throw new InvalidOperationException();
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!);
    }
}
