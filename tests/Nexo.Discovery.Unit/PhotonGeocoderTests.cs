using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nexo.Discovery.Application;
using Nexo.Discovery.Infrastructure;

namespace Nexo.Discovery.Unit;

public sealed class PhotonGeocoderTests
{
    private const string Json = """{"features":[{"properties":{"name":"Jardín","city":"Tula de Allende","state":"Hidalgo","countrycode":"MX"},"geometry":{"type":"Point","coordinates":[-99.34,20.05]}}]}""";
    [Fact]
    public async Task Busca_lugares_reales_forma_URL_y_cachea_sin_repetir_consulta()
    {
        var handler = new ResponseHandler(Json);
        using var api = new PhotonGeocoder(new HttpClient(handler), Options.Create(new GeocodingOptions()));
        var first = Assert.Single(await api.SearchAsync("Tula & Jardín", 20.05, -99.34, default));
        Assert.Equal(20.05, first.Lat);
        Assert.Equal(-99.34, first.Lng);
        Assert.Equal("Tula de Allende, Hidalgo", first.ZoneLabel);
        Assert.Contains("q=Tula%20%26%20Jard", handler.Uri);
        await api.SearchAsync("Tula & Jardín", 20.05, -99.34, default);
        Assert.Equal(1, handler.Calls);
    }
    [Theory]
    [InlineData(429)] [InlineData(503)]
    public async Task No_inventa_resultados_cuando_falla_el_proveedor(int status)
    {
        using var api = new PhotonGeocoder(new HttpClient(new ResponseHandler(Json, (HttpStatusCode)status)), Options.Create(new GeocodingOptions()));
        await Assert.ThrowsAsync<GeocodingUnavailableException>(() => api.ReverseAsync(20.05, -99.34, default));
    }
    [Fact]
    public void Una_localidad_sin_campo_city_conserva_su_nombre_en_la_zona()
    {
        using var data = JsonDocument.Parse(Json.Replace("\"city\":\"Tula de Allende\",", "\"type\":\"county\",").Replace("\"name\":\"Jardín\"", "\"name\":\"Tula de Allende\""));
        Assert.Equal("Tula de Allende, Hidalgo", Assert.Single(PhotonGeocoder.Parse(data.RootElement)).ZoneLabel);
    }
    [Fact]
    public void Descarta_extranjero_coordenadas_rotas_y_respuestas_sin_datos()
    {
        using var foreign = JsonDocument.Parse(Json.Replace("\"MX\"", "\"US\""));
        Assert.Empty(PhotonGeocoder.Parse(foreign.RootElement));
        using var invalid = JsonDocument.Parse(Json.Replace("[-99.34,20.05]", "[\"bad\",20.05]"));
        Assert.Empty(PhotonGeocoder.Parse(invalid.RootElement));
        using var malformed = JsonDocument.Parse("{}");
        Assert.Throws<GeocodingUnavailableException>(() => PhotonGeocoder.Parse(malformed.RootElement));
    }
    private sealed class ResponseHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string Uri { get; private set; } = "";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Uri = request.RequestUri!.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
