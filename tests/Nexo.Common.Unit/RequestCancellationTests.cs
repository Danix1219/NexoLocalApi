using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Common.Errors;

namespace Nexo.Common.Unit;

public sealed class RequestCancellationTests
{
    [Fact]
    public async Task Already_aborted_request_does_not_query_dependencies()
    {
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(true) };
        var called = false;
        await Middleware(_ => { called = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.False(called);
        Assert.Equal(499, context.Response.StatusCode);
    }

    [Fact]
    public async Task Client_cancellation_during_download_is_not_a_server_or_session_error()
    {
        using var abort = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = abort.Token };
        context.Response.Body = new MemoryStream();
        await Middleware(_ => { abort.Cancel(); throw new TaskCanceledException(); }).InvokeAsync(context);
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dependency_cancellation_without_client_abort_returns_503(bool taskCancelled)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        context.Response.ContentLength = 999;
        context.Response.ContentType = "image/webp";
        await Middleware(_ => throw (taskCancelled ? new TaskCanceledException() : new OperationCanceledException())).InvokeAsync(context);
        Assert.Equal(503, context.Response.StatusCode);
        Assert.NotEqual(999L, context.Response.ContentLength);
        Assert.Contains("application/problem+json", context.Response.ContentType);
        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("server.dependency_unavailable", body);
        Assert.DoesNotContain("auth.token_revoked", body);
    }

    [Fact]
    public async Task Unrelated_failures_are_not_suppressed()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Middleware(_ => throw new InvalidOperationException()).InvokeAsync(new DefaultHttpContext()));
    }

    private static RequestCancellationMiddleware Middleware(RequestDelegate next) =>
        new(next, NullLogger<RequestCancellationMiddleware>.Instance);
}
