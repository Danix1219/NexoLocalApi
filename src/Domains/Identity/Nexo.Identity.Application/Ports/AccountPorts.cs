using Nexo.Common.Results;

namespace Nexo.Identity.Application.Ports;

// Proyeccion interna sin secretos. Revision y AuditHash no pertenecen al DTO HTTP.
public sealed record AccountView(
    Guid Id, string Email, DateTime EmailVerifiedAt, string Name, string? PhotoUrl,
    string CurrentMode, bool HasProvider, string? Phone, string Status,
    DateTime? DeletionRequestedAt, DateTime CreatedAt, long Revision, long AuthEpoch, string? AuditHash);

public sealed record AccountWrite(
    string EventType, string? Name = null, bool SetPhoto = false, string? PhotoUrl = null,
    string? Mode = null, string? Phone = null, string? Status = null,
    DateTime? DeletionRequestedAt = null, string? DeletionReason = null);

public interface IAccountManagementStore
{
    Task<AccountView?> ReadAsync(Guid accountId, CancellationToken ct);
    Task<bool> ApplyAsync(AccountView expected, AccountWrite change, DateTime now, CancellationToken ct);
}

public interface IAccountProfileSafety
{
    Task<Result<bool>> CheckNameAsync(string name, CancellationToken ct);
    Task<Result<string>> ResolvePhotoAsync(Guid accountId, Guid uploadId, CancellationToken ct);
}

public sealed record DeviceRegistration(Guid DeviceId, DateTime LastSeenAt, bool Created, bool Reassigned);
