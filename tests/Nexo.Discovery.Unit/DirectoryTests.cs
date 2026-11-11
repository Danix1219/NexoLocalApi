using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Unit;

public sealed class DirectoryTests
{
    private sealed class Catalog : IDirectoryCatalog
    {
        public Task<CatalogResponse> ReadAsync(CancellationToken ct) => Task.FromResult(DirectoryCatalog.Current);
    }
    private sealed class Profiles(params StoredProvider[] values) : IExpandedDirectoryProfiles
    {
        public int Calls;
        public Task<IReadOnlyList<StoredProvider>> CandidatesAsync(ProviderPoint? point, string? category, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<StoredProvider>>(values.Where(p => (category == null || p.Profile.Categories.Contains(category)) &&
                (point == null || p.DistanceKm <= 20)).ToArray());
        }
        public Task<IReadOnlyList<StoredProvider>> ExpandedAsync(ProviderPoint point, string? category, CancellationToken ct) => Task.FromResult<IReadOnlyList<StoredProvider>>(values);
    }
    private sealed class Accounts : IProviderAccounts
    {
        public HashSet<Guid> Inactive = [];
        public HashSet<Guid> Unverified = [];
        public Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct) => Task.FromResult<ProviderAccountState?>(new(!Inactive.Contains(id), !Unverified.Contains(id), false));
        public Task<bool> EnableProviderAsync(Guid id, CancellationToken ct) => throw new InvalidOperationException("Las lecturas no deben escribir Identity.");
    }
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 9, 18, 0, 0, TimeSpan.Zero); // Monday, noon Mexico.
    }
    private static readonly ProviderPoint Center = new(20.05, -99.34);
    private static StoredProvider Provider(string name, double distance = 1, string attention = "home", int recs = 0) => new(Guid.NewGuid(), new()
    {
        ProviderId = Guid.CreateVersion7(), Name = name, Bio = "Reparación de fugas y calentadores", Categories = ["plomeria"], Specialties = ["fugas"],
        Attention = attention, RadiusKm = 5, Recs = recs, PublicZone = new("Zona pública", new(20.06, -99.35)), Location = attention == "home" ? null : Center,
        Schedule = new(Enumerable.Repeat<ProviderDay?>(new("08:00", "18:00"), 7).ToArray(), true),
        Visit = new("free"), Prices = [new("Reparación", 15000)], UpdatedAt = DateTime.UtcNow
    }, 0, "", "", true, distance);
    private static DirectoryService Service(Profiles p, Accounts? a = null) => new(p, a ?? new(), new Catalog(),
        [new RelevantProviderSort(), new NearestProviderSort(), new RecommendedProviderSort()], new Clock());
    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, ProviderService.Json);
    [Fact]
    public void Catalogo_compatible_con_los_slugs_existentes_y_colores_maqueta()
    {
        var cats = DirectoryCatalog.Current.Categories;
        Assert.Equal(7, cats.Length);
        Assert.Equal(30, cats.Sum(c => c.Specialties.Length));
        foreach (var cat in cats) Assert.Equal(ProviderRules.Specialties[cat.Slug], cat.Specialties.Select(s => s.Slug));
        Assert.Equal("#46A3F0", cats[0].Gradient.From);
        Assert.DoesNotContain("keywords", Json(cats).GetRawText());
    }
    [Theory]
    [InlineData("reparacion calentador", true)]
    [InlineData("REPARACIÓN FUGAS", true)]
    [InlineData("plomeria fugas", true)]
    [InlineData("fugas inexistente", false)]
    [InlineData(".*", false)]
    public void Busqueda_AND_acentos_etiquetas_y_texto_literal(string query, bool expected) =>
        Assert.Equal(expected, DirectoryService.Matches(Provider("Nombre").Profile, query, DirectoryCatalog.Current));
    [Fact]
    public async Task Filtros_horario_real_atencion_radio_y_expansion_solo_sin_coincidencias()
    {
        var home = Provider("Dentro", 1);
        var outside = Provider("Fuera", 30);
        var shop = Provider("Local", 8, "shop");
        var service = Service(new(home, outside, shop));
        var query = new DirectoryQuery(Lat: Center.Lat, Lng: Center.Lng, Open: true, Free: true, Prices: true, Urgent: true, Attention: "home", Expanded: true);
        var first = (await service.SearchAsync(query, default)).Value!;
        Assert.Single(first.Items); Assert.False(first.OutOfZone);
        Assert.Equal("Dentro", Json(first.Items[0]).GetProperty("name").GetString());
        var expanded = (await service.SearchAsync(query with { Q = "Fuera" }, default)).Value!;
        Assert.True(expanded.OutOfZone); Assert.NotNull(expanded.Notice); Assert.Single(expanded.Items);
        Assert.Empty((await service.SearchAsync(query with { Q = "Fuera", Expanded = false }, default)).Value!.Items);
        Assert.Equal(2, (await service.SearchAsync(query with { Attention = "all" }, default)).Value!.Total);
    }
    [Fact]
    public async Task Cursor_compacto_desempata_sin_repetir_y_rechaza_filtros_distintos()
    {
        var values = new[] { Provider("Uno"), Provider("Dos"), Provider("Tres") };
        var service = Service(new(values));
        var query = new DirectoryQuery(Limit: 1);
        var seen = new HashSet<Guid>();
        DirectoryResponse page;
        do
        {
            page = (await service.SearchAsync(query, default)).Value!;
            Assert.True(seen.Add(Json(Assert.Single(page.Items)).GetProperty("providerId").GetGuid()));
            if (page.HasMore)
            {
                Assert.InRange(page.NextCursor!.Length, 1, 64);
                var invalid = await service.SearchAsync(query with { Q = "otra", Cursor = page.NextCursor }, default);
                Assert.Equal(NexoCodes.RequestCursorInvalid, invalid.Error!.Code);
            }
            query = query with { Cursor = page.NextCursor };
        } while (page.HasMore);
        Assert.Equal(3, seen.Count);
        Assert.Equal(NexoCodes.RequestCursorInvalid, (await service.SearchAsync(query with { Cursor = "basura" }, default)).Error!.Code);
    }
    [Fact]
    public async Task Ordenes_recomendacion_cercania_y_relevancia_sin_cuentas_inactivas()
    {
        var a = Provider("Cercano", .2, recs: 1);
        var b = Provider("Recomendado", 2, recs: 10);
        var c = Provider("Baja", .1, recs: 99);
        var accounts = new Accounts(); accounts.Inactive.Add(c.AccountId); accounts.Unverified.Add(b.AccountId);
        var service = Service(new(a, b, c), accounts);
        var q = new DirectoryQuery(Lat: Center.Lat, Lng: Center.Lng);
        async Task<string?> First(string sort) => Json((await service.SearchAsync(q with { Sort = sort }, default)).Value!.Items[0]).GetProperty("name").GetString();
        Assert.Equal("Cercano", await First("near")); Assert.Equal("Recomendado", await First("recs")); Assert.Equal("Cercano", await First("relevance"));
        Assert.Equal(2, (await service.SearchAsync(q, default)).Value!.Total);
    }
    [Fact]
    public async Task Sugerencias_ordenadas_publicas_cortas_no_consultan_y_sin_fugas_de_coordenadas()
    {
        var provider = Provider("Fugas del Centro");
        var profiles = new Profiles(provider);
        var service = Service(profiles);
        Assert.Empty((await service.SuggestAsync("f", null, 8, default)).Value!.Suggestions); Assert.Equal(0, profiles.Calls);
        var suggestions = (await service.SuggestAsync("fu", Center, 8, default)).Value!.Suggestions;
        Assert.Equal(new[] { "specialty", "provider" }, suggestions.Select(s => s.Kind));
        var text = Json(suggestions).GetRawText();
        Assert.DoesNotContain("location", text); Assert.DoesNotContain("AccountId", text);
    }
    [Fact]
    public async Task Mapa_usa_punto_difuminado_estable_local_exacto_y_count_antes_de_limit()
    {
        var home = Provider("Home", .24);
        var shop = Provider("Shop", 2, "shop");
        var service = Service(new(home, shop));
        var map = await service.MapAsync(Center, 10, 1, default);
        Assert.Equal(2, map.Count); var point = Assert.Single(map.Points);
        Assert.False(point.Exact); Assert.Equal(home.Profile.PublicZone!.Point, point.Point); Assert.Equal(0, point.DistanceKm);
        Assert.True((await service.MapAsync(Center, 10, 10, default)).Points[1].Exact);
    }
    [Theory]
    [InlineData(double.NaN, -99.3, "relevance")]
    [InlineData(91, -99.3, "relevance")]
    [InlineData(20, -99.3, "invalid")]
    public void Coordenadas_y_enums_invalidos(double lat, double lng, string sort) => Assert.NotNull(new DirectoryQuery(Lat: lat, Lng: lng, Sort: sort).Error);
    [Fact]
    public void Distancia_y_cursor_no_se_fingen_sin_punto()
    {
        Assert.NotNull(new DirectoryQuery(Sort: "near").Error);
        Assert.NotNull(new DirectoryQuery(Attention: "shop").Error);
        Assert.Null(DirectoryService.PublicDistance(null));
        Assert.Equal(.5, DirectoryService.PublicDistance(.47));
    }
}
