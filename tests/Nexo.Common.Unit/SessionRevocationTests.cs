using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Common.Redis;
using StackExchange.Redis;

namespace Nexo.Common.Unit;

public sealed class SessionRevocationTests
{
    [Fact]
    public async Task Timeout_read_uses_existing_authoritative_session_fallback()
    {
        var store = Store(Task.FromException<bool>(Timeout()));
        Assert.False(await store.IsRevokedAsync("test-session"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Successful_read_preserves_revocation_result(bool revoked)
    {
        Assert.Equal(revoked, await Store(Task.FromResult(revoked)).IsRevokedAsync("test-session"));
    }

    [Fact]
    public async Task Failed_revocation_write_is_never_reported_as_success()
    {
        await Assert.ThrowsAsync<RedisTimeoutException>(() =>
            Store(Task.FromException<bool>(Timeout())).RevokeAsync("test-session", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Unrelated_errors_are_not_silently_swallowed()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Store(Task.FromException<bool>(new InvalidOperationException("test"))).IsRevokedAsync("test-session"));
    }

    private static RedisTimeoutException Timeout() => new(CommandFlags.None, "Simulated timeout", CommandStatus.Unknown);

    private static RedisSessionRevocationStore Store(Task<bool> result)
    {
        var db = DispatchProxy.Create<IDatabase, RedisProxy>();
        ((RedisProxy)(object)db).InvokeCall = _ => result;
        var connection = DispatchProxy.Create<IConnectionMultiplexer, RedisProxy>();
        ((RedisProxy)(object)connection).InvokeCall = method => method.Name == "GetDatabase"
            ? db : throw new InvalidOperationException(method.Name);
        return new(connection, NullLogger<RedisSessionRevocationStore>.Instance);
    }

    public class RedisProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> InvokeCall { get; set; } = _ => throw new InvalidOperationException();
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => InvokeCall(targetMethod!);
    }
}
