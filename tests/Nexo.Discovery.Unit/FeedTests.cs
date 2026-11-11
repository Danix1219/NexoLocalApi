using System.Text.Json;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Works;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Unit;

public sealed class FeedTests
{
    private static readonly ProviderPoint Point = new(20.0539, -99.3403);

    [Fact]
    public void Cursor_es_opaco_y_se_vincula_a_la_ubicacion()
    {
        var id = Guid.CreateVersion7();
        var cursor = FeedRules.Cursor(id, Point);

        Assert.True(FeedRules.TryCursor(cursor, Point, out var decoded));
        Assert.Equal(id, decoded);
        Assert.False(FeedRules.TryCursor(cursor, new ProviderPoint(20.1, -99.4), out _));
        Assert.False(FeedRules.TryCursor("!invalid!", Point, out _));
    }

    [Theory]
    [InlineData(0, Nexo.Common.Errors.NexoCodes.RequestLimitExceeded)]
    [InlineData(51, Nexo.Common.Errors.NexoCodes.RequestLimitExceeded)]
    public void Limite_fuera_de_rango_se_rechaza(int limit, string expected)
    {
        Assert.False(FeedRules.Validate(new(Point.Lat, Point.Lng, Limit: limit), out var error));
        Assert.Equal(expected, error);
    }

    [Fact]
    public async Task Feed_incluye_solo_perfiles_activos_y_no_serializa_ubicacion_privada()
    {
        var owner = Guid.NewGuid();
        var providerId = Guid.CreateVersion7();
        var profile = new ProviderProfile
        {
            ProviderId = providerId,
            Name = "Plomeria local",
            PhotoUrl = "https://cdn.test/avatar.webp",
            Location = Point,
            RadiusKm = 10,
            Categories = ["plomeria"]
        };
        var visible = new StoredProvider(owner, profile, 0, "", "", true, 2.3);
        var inactiveOwner = Guid.NewGuid();
        var inactiveId = Guid.CreateVersion7();
        var inactive = new StoredProvider(inactiveOwner, profile with { ProviderId = inactiveId, Name = "Cuenta inactiva" }, 0, "", "", true, 1);
        var works = new[]
        {
            new Work(Guid.CreateVersion7(), providerId, "plomeria", "Trabajo reciente", "Texto de la publicación", null, null, null, null, Point, "Zona Centro", [], 3, DateTime.UtcNow, DateTime.UtcNow),
            new Work(Guid.CreateVersion7(), inactiveId, "plomeria", "No debe salir", "Trabajo de una cuenta inactiva", null, null, null, null, Point, "Zona Centro", [], 0, DateTime.UtcNow, DateTime.UtcNow)
        };
        var candidates = new Profiles([visible, inactive]);
        var states = new Accounts(new Dictionary<Guid, ProviderAccountState>
        {
            [owner] = new(true, true, false),
            [inactiveOwner] = new(false, false, false)
        });
        var store = new Feed([works[0], works[1]]);
        var service = new FeedService(candidates, states, store, TimeProvider.System);

        var result = await service.ReadAsync(new(Point.Lat, Point.Lng), null, default);

        Assert.True(result.IsSuccess);
        Assert.Equal([providerId], store.ProviderIds);
        Assert.Single(result.Value!.Items);
        var json = JsonSerializer.Serialize(result.Value, ProviderService.Json);
        Assert.Contains("Plomeria local", json);
        Assert.Contains("Zona Centro", json);
        Assert.DoesNotContain("location\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recommended", json, StringComparison.Ordinal);
        Assert.Contains("\"canContact\":false", json, StringComparison.Ordinal);
        Assert.True(result.Value.Guest);
    }

    private sealed class Profiles(IReadOnlyList<StoredProvider> values) : IDirectoryProfiles
    {
        public Task<IReadOnlyList<StoredProvider>> CandidatesAsync(ProviderPoint? point, string? category, CancellationToken ct) => Task.FromResult(values);
    }

    private sealed class Accounts(IReadOnlyDictionary<Guid, ProviderAccountState> values) : IProviderAccounts
    {
        public Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct) => Task.FromResult(values.GetValueOrDefault(id));
        public Task<IReadOnlyDictionary<Guid, ProviderAccountState>> ReadManyAsync(Guid[] ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<Guid, ProviderAccountState>>(values.Where(p => ids.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value));
        public Task<bool> EnableProviderAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class Feed(IReadOnlyList<Work> values) : IFeedStore
    {
        public Guid[] ProviderIds { get; private set; } = [];
        public Task<WorkPage> ReadAsync(ProviderPoint point, Guid[] providerIds, Guid? after, int limit, Guid? viewer, CancellationToken ct)
        {
            ProviderIds = providerIds;
            return Task.FromResult(new WorkPage(values.ToArray(), [], [], false));
        }
    }
}
