namespace Nexo.Identity.Contracts;

/// <summary>Minimal cross-module surface: no email, phone, hash or foreign persistence.</summary>
public sealed record ProviderIdentityState(bool Active, bool EmailVerified, bool HasPhone, string? PhotoUrl = null, bool Deleted = false);
public interface IProviderIdentityContract
{
    Task<ProviderIdentityState?> ReadAsync(Guid accountId, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, ProviderIdentityState>> ReadManyAsync(Guid[] accountIds, CancellationToken ct);
    Task<bool> EnableProviderAsync(Guid accountId, CancellationToken ct);
}

/// <summary>Acceso deliberado y auditable a un celular; nunca se incluye en DTOs de perfil.</summary>
public interface IPhoneDisclosureContract
{
    Task<string?> GetPhoneAsync(Guid accountId, string reason, CancellationToken ct);
}
