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

public sealed class OrderRequestLinkBridge(Nexo.Discovery.Application.Requests.IRequestStore requests) : IOrderRequestLink
{
    public Task<OrderRequestState> StateAsync(Guid clientId, Guid requestId, CancellationToken ct) => Guard(async () =>
        await requests.OwnAsync(clientId, requestId, ct) switch
        {
            null => OrderRequestState.Missing,
            { Status: "open" } => OrderRequestState.Open,
            _ => OrderRequestState.Taken
        });
    public Task<bool> AssignAsync(Guid clientId, Guid requestId, Guid providerId, CancellationToken ct) => Guard(() => requests.AssignAsync(clientId, requestId, providerId, ct));
    public Task<bool> ReleaseAsync(Guid requestId, Guid providerId, CancellationToken ct) => Guard(() => requests.ReleaseAsync(requestId, providerId, ct));
    public Task<bool> CompleteAsync(Guid requestId, Guid providerId, DateTime now, CancellationToken ct) => Guard(() => requests.CompleteAsync(requestId, providerId, now, ct));

    private static async Task<T> Guard<T>(Func<Task<T>> call)
    {
        try { return await call(); }
        catch (Nexo.Discovery.Application.Requests.RequestUnavailableException e) { throw new OrderUnavailableException(e); }
    }
}

public sealed class ReviewDirectoryBridge(Nexo.Discovery.Application.Requests.IRequestClients clients, IProviderStore providers)
    : Nexo.Engagement.Application.Reviews.IReviewDirectory
{
    public async Task<IReadOnlyDictionary<Guid, Nexo.Engagement.Application.Reviews.ReviewAuthor>> AuthorsAsync(Guid[] ids, CancellationToken ct) =>
        ids.Length == 0 ? new Dictionary<Guid, Nexo.Engagement.Application.Reviews.ReviewAuthor>()
            : (await clients.ReadAsync(ids, ct)).Where(p => p.Value.Active)
                .ToDictionary(p => p.Key, p => new Nexo.Engagement.Application.Reviews.ReviewAuthor(p.Value.DisplayName, p.Value.PhotoUrl));

    public Task SetRatingAsync(Guid providerId, double average, int count, CancellationToken ct) => providers.SetRatingAsync(providerId, average, count, ct);
}

public sealed class ConversationDirectoryBridge(IProviderStore providers, Nexo.Discovery.Application.Requests.IRequestClients clients,
    Nexo.Discovery.Application.Media.IMediaService media) : Nexo.Engagement.Application.Conversations.IConversationDirectory
{
    public async Task<IReadOnlyDictionary<Guid, Nexo.Engagement.Application.Conversations.ChatParty>> ProvidersAsync(Guid[] ids, CancellationToken ct) =>
        (await providers.PublicAsync(ids, null, ct)).ToDictionary(p => p.Profile.ProviderId,
            p => new Nexo.Engagement.Application.Conversations.ChatParty(p.Profile.Name, p.Profile.PhotoUrl));

    public async Task<IReadOnlyDictionary<Guid, Nexo.Engagement.Application.Conversations.ChatParty>> ClientsAsync(Guid[] ids, CancellationToken ct) =>
        (await clients.ReadAsync(ids, ct)).Where(p => p.Value.Active)
            .ToDictionary(p => p.Key, p => new Nexo.Engagement.Application.Conversations.ChatParty(p.Value.DisplayName, p.Value.PhotoUrl));

    public async Task<Nexo.Engagement.Application.Conversations.ChatImage?> ImageAsync(Guid owner, Guid id, CancellationToken ct)
    {
        var state = await media.StateAsync(owner, id, ct);
        return state.IsSuccess && state.Value is { Status: "approved", Url: { } url } s ? new(url, s.ThumbUrl, s.Width, s.Height) : null;
    }
}

public sealed class ContactDirectoryBridge(IProviderStore providers, Nexo.Discovery.Application.Requests.IRequestClients clients,
    Nexo.Identity.Contracts.IPhoneDisclosureContract phones, Nexo.Discovery.Application.Saved.ISavedStore saved)
    : Nexo.Engagement.Application.Safety.IContactDirectory
{
    public async Task<Nexo.Engagement.Application.Safety.ContactTarget?> ProviderAsync(Guid providerId, CancellationToken ct) =>
        (await providers.PublicAsync([providerId], null, ct)).FirstOrDefault() is { Published: true } p
            ? new(p.AccountId, p.Profile.Name, p.Profile.Channels.Call) : null;

    public async Task<string?> ClientNameAsync(Guid accountId, CancellationToken ct) =>
        (await clients.ReadAsync([accountId], ct)).TryGetValue(accountId, out var c) && c.Active ? c.DisplayName : null;

    public Task<string?> PhoneAsync(Guid accountId, string reason, CancellationToken ct) => phones.GetPhoneAsync(accountId, reason, ct);

    public Task<Guid?> OwnerAsync(string kind, Guid id, CancellationToken ct) => saved.TargetOwnerAsync(kind, id, ct);
}
