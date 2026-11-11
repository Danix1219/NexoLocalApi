using Nexo.Common.Results;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Providers;

// Host composes the Identity contract; Discovery receives no foreign collection or secrets.
public sealed record ProviderAccountState(bool Active, bool EmailVerified, bool HasPhone, string? PhotoUrl = null, bool Deleted = false);
public interface IProviderAccounts
{
    Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct);
    async Task<IReadOnlyDictionary<Guid, ProviderAccountState>> ReadManyAsync(Guid[] ids, CancellationToken ct)
    {
        var values = new Dictionary<Guid, ProviderAccountState>();
        foreach (var id in ids.Distinct()) if (await ReadAsync(id, ct) is { } value) values[id] = value;
        return values;
    }
    Task<bool> EnableProviderAsync(Guid id, CancellationToken ct);
}
public sealed record StoredProvider(Guid AccountId, ProviderProfile Profile, long Revision, string CreationKey, string CreationHash, bool Published, double? DistanceKm = null, ProviderProfile? CreationProfile = null, long Views = 0);
public interface IProviderStore
{
    Task<StoredProvider?> OwnAsync(Guid accountId, CancellationToken ct);
    Task<IReadOnlyList<StoredProvider>> PublicAsync(Guid[] ids, ProviderPoint? from, CancellationToken ct);
    Task<bool> InsertAsync(StoredProvider profile, CancellationToken ct);
    Task RecordViewAsync(Guid providerId, CancellationToken ct);
    Task<bool> PatchAsync(StoredProvider expected, ProviderProfile profile, string section, string[] fields, CancellationToken ct);
}
public interface IProviderSafety
{
    Task<Result<bool>> ModerateAsync(string text, CancellationToken ct);
    Task<Result<ProviderImage>> ImageAsync(Guid ownerId, Guid uploadId, string purpose, CancellationToken ct);
}
