using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Discovery.Application;

namespace Nexo.Seguridad.Tests;

[Collection("seguridad")]
public sealed class GeocodingTests(InfraDeSeguridad infra)
{
    private sealed class Geocoder(bool fail = false) : IPlaceGeocoder
    {
        public Task<IReadOnlyList<Place>> SearchAsync(string query, double? lat, double? lng, CancellationToken ct) =>
            fail ? throw new GeocodingUnavailableException() : Task.FromResult<IReadOnlyList<Place>>([new(20.05, -99.34, "Jardín, Tula de Allende", "Tula de Allende, Hidalgo")]);
        public Task<Place?> ReverseAsync(double lat, double lng, CancellationToken ct) => Task.FromResult<Place?>(null);
    }
    [Fact]
    public async Task Registro_puede_buscar_sin_sesion_pero_ubicacion_privada_exige_sesion()
    {
        using var root = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
        using var host = root.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IPlaceGeocoder>(); s.AddSingleton<IPlaceGeocoder>(new Geocoder()); }));
        using var client = host.CreateClient();
        var response = await client.GetAsync("/v1/discovery/places/search?q=Tula");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tula de Allende, Hidalgo", json.GetProperty("items")[0].GetProperty("zoneLabel").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/discovery/me/location")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/v1/discovery/places/search?q=ab")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/v1/discovery/places/reverse?lat=45&lng=0")).StatusCode);
        Assert.Null((await client.GetFromJsonAsync<JsonElement>("/v1/discovery/places/reverse?lat=20.05&lng=-99.34")).GetProperty("place").Deserialize<Place>());
    }
    [Fact]
    public async Task Dependencia_caida_devuelve_503_sin_datos_simulados()
    {
        using var root = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
        using var host = root.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IPlaceGeocoder>(); s.AddSingleton<IPlaceGeocoder>(new Geocoder(true)); }));
        using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/v1/discovery/places/search?q=Tula")).StatusCode);
    }
}
