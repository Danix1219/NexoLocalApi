namespace Nexo.Identity.Contracts;

public sealed record RequestClientIdentity(bool Active, string DisplayName, string? PhotoUrl);
public interface IRequestClientContract
{
    Task<IReadOnlyDictionary<Guid, RequestClientIdentity>> ReadAsync(Guid[] ids, CancellationToken ct);
}
