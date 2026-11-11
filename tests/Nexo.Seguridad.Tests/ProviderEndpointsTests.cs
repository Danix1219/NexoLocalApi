using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application;
using Nexo.Discovery.Domain;

namespace Nexo.Seguridad.Tests;

[Collection("seguridad")]
public sealed class ProviderEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class Geocoder : IPlaceGeocoder
    {
        public Task<IReadOnlyList<Place>> SearchAsync(string query, double? lat, double? lng, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Place>>([]);
        public Task<Place?> ReverseAsync(double lat, double lng, CancellationToken ct) =>
            Task.FromResult<Place?>(new(lat, lng, "Dirección exacta reservada", "Tula, Hidalgo"));
    }
    private sealed class SelectedLocation : ISearchLocationStore
    {
        public Task SaveAsync(Guid accountId, SearchLocation location, CancellationToken ct) => Task.CompletedTask;
        public Task<SearchLocation?> GetAsync(Guid accountId, CancellationToken ct) =>
            Task.FromResult<SearchLocation?>(new(20.0539, -99.3403, 5, "map", "Santa María Magdalena, Hidalgo"));
    }
    private sealed class UnavailableGeocoder : IPlaceGeocoder
    {
        public Task<IReadOnlyList<Place>> SearchAsync(string query, double? lat, double? lng, CancellationToken ct) =>
            throw new GeocodingUnavailableException();
        public Task<Place?> ReverseAsync(double lat, double lng, CancellationToken ct) =>
            throw new GeocodingUnavailableException();
    }
    // Only the external moderation dependency is a double. HTTP/auth/Mongo/Redis are real.
    private sealed class Safety(string? error = null) : IProviderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(error == null ? Result<bool>.Success(true) : Result<bool>.Failure(error, "Moderacion no disponible o rechazada."));
        public Task<Result<ProviderImage>> ImageAsync(Guid owner, Guid upload, string purpose, CancellationToken ct) =>
            Task.FromResult(Result<ProviderImage>.Failure(NexoCodes.UploadNotFound, "Subida no disponible."));
    }
    private WebApplicationFactory<Program> Host(string? error = null)
    {
        var host = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
        return host.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProviderSafety>(); services.AddSingleton<IProviderSafety>(new Safety(error));
        }));
    }
    private static object Body(string name = "Oficio Local") => new
    {
        kind = "trade",
        name,
        trades = new[] { "Plomero" },
        categories = new[] { "plomeria" },
        specialties = new[] { "fugas" },
        bio = "Reparamos fugas y tuberias de los hogares.",
        attention = "home",
        location = new { lat = 20.0539, lng = -99.3403 },
        radiusKm = 5,
        acceptedRules = true
    };
    private static async Task Login(WebApplicationFactory<Program> host, HttpClient client)
    {
        var session = await Cuenta.CrearAsync(host, client, $"perfil-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
    }
    private static Task<HttpResponseMessage> Create(HttpClient client, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/discovery/providers") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }
    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString());
    }
    [Fact]
    public async Task Zona_publica_del_prestador_muestra_localidad_real_y_oculta_punto_exacto()
    {
        using var host = Host().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPlaceGeocoder>(); services.AddSingleton<IPlaceGeocoder>(new Geocoder());
        }));
        using var client = host.CreateClient();
        await Login(host, client);
        var created = await Json(await Create(client, Body()));
        var zone = created.GetProperty("publicZone");
        Assert.Equal("Tula, Hidalgo", zone.GetProperty("label").GetString());
        Assert.False(created.TryGetProperty("location", out _));
        var point = zone.GetProperty("point");
        Assert.InRange(new ProviderPoint(20.0539, -99.3403).DistanceKm(new(point.GetProperty("lat").GetDouble(), point.GetProperty("lng").GetDouble())), .499999, .800001);
        Assert.Equal("Tula, Hidalgo", (await Json(await client.GetAsync("/v1/discovery/providers/me"))).GetProperty("publicZone").GetProperty("label").GetString());
    }
    [Fact]
    public async Task Zona_del_prestador_usa_seleccion_guardada_si_coincide_con_el_punto()
    {
        using var host = Host().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISearchLocationStore>(); services.AddSingleton<ISearchLocationStore>(new SelectedLocation());
            services.RemoveAll<IPlaceGeocoder>(); services.AddSingleton<IPlaceGeocoder>(new UnavailableGeocoder());
        }));
        using var client = host.CreateClient();
        await Login(host, client);
        var created = await Json(await Create(client, Body()));
        Assert.Equal("Santa María Magdalena, Hidalgo", created.GetProperty("publicZone").GetProperty("label").GetString());
    }
    [Fact]
    public async Task Alta_idempotente_edicion_por_seccion_y_perfil_publico_privado()
    {
        using var host = Host(); using var client = host.CreateClient();
        await Login(host, client);
        var key = Guid.NewGuid().ToString();
        var created = await Create(client, Body(), key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var first = await Json(created);
        var id = first.GetProperty("providerId").GetGuid();
        var blurred = first.GetProperty("publicZone").GetProperty("point");
        Assert.InRange(new ProviderPoint(20.0539, -99.3403).DistanceKm(new(blurred.GetProperty("lat").GetDouble(), blurred.GetProperty("lng").GetDouble())), .499999, .800001);
        Assert.False(first.TryGetProperty("location", out _));
        Assert.Equal(id, (await Json(await Create(client, Body(), key))).GetProperty("providerId").GetGuid());
        await Error(await Create(client, Body("Otro Nombre"), key), HttpStatusCode.Conflict, NexoCodes.RequestIdempotencyKeyReused);
        await Error(await Create(client, Body()), HttpStatusCode.Conflict, NexoCodes.ProviderAlreadyExists);
        var identity = await Json(await client.GetAsync("/v1/identity/me"));
        Assert.True(identity.GetProperty("hasProvider").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/v1/identity/me/mode", new { mode = "provider" })).StatusCode);
        var own = await Json(await client.GetAsync("/v1/discovery/providers/me"));
        Assert.Equal(20.0539, own.GetProperty("location").GetProperty("lat").GetDouble());
        Assert.Equal(35, own.GetProperty("completeness").GetInt32());
        Assert.Equal(first.GetProperty("policyVersion").GetString(), own.GetProperty("policyVersion").GetString());
        Assert.Equal(first.GetProperty("completeness").GetInt32(), own.GetProperty("completeness").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/v1/discovery/providers/me/basic", new { name = "Oficio Editado" })).StatusCode);
        Assert.Equal("Oficio Local", (await Json(await Create(client, Body(), key))).GetProperty("name").GetString());
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/basic", new { accountId = Guid.NewGuid() }), HttpStatusCode.BadRequest, NexoCodes.ValidationFailed);
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/hours", new { schedule = new { days = new object?[] { null }, urgent = false } }), HttpStatusCode.BadRequest, NexoCodes.ProviderScheduleInvalid);
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/prices", new { prices = Enumerable.Range(0, 4).Select(_ => new { service = "Visita", fromCents = 100, unit = "from" }) }), HttpStatusCode.BadRequest, "provider.prices_limit");
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/terms", new { channels = new { chat = true, call = true } }), HttpStatusCode.BadRequest, NexoCodes.PhoneInvalid);
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/portfolio", new { portfolioUploadIds = new[] { Guid.NewGuid() } }), HttpStatusCode.NotFound, NexoCodes.UploadNotFound);
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/no-existe", new { name = "Nada" }), HttpStatusCode.NotFound, NexoCodes.ResourceNotFound);
        var hours = await client.PatchAsJsonAsync("/v1/discovery/providers/me/hours", new { schedule = new { days = new object?[] { new { open = "08:00", close = "18:00" }, null, null, null, null, null, null }, urgent = true } });
        Assert.Equal(HttpStatusCode.OK, hours.StatusCode);
        Assert.Equal("Oficio Editado", (await Json(hours)).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/v1/discovery/providers/me/services", new { bio = "Reparamos tuberias y fugas de agua en hogares." })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/v1/discovery/providers/me/prices", new { visit = new { mode = "free" }, prices = new[] { new { service = "Reparacion", fromCents = 10000, unit = "from", suffix = "" } } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/v1/discovery/providers/me/terms", new { payment = new[] { "cash" }, warranty = "30 dias" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/v1/discovery/providers/me/portfolio", new { portfolioUploadIds = Array.Empty<Guid>() })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/v1/discovery/providers/me/zone", new { radiusKm = 10 })).StatusCode);
        using var anonymous = host.CreateClient();
        var publicResponse = await anonymous.GetAsync($"/v1/discovery/providers/{id}?lat=20.05&lng=-99.34");
        var pub = await Json(publicResponse);
        foreach (var forbidden in new[] { "location", "accountId", "email", "phone", "stats", "creationProfile" }) Assert.False(pub.TryGetProperty(forbidden, out _), forbidden);
        Assert.Equal(JsonValueKind.Null, pub.GetProperty("shopLocation").ValueKind);
        Assert.True(pub.GetProperty("emailVerified").GetBoolean());
        var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/discovery/providers/{id}?lat=20.05&lng=-99.34");
        request.Headers.IfNoneMatch.Add(publicResponse.Headers.ETag!);
        Assert.Equal(HttpStatusCode.NotModified, (await anonymous.SendAsync(request)).StatusCode);
        Assert.True((await Json(await client.GetAsync("/v1/discovery/providers/me"))).GetProperty("stats").GetProperty("views").GetInt64() >= 2);
    }
    [Fact]
    public async Task Comparacion_respeta_orden_dos_ids_y_oculta_cuentas_en_baja()
    {
        using var host = Host(); using var one = host.CreateClient(); using var two = host.CreateClient(); using var anonymous = host.CreateClient();
        await Login(host, one); await Login(host, two);
        var a = (await Json(await Create(one, Body("Prestador Uno")))).GetProperty("providerId").GetGuid();
        await Error(await two.GetAsync("/v1/discovery/providers/me"), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
        await Error(await two.PatchAsJsonAsync("/v1/discovery/providers/me/basic", new { name = "Ajeno" }), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
        var b = (await Json(await Create(two, Body("Prestador Dos")))).GetProperty("providerId").GetGuid();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/v1/discovery/providers/compare?ids={a},{b}")).StatusCode);
        await Error(await one.GetAsync($"/v1/discovery/providers/compare?ids={a},{a}"), HttpStatusCode.BadRequest, NexoCodes.CompareNeedTwoIds);
        await Error(await one.GetAsync($"/v1/discovery/providers/compare?ids={a},{Guid.NewGuid()}"), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
        var compare = await Json(await one.GetAsync($"/v1/discovery/providers/compare?ids={b},{a}&lat=20.05&lng=-99.34"));
        Assert.Equal(b, compare.GetProperty("providers")[0].GetProperty("providerId").GetGuid());
        Assert.Equal(a, compare.GetProperty("providers")[1].GetProperty("providerId").GetGuid());
        Assert.Equal(14, compare.GetProperty("rows").GetArrayLength());
        foreach (var row in compare.GetProperty("rows").EnumerateArray().Where(r => r.GetProperty("key").GetString() is "attention" or "warranty" or "payment" or "emailVerified"))
            Assert.Equal(JsonValueKind.Null, row.GetProperty("best").ValueKind);
        Assert.Equal(HttpStatusCode.Accepted, (await two.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena })).StatusCode);
        await Error(await anonymous.GetAsync($"/v1/discovery/providers/{b}"), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
        await Error(await one.GetAsync($"/v1/discovery/providers/compare?ids={a},{b}"), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
    }
    [Theory]
    [InlineData(NexoCodes.ServerDependencyUnavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(NexoCodes.ContentInappropriate, HttpStatusCode.Forbidden)]
    public async Task Moderacion_no_aprobada_no_persiste(string code, HttpStatusCode status)
    {
        using var host = Host(code); using var client = host.CreateClient();
        await Login(host, client);
        await Error(await Create(client, Body()), status, code);
        await Error(await client.GetAsync("/v1/discovery/providers/me"), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
    }
    [Fact]
    public async Task Negocio_infiere_categorias_y_solo_publica_punto_si_hay_local()
    {
        using var host = Host(); using var client = host.CreateClient();
        await Login(host, client);
        var result = await Json(await Create(client, new
        {
            kind = "business",
            name = "Electricidad Local",
            owner = "Responsable Local",
            sector = "servicios-hogar",
            activity = "Instalaciones electricas",
            bio = "Realizamos instalaciones electricas residenciales y comerciales.",
            categories = new[] { "cerrajeria" },
            attention = "shop",
            location = new { lat = 20.0539, lng = -99.3403 },
            radiusKm = 0,
            acceptedRules = true
        }));
        Assert.Equal("electricidad", result.GetProperty("categories")[0].GetString());
        Assert.True(result.GetProperty("inferredCategories").GetBoolean());
        var id = result.GetProperty("providerId").GetGuid();
        var publicResult = await Json(await client.GetAsync($"/v1/discovery/providers/{id}"));
        Assert.Equal(20.0539, publicResult.GetProperty("shopLocation").GetProperty("lat").GetDouble());
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/zone", new { attention = "home", radiusKm = 0 }), HttpStatusCode.BadRequest, NexoCodes.ProviderRadiusInvalid);
        await Error(await client.PatchAsJsonAsync("/v1/discovery/providers/me/zone", new { location = new { lat = 20.0, lng = -99.0, accountId = "intrusion" } }), HttpStatusCode.BadRequest, NexoCodes.ValidationFailed);
    }
    [Theory]
    [InlineData("GET", "/v1/discovery/providers/me")]
    [InlineData("PATCH", "/v1/discovery/providers/me/basic")]
    [InlineData("POST", "/v1/discovery/providers")]
    public async Task Escrituras_y_lectura_propia_requieren_sesion(string method, string path)
    {
        using var host = Host(); using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) })).StatusCode);
    }
}
