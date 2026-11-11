using Nexo.Common.Results;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Requests;

public sealed record RequestReceipt(string Hash, Guid RequestId);
public sealed record RequestCandidate(Guid ClientId, ServiceRequest Request, double DistanceKm);
public sealed record RequestClosure(ServiceRequest Request, bool AlreadyClosed);
public sealed record RequestClient(bool Active, string DisplayName, string? PhotoUrl);
public sealed record RequestInterest(int Count, Guid[] Providers, bool Interested);
public sealed record RequestInterestResult(bool Available, IReadOnlyDictionary<Guid, RequestInterest> Items);
public interface IRequestClients
{
    Task<IReadOnlyDictionary<Guid, RequestClient>> ReadAsync(Guid[] ids, CancellationToken ct);
}
public interface IRequestInterests
{
    Task<RequestInterestResult> ReadAsync(Guid[] requests, Guid? providerId, CancellationToken ct);
}
public interface IRequestStore
{
    Task<RequestReceipt?> ReceiptAsync(Guid owner, Guid key, CancellationToken ct);
    Task<ServiceRequest?> OwnAsync(Guid owner, Guid id, CancellationToken ct);
    Task<RequestCandidate?> VisibleAsync(Guid viewer, Guid id, ProviderPoint point, double radiusKm, CancellationToken ct);
    Task<Result<ServiceRequest>> CreateAsync(Guid owner, Guid key, string hash, ServiceRequest request, ProviderPoint exact, CancellationToken ct);
    Task<Result<ServiceRequest>> UpdateAsync(Guid owner, Guid id, ServiceRequest request, CancellationToken ct);
    Task<ServiceRequest[]> MineAsync(Guid owner, string status, Guid? after, int limit, CancellationToken ct);
    Task<IReadOnlyList<RequestCandidate>> CandidatesAsync(Guid viewer, ProviderPoint point, double? radiusKm, string[]? cats, string text, bool today, CancellationToken ct);
    Task<Result<RequestClosure>> CloseAsync(Guid owner, Guid id, string outcome, Guid? declaredProvider, DateTime now, CancellationToken ct);
}
public sealed class RequestUnavailableException : Exception;
