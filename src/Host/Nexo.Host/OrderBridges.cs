using Nexo.Discovery.Application.Providers;
using Nexo.Engagement.Application.Interests;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Host;

public sealed class OrderProviderAccessBridge(IProviderStore providers, IProviderAccounts accounts, Nexo.Discovery.Application.Requests.IRequestClients clients) : IOrderProviderAccess
{
    public async Task<OrderProvider?> GetAsync(Guid id, CancellationToken ct)
    {
        var profile = (await providers.PublicAsync([id], null, ct)).FirstOrDefault();
        if (profile is not { Published: true } || (await accounts.ReadAsync(profile.AccountId, ct))?.Active != true) return null;
        return new(profile.Profile.ProviderId, profile.AccountId, profile.Profile.Name, profile.Profile.Categories,
            profile.Profile.Attention, profile.Published, profile.Profile.PublicZone?.Label ?? "Zona aproximada");
    }

    public async Task<Guid?> ProviderForAccountAsync(Guid accountId, CancellationToken ct) =>
        (await providers.OwnAsync(accountId, ct)) is { Published: true } profile ? profile.Profile.ProviderId : null;

    public async Task<OrderClient?> GetClientAsync(Guid accountId, CancellationToken ct)
    {
        var rows = await clients.ReadAsync([accountId], ct);
        return rows.TryGetValue(accountId, out var client) && client.Active ? new(client.DisplayName, client.PhotoUrl) : null;
    }
}

public sealed class OrderSafetyBridge(IInterestSafety safety) : IOrderSafety
{
    public Task<Nexo.Common.Results.Result<bool>> ModerateAsync(string text, CancellationToken ct) => safety.ModerateAsync(text, ct);
}
