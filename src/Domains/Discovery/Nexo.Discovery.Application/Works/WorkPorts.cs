using Nexo.Common.Results;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Works;

public sealed record WorkReceipt(string Hash, Guid WorkId);
public sealed record WorkPage(Work[] Items, HashSet<Guid> Saved, HashSet<Guid> Recommended, bool HasMore);
public sealed record WorkRecommendation(Guid WorkId, bool Recommended, int Recs)
{
    public string RecsText => Recs == 1 ? "1 recomendación" : $"{Recs} recomendaciones";
}
public interface IWorkStore
{
    Task<WorkReceipt?> ReceiptAsync(Guid owner, Guid key, CancellationToken ct);
    Task<Work?> FindAsync(Guid id, CancellationToken ct);
    Task<Result<Work>> CreateAsync(Guid owner, Guid key, string hash, Work work, CancellationToken ct);
    Task<Result<Work>> UpdateAsync(Guid owner, Guid id, Work work, CancellationToken ct);
    Task<WorkPage> ListAsync(Guid provider, Guid? after, int limit, Guid? viewer, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid owner, Guid id, CancellationToken ct);
    Task<Result<WorkRecommendation>> RecommendAsync(Guid viewer, Guid id, bool desired, CancellationToken ct);
}
public sealed class WorkUnavailableException : Exception;
