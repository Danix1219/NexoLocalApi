using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Requests;
using Nexo.Discovery.Domain;
using Nexo.Engagement.Application.Interests;
using MongoDB.Driver;
using System.Net.Http;

namespace Nexo.Host;

public sealed class InterestDiscoveryResilience(TimeProvider clock)
{
    private readonly object sync = new();
    private readonly SemaphoreSlim bulkhead = new(64, 64);
    private int failures;
    private DateTimeOffset openUntil;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (sync)
            if (openUntil > clock.GetUtcNow()) throw new InterestUnavailableException();
        if (!bulkhead.Wait(0)) throw new InterestUnavailableException();

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            try
            {
                var value = await operation(timeout.Token).WaitAsync(timeout.Token);
                ct.ThrowIfCancellationRequested();
                lock (sync) { failures = 0; openUntil = default; }
                return value;
            }
            catch (OperationCanceledException error) when (!ct.IsCancellationRequested)
            {
                Failed(); throw new InterestUnavailableException(error);
            }
            catch (Exception error) when (error is RequestUnavailableException or MongoException or TimeoutException or HttpRequestException or Nexo.Discovery.Application.Directory.DirectoryUnavailableException)
            {
                Failed(); throw new InterestUnavailableException(error);
            }
        }
        finally { bulkhead.Release(); }
    }

    private void Failed()
    {
        lock (sync)
            if (++failures >= 3) openUntil = clock.GetUtcNow().AddSeconds(30);
    }
}

public sealed class InterestRequestAccessBridge(IRequestStore requests, IProviderStore providers, IProviderAccounts accounts, IRequestClients clients,
    InterestDiscoveryResilience resilience) : IInterestRequestAccess
{
    public async Task<InterestRequestAccess> ForProviderAsync(Guid requestId, Guid providerId, CancellationToken ct)
    {
        return await resilience.ExecuteAsync(token => ForProviderCoreAsync(requestId, providerId, token), ct);
    }

    private async Task<InterestRequestAccess> ForProviderCoreAsync(Guid requestId, Guid providerId, CancellationToken ct)
    {
        if ((await accounts.ReadAsync(providerId, ct))?.Active != true) return new(false, null, null);
        var provider = await providers.OwnAsync(providerId, ct);
        if (provider is not { Published: true } || provider.Profile.Location?.IsValid != true) return new(false, null, null);
        var ownRequest = await requests.OwnAsync(providerId, requestId, ct);
        if (ownRequest != null) return new(true, provider.Profile.ProviderId, Context(providerId, ownRequest));
        var radius = Math.Max(10, provider.Profile.RadiusKm);
        var visible = await requests.VisibleAsync(providerId, requestId, provider.Profile.Location, radius, ct);
        if (visible == null || visible.DistanceKm > provider.Profile.RadiusKm && !(provider.Profile.Attention is "shop" or "both" && visible.DistanceKm <= 10)) return new(true, provider.Profile.ProviderId, null);
        var clientRows = await clients.ReadAsync([visible.ClientId], ct);
        if (!clientRows.TryGetValue(visible.ClientId, out var client) || !client.Active) return new(true, provider.Profile.ProviderId, null);
        return new(true, provider.Profile.ProviderId, Context(visible.ClientId, visible.Request));
    }

    public async Task<InterestRequestContext?> OwnedAsync(Guid requestId, Guid clientId, CancellationToken ct)
    {
        return await resilience.ExecuteAsync(async token =>
        {
            var request = await requests.OwnAsync(clientId, requestId, token);
            return request == null ? null : Context(clientId, request);
        }, ct);
    }

    private static InterestRequestContext Context(Guid clientId, ServiceRequest request) => new(request.RequestId, clientId, request.Title,
        request.Status, request.Contact, new(request.PublicPoint.Lat, request.PublicPoint.Lng), request.ZoneLabel, request.CreatedAt);
}

public sealed class InterestDirectoryBridge(IProviderStore providers, IProviderAccounts accounts, TimeProvider clock,
    InterestDiscoveryResilience resilience) : IInterestDirectory
{
    public async Task<IReadOnlyDictionary<Guid, InterestProviderCard>> ReadAsync(Guid[] providerIds, InterestPoint from, CancellationToken ct)
    {
        if (providerIds.Length > 1000) throw new InterestUnavailableException();
        return await resilience.ExecuteAsync(async token =>
        {
            var profiles = await providers.PublicAsync(providerIds.Distinct().ToArray(), new(from.Lat, from.Lng), token);
            var states = await accounts.ReadManyAsync(profiles.Select(p => p.AccountId).ToArray(), token);
            var now = clock.GetUtcNow().UtcDateTime;
            return (IReadOnlyDictionary<Guid, InterestProviderCard>)profiles.ToDictionary(p => p.Profile.ProviderId, p =>
            {
                var schedule = p.Profile.Schedule;
                var isOpen = schedule?.IsOpen(now) == true;
                var distance = p.DistanceKm ?? 0;
                var minPrice = p.Profile.Prices.Where(x => x.FromCents is not null).Select(x => x.FromCents!.Value).DefaultIfEmpty().Min();
                return new InterestProviderCard(p.Profile.ProviderId, p.Profile.Name, p.Profile.Kind == "business" ? "business" : "trade", p.Profile.PhotoUrl,
                    p.Profile.Categories, p.Profile.PublicZone?.Label ?? "Zona aproximada", p.Profile.Attention switch { "home" => "Va a domicilio", "shop" => "En su local", "both" => "Domicilio y local", _ => "Según perfil" },
                    RequestRules.DistanceText(distance), isOpen, schedule?.IsValid == true ? isOpen ? "Abierto ahora" : "Cerrado" : "Horario sin publicar",
                    p.Profile.RatingAverage, p.Profile.RatingCount, p.Profile.Recs, minPrice == 0 ? null : minPrice,
                    p.Profile.Visit.Mode, states.TryGetValue(p.AccountId, out var account) && account.EmailVerified);
            });
        }, ct);
    }
}

public sealed class InterestSafetyBridge(IProviderSafety safety) : IInterestSafety
{
    public Task<Nexo.Common.Results.Result<bool>> ModerateAsync(string text, CancellationToken ct) => safety.ModerateAsync(text, ct);
}
