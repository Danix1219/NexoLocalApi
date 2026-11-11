using Nexo.Discovery.Application.Providers;
using Nexo.Identity.Contracts;

namespace Nexo.Host;

public sealed class ProviderAccountsBridge(IProviderIdentityContract identity) : IProviderAccounts
{
    public async Task<IReadOnlyDictionary<Guid, ProviderAccountState>> ReadManyAsync(Guid[] ids, CancellationToken ct)
    {
        try
        {
            return (await identity.ReadManyAsync(ids, ct)).ToDictionary(p => p.Key, p => new ProviderAccountState(p.Value.Active, p.Value.EmailVerified, p.Value.HasPhone, p.Value.PhotoUrl, p.Value.Deleted));
        }
        catch (Exception e) when (e is MongoDB.Driver.MongoException or TimeoutException)
        {
            throw new Nexo.Discovery.Application.Directory.DirectoryUnavailableException();
        }
    }
    public async Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct)
    {
        var account = await identity.ReadAsync(id, ct);
        return account == null ? null : new(account.Active, account.EmailVerified, account.HasPhone, account.PhotoUrl, account.Deleted);
    }
    public Task<bool> EnableProviderAsync(Guid id, CancellationToken ct) => identity.EnableProviderAsync(id, ct);
}
