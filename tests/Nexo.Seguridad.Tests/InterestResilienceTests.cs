using Nexo.Discovery.Application.Requests;
using Nexo.Engagement.Application.Interests;
using Nexo.Host;

namespace Nexo.Seguridad.Tests;

public sealed class InterestResilienceTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; private set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(TimeSpan duration) => Now += duration;
    }

    [Fact]
    public async Task Discovery_abre_circuito_tras_tres_fallos_y_se_recupera()
    {
        var clock = new Clock(DateTimeOffset.Parse("2026-11-10T12:00:00Z"));
        var resilience = new InterestDiscoveryResilience(clock);
        var calls = 0;
        Task<int> Fail(CancellationToken _) { calls++; throw new RequestUnavailableException(); }
        for (var i = 0; i < 3; i++) await Assert.ThrowsAsync<InterestUnavailableException>(() => resilience.ExecuteAsync(Fail, default));
        await Assert.ThrowsAsync<InterestUnavailableException>(() => resilience.ExecuteAsync(Fail, default));
        Assert.Equal(3, calls); // circuito abierto: no invoca Discovery

        clock.Advance(TimeSpan.FromSeconds(31));
        Assert.Equal(42, await resilience.ExecuteAsync<int>(_ => { calls++; return Task.FromResult(42); }, default));
        Assert.Equal(4, calls);
    }

    [Fact]
    public async Task Discovery_aplica_timeout_y_respeta_cancelacion_del_cliente()
    {
        var resilience = new InterestDiscoveryResilience(TimeProvider.System);
        await Assert.ThrowsAsync<InterestUnavailableException>(() => resilience.ExecuteAsync<int>(
            async token => { await Task.Delay(Timeout.InfiniteTimeSpan, token); return 1; }, default));

        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resilience.ExecuteAsync<int>(
            _ => Task.FromResult(1), cancelled.Token));
    }

    [Fact]
    public async Task Discovery_limita_concurrencia_y_rechaza_carga_excedente()
    {
        var resilience = new InterestDiscoveryResilience(TimeProvider.System);
        var entered = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> Hold(CancellationToken _) { Interlocked.Increment(ref entered); return WaitAsync(); }
        async Task<int> WaitAsync() { await release.Task; return 1; }
        var calls = Enumerable.Range(0, 64).Select(_ => resilience.ExecuteAsync(Hold, default)).ToArray();
        Assert.Equal(64, Volatile.Read(ref entered));
        await Assert.ThrowsAsync<InterestUnavailableException>(() => resilience.ExecuteAsync(Hold, default));
        release.SetResult();
        Assert.All(await Task.WhenAll(calls), value => Assert.Equal(1, value));
        Assert.Equal(64, entered);
    }
}
