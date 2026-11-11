namespace Nexo.Discovery.Contracts;

public interface IMediaContract
{
    Task<string?> AvatarAsync(Guid owner, Guid upload, CancellationToken ct);
}
