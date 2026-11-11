using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Seguridad.Tests;

[Collection("seguridad")]
public sealed class DirectoryEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class Unavailable : IDirectoryProfiles
    {
        public Task<IReadOnlyList<StoredProvider>> CandidatesAsync(ProviderPoint? point, string? category, CancellationToken ct) => throw new DirectoryUnavailableException();
    }
    private WebApplicationFactory<Program> Host() => new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
    private static async Task<JsonElement> Json(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    [Fact]
    public async Task Catalogo_anonimo_ETag_y_autocompletado_sin_dependencia_con_consulta_corta()
    {
        using var host = Host(); using var client = host.CreateClient();
        var response = await client.GetAsync("/v1/discovery/categories"); response.EnsureSuccessStatusCode();
        var body = await Json(response);
        Assert.Equal(7, body.GetProperty("categories").GetArrayLength()); Assert.DoesNotContain("keywords", body.GetRawText());
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/discovery/categories"); request.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        Assert.Equal(HttpStatusCode.NotModified, (await client.SendAsync(request)).StatusCode);
        Assert.Empty((await Json(await client.GetAsync("/v1/discovery/search/suggest?q=p"))).GetProperty("suggestions").EnumerateArray());
        var suggest = await client.GetAsync("/v1/discovery/search/suggest?q=plom"); suggest.EnsureSuccessStatusCode();
        Assert.Equal("category", (await Json(suggest)).GetProperty("suggestions")[0].GetProperty("kind").GetString());
    }
    [Theory]
    [InlineData("search/providers?lat=20")]
    [InlineData("search/providers?lat=NaN&lng=-99")]
    [InlineData("search/providers?open=wrong")]
    [InlineData("search/providers?limit=51")]
    [InlineData("search/providers?cat=no-existe")]
    [InlineData("search/providers?sort=near")]
    [InlineData("search/providers?open=true&open=false")]
    [InlineData("search/suggest?q=abc&limit=11")]
    [InlineData("map/nearby?lat=20&lng=-99&limit=101")]
    [InlineData("map/nearby?lat=20&lng=-99&radiusKm=3")]
    public async Task Parametros_invalidos_no_consultan_datos(string suffix)
    {
        using var host = Host(); using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/v1/discovery/" + suffix)).StatusCode);
    }
    [Fact]
    public async Task Busqueda_y_mapa_publicos_ocultan_home_bajas_y_paginan_con_filtros()
    {
        using var host = Host(); using var client = host.CreateClient();
        var session = await Cuenta.CrearAsync(host, client, $"directory-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var scope = host.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IProviderStore>();
        var me = await Json(await client.GetAsync("/v1/identity/me"));
        var id = me.GetProperty("accountId").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/discovery/map/nearby?lat=20.05&lng=-99.34&kind=requests")).StatusCode);
        var unique = "Directorio" + Guid.NewGuid().ToString("N");
        var profile = new ProviderProfile
        {
            ProviderId = Guid.CreateVersion7(),
            Name = unique,
            Bio = "Reparación de fugas domésticas",
            Categories = ["plomeria"],
            Specialties = ["fugas"],
            Location = new(20.05, -99.34),
            PublicZone = new("Zona aproximada", new(20.056, -99.346)),
            RadiusKm = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Assert.True(await store.InsertAsync(new(id, profile, 0, "private-key", "private-hash", true), default));
        using var anon = host.CreateClient();
        var response = await anon.GetAsync($"/v1/discovery/search/providers?q={unique}&lat=20.05&lng=-99.34"); response.EnsureSuccessStatusCode();
        var result = await Json(response); Assert.Equal(1, result.GetProperty("total").GetInt32());
        var card = result.GetProperty("items")[0];
        foreach (var forbidden in new[] { "location", "locationGeo", "publicZone", "accountId", "phone", "email", "owner", "creationProfile" }) Assert.False(card.TryGetProperty(forbidden, out _), forbidden);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var map = await Json(await anon.GetAsync("/v1/discovery/map/nearby?lat=20.05&lng=-99.34"));
        var point = map.GetProperty("points").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == profile.ProviderId); Assert.False(point.GetProperty("exact").GetBoolean());
        Assert.Equal(20.056, point.GetProperty("point").GetProperty("lat").GetDouble());
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/v1/discovery/map/nearby?lat=20.05&lng=-99.34&kind=requests")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/discovery/map/nearby?lat=20.05&lng=-99.34&kind=requests")).StatusCode);
        // Account lifecycle checked through the real Identity contract, not a cached flag in profile.
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena, reason = "Prueba de aislamiento" })).StatusCode);
        Assert.Equal(0, (await Json(await anon.GetAsync($"/v1/discovery/search/providers?q={unique}"))).GetProperty("total").GetInt32());
    }
    [Fact]
    public async Task Dependencia_caida_devuelve_503_y_no_resultados_demo()
    {
        using var root = Host(); using var host = root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IDirectoryProfiles>(); s.AddSingleton<IDirectoryProfiles, Unavailable>();
        }));
        using var client = host.CreateClient();
        foreach (var path in new[] { "search/providers", "search/suggest?q=zz", "map/nearby?lat=20.05&lng=-99.34" })
        {
            var response = await client.GetAsync("/v1/discovery/" + path);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(NexoCodes.ServerDependencyUnavailable, (await Json(response)).GetProperty("code").GetString());
        }
    }
}
