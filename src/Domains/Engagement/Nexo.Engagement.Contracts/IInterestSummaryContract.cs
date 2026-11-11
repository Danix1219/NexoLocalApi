namespace Nexo.Engagement.Contracts;

// Trusted in-process read contract, not an HTTP surface or a list of customers.
public sealed record InterestSummary(int Count, Guid[] ProviderIds, bool Interested);
public interface IInterestSummaryContract
{
    Task<IReadOnlyDictionary<Guid, InterestSummary>> ReadAsync(Guid[] requestIds, Guid? viewerProviderId, CancellationToken ct);
}
