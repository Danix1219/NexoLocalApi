using Nexo.Common.Errors;
using Nexo.Discovery.Application.Requests;
using Nexo.Engagement.Contracts;
using Nexo.Identity.Contracts;

namespace Nexo.Host;

public sealed class RequestClientsBridge(IRequestClientContract identity) : IRequestClients
{
    public async Task<IReadOnlyDictionary<Guid, RequestClient>> ReadAsync(Guid[] ids, CancellationToken ct) =>
        (await identity.ReadAsync(ids, ct)).ToDictionary(p => p.Key, p => new RequestClient(p.Value.Active, p.Value.DisplayName, p.Value.PhotoUrl));
}
public sealed class RequestInterestsBridge(IInterestSummaryContract engagement, TimeProvider clock) : IRequestInterests
{
    private int failures;
    private long openUntil;
    public async Task<RequestInterestResult> ReadAsync(Guid[] requests, Guid? providerId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (Volatile.Read(ref openUntil) > clock.GetUtcNow().UtcTicks) return Missing();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            var rows = await engagement.ReadAsync(requests, providerId, timeout.Token).WaitAsync(timeout.Token);
            Interlocked.Exchange(ref failures, 0); Interlocked.Exchange(ref openUntil, 0);
            return new(true, rows.ToDictionary(p => p.Key, p => new RequestInterest(p.Value.Count, p.Value.ProviderIds, p.Value.Interested)));
        }
        catch (Exception error) when (!ct.IsCancellationRequested && (DependencyErrors.IsUnavailable(error) || error is OperationCanceledException))
        {
            if (Interlocked.Increment(ref failures) >= 3) Interlocked.Exchange(ref openUntil, clock.GetUtcNow().AddSeconds(30).UtcTicks);
            return Missing();
        }
    }
    private static RequestInterestResult Missing() => new(false, new Dictionary<Guid, RequestInterest>());
}
