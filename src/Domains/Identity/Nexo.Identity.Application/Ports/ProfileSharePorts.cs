namespace Nexo.Identity.Application.Ports;

public sealed record SharedProfile(string Name, string? PhotoUrl);
public interface IProfileShareStore
{
    Task<Guid> EnableAsync(Guid owner, CancellationToken ct);
    Task<SharedProfile?> ReadAsync(Guid shareId, CancellationToken ct);
    Task RevokeAsync(Guid owner, CancellationToken ct);
}
