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
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Seguridad.Tests;

[Collection("seguridad")]
public sealed class WorkEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class UnavailableAccounts : IProviderAccounts
    {
        public Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct) => throw new TimeoutException("Internal dependency details must not be returned");
        public Task<bool> EnableProviderAsync(Guid id, CancellationToken ct) => throw new TimeoutException();
    }
    // Only external Content Safety is replaced. HTTP, JWT, Mongo transactions and Redis are real.
    private sealed class Safety(string? error = null, bool images = false) : IProviderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(error == null ? Result<bool>.Success(true) : Result<bool>.Failure(error, "Rechazado/no disponible"));
        public Task<Result<ProviderImage>> ImageAsync(Guid owner, Guid id, string purpose, CancellationToken ct) => Task.FromResult(images
            ? Result<ProviderImage>.Success(new("https://cdn.test/image.webp", null, 1, 1)) : Result<ProviderImage>.Failure(NexoCodes.UploadNotFound, "No disponible"));
    }
    private WebApplicationFactory<Program> Host(string? error = null, bool images = false)
    {
        var host = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
        return host.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IProviderSafety>(); s.AddSingleton<IProviderSafety>(new Safety(error, images)); }));
    }
    private static object Body(string title = "Trabajo de prueba", Guid[]? images = null, string category = "plomeria") => new
    {
        category, title, text = "Reparamos las tuberías y comprobamos el funcionamiento.", problem = "Fuga en el calentador", did = "Cambio de válvula", time = "2 horas", tag = "Terminado",
        location = new { lat = 20.053912345, lng = -99.340312345 }, imageUploadIds = images ?? []
    };
    private static async Task<JsonElement> Json(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    private static async Task<Guid> Login(WebApplicationFactory<Program> host, HttpClient client)
    {
        var session = await Cuenta.CrearAsync(host, client, $"works-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (await Json(await client.GetAsync("/v1/identity/me"))).GetProperty("accountId").GetGuid();
    }
    private static async Task<Guid> Provider(WebApplicationFactory<Program> host, Guid owner, bool realZone = false)
    {
        using var scope = host.Services.CreateScope(); var id = Guid.CreateVersion7();
        var profile = new ProviderProfile { ProviderId = id, Name = "Prestador de trabajos", Categories = ["plomeria"], Location = realZone ? new(20.053912345, -99.340312345) : new(20.05, -99.34),
            PublicZone = new(realZone ? "Santa María Magdalena, Hidalgo" : "Zona aproximada", new(20.056, -99.346)), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        Assert.True(await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(owner, profile, 0, Guid.NewGuid().ToString(), "hash", true), default));
        return id;
    }
    private static Task<HttpResponseMessage> Publish(HttpClient client, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/discovery/works") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString()); return client.SendAsync(request);
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode expected, string code)
    {
        Assert.Equal(expected, response.StatusCode); Assert.Equal(code, (await Json(response)).GetProperty("code").GetString());
    }
    [Fact]
    public async Task Trabajo_en_punto_del_perfil_publica_su_zona_real()
    {
        using var host = Host(); using var client = host.CreateClient();
        var owner = await Login(host, client);
        await Provider(host, owner, realZone: true);
        var created = await Json(await Publish(client, Body()));
        Assert.Equal("Santa María Magdalena, Hidalgo", created.GetProperty("zoneLabel").GetString());
        Assert.False(created.TryGetProperty("location", out _));
    }
    [Fact]
    public async Task Visitante_no_publica_borra_ni_recomienda_y_listado_oculta_proveedor_inexistente()
    {
        using var host = Host(); using var client = host.CreateClient(); var id = Guid.CreateVersion7();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Publish(client, Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/v1/discovery/works/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/v1/discovery/works/{id}/recommend", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/v1/discovery/works/{id}/recommend")).StatusCode);
        await Error(await client.GetAsync($"/v1/discovery/providers/{id}/works"), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
        await Error(await client.GetAsync($"/v1/discovery/providers/{id}/works?limit=51"), HttpStatusCode.BadRequest, NexoCodes.RequestLimitExceeded);
    }
    [Fact]
    public async Task Editar_trabajo_solo_dueno_revalida_y_actualiza_lista()
    {
        using var host = Host(); using var owner = host.CreateClient(); using var other = host.CreateClient();
        var account = await Login(host, owner); var provider = await Provider(host, account);
        await Login(host, other);
        var created = await Json(await Publish(owner, Body("Trabajo original")));
        var id = created.GetProperty("workId").GetGuid(); var path = $"/v1/discovery/works/{id}";
        var edit = new { title = "Trabajo actualizado", text = "Reparamos la tubería y comprobamos que ya no hay fugas.",
            tag = "Finalizado", imageUploadIds = Array.Empty<Guid>() };
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.CreateClient().PatchAsJsonAsync(path, edit)).StatusCode);
        await Error(await other.PatchAsJsonAsync(path, edit), HttpStatusCode.NotFound, NexoCodes.WorkNotFound);
        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync(path, edit)).StatusCode);
        var list = (await Json(await owner.GetAsync($"/v1/discovery/providers/{provider}/works"))).GetProperty("items")[0];
        Assert.Equal("Trabajo actualizado", list.GetProperty("title").GetString());
        Assert.Equal(edit.text, list.GetProperty("text").GetString());
        Assert.Equal("Finalizado", list.GetProperty("tag").GetString());
        Assert.False(list.TryGetProperty("location", out _));
    }
    [Fact]
    public async Task Ciclo_real_idempotencia_recomendacion_privacidad_y_borrado_por_propietario()
    {
        using var host = Host(); using var author = host.CreateClient(); using var viewer = host.CreateClient(); using var anonymous = host.CreateClient();
        var owner = await Login(host, author); var provider = await Provider(host, owner); var viewerId = await Login(host, viewer);
        var key = Guid.NewGuid().ToString(); var first = await Publish(author, Body(), key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await Json(first); var id = created.GetProperty("workId").GetGuid();
        Assert.EndsWith(id.ToString(), first.Headers.Location!.ToString());
        Assert.Equal(id, (await Json(await Publish(author, Body(), key))).GetProperty("workId").GetGuid());
        await Error(await Publish(author, Body("Otro trabajo distinto"), key), HttpStatusCode.Conflict, NexoCodes.RequestIdempotencyKeyReused);
        Assert.False(created.TryGetProperty("location", out _)); Assert.Equal("approved", created.GetProperty("moderation").GetProperty("status").GetString());
        var exact = new ProviderPoint(20.053912345, -99.340312345);
        var point = created.GetProperty("publicPoint"); Assert.InRange(exact.DistanceKm(new(point.GetProperty("lat").GetDouble(), point.GetProperty("lng").GetDouble())), .499, .801);
        var anonymousList = await Json(await anonymous.GetAsync($"/v1/discovery/providers/{provider}/works"));
        var item = anonymousList.GetProperty("items")[0]; Assert.False(item.TryGetProperty("recommended", out _)); Assert.False(item.TryGetProperty("saved", out _));
        Assert.Equal(1, anonymousList.GetProperty("total").GetInt32());
        foreach (var _ in Enumerable.Range(0, 2)) Assert.Equal(1, (await Json(await viewer.PostAsync($"/v1/discovery/works/{id}/recommend", null))).GetProperty("recs").GetInt32());
        var mine = await Json(await viewer.GetAsync($"/v1/discovery/providers/{provider}/works"));
        Assert.True(mine.GetProperty("items")[0].GetProperty("recommended").GetBoolean());
        Assert.False((await Json(await author.GetAsync($"/v1/discovery/providers/{provider}/works"))).GetProperty("items")[0].GetProperty("recommended").GetBoolean());
        await Error(await author.PostAsync($"/v1/discovery/works/{id}/recommend", null), HttpStatusCode.Forbidden, NexoCodes.WorkOwnContent);
        await Error(await viewer.DeleteAsync($"/v1/discovery/works/{id}"), HttpStatusCode.NotFound, NexoCodes.WorkNotFound);
        foreach (var _ in Enumerable.Range(0, 2)) Assert.Equal(0, (await Json(await viewer.DeleteAsync($"/v1/discovery/works/{id}/recommend"))).GetProperty("recs").GetInt32());
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery);
        var persisted = await db.GetCollection<BsonDocument>("works").Find(new BsonDocument("_id", id.ToString())).SingleAsync();
        Assert.False(persisted["data"].AsBsonDocument.Contains("location")); Assert.False(persisted.ToJson().Contains(exact.Lat.ToString("R", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.NoContent, (await author.DeleteAsync($"/v1/discovery/works/{id}")).StatusCode);
        await Error(await author.DeleteAsync($"/v1/discovery/works/{id}"), HttpStatusCode.NotFound, NexoCodes.WorkNotFound);
        Assert.Equal(0, (await Json(await anonymous.GetAsync($"/v1/discovery/providers/{provider}/works"))).GetProperty("total").GetInt32());
        await Error(await Publish(author, Body(), key), HttpStatusCode.NotFound, NexoCodes.WorkNotFound);
    }
    [Theory]
    [InlineData(NexoCodes.ContentInappropriate, HttpStatusCode.Forbidden)]
    [InlineData(NexoCodes.ServerDependencyUnavailable, HttpStatusCode.ServiceUnavailable)]
    public async Task Moderacion_cerrada_y_campos_fuera_de_contrato_no_publican(string error, HttpStatusCode status)
    {
        using var host = Host(error); using var client = host.CreateClient(); var owner = await Login(host, client); var provider = await Provider(host, owner);
        await Error(await Publish(client, Body()), status, error);
        var list = await Json(await client.GetAsync($"/v1/discovery/providers/{provider}/works")); Assert.Empty(list.GetProperty("items").EnumerateArray());
        await Error(await Publish(client, Body(category: "pintura")), HttpStatusCode.BadRequest, NexoCodes.WorkCategoryNotInProfile);
        await Error(await Publish(client, Body(new string('x', 91))), HttpStatusCode.BadRequest, NexoCodes.ValidationFailed);
        await Error(await client.PostAsJsonAsync("/v1/discovery/works", Body()), HttpStatusCode.BadRequest, NexoCodes.RequestIdempotencyKeyMissing);
    }
    [Theory]
    [InlineData(false, HttpStatusCode.NotFound, NexoCodes.UploadNotFound)]
    [InlineData(true, HttpStatusCode.ServiceUnavailable, NexoCodes.ServerDependencyUnavailable)]
    public async Task Imagenes_ajenas_o_sin_purga_disponible_no_se_publican(bool approved, HttpStatusCode status, string code)
    {
        using var host = Host(images: approved); using var client = host.CreateClient(); var owner = await Login(host, client); await Provider(host, owner);
        await Error(await Publish(client, Body(images: [Guid.NewGuid()])), status, code);
        await Error(await Publish(client, Body(images: Enumerable.Range(0, 7).Select(_ => Guid.NewGuid()).ToArray())), HttpStatusCode.BadRequest, NexoCodes.ContentImagesLimit);
    }
    [Fact]
    public async Task Categoria_perfil_y_cursores_validan_en_servidor_y_cuenta_en_baja_no_aparece()
    {
        using var host = Host(); using var client = host.CreateClient(); using var anonymous = host.CreateClient(); var owner = await Login(host, client);
        await Error(await Publish(client, Body()), HttpStatusCode.Forbidden, NexoCodes.ProviderProfileRequired);
        var provider = await Provider(host, owner);
        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.Created, (await Publish(client, Body())).StatusCode);
        var page = await Json(await anonymous.GetAsync($"/v1/discovery/providers/{provider}/works?limit=1"));
        Assert.True(page.GetProperty("hasMore").GetBoolean()); var cursor = page.GetProperty("nextCursor").GetString(); Assert.InRange(cursor!.Length, 1, 64);
        var next = await Json(await anonymous.GetAsync($"/v1/discovery/providers/{provider}/works?limit=1&cursor={cursor}"));
        Assert.NotEqual(page.GetProperty("items")[0].GetProperty("workId").GetGuid(), next.GetProperty("items")[0].GetProperty("workId").GetGuid());
        await Error(await anonymous.GetAsync($"/v1/discovery/providers/{Guid.NewGuid()}/works?cursor={cursor}"), HttpStatusCode.BadRequest, NexoCodes.RequestCursorInvalid);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena })).StatusCode);
        await Error(await anonymous.GetAsync($"/v1/discovery/providers/{provider}/works"), HttpStatusCode.NotFound, NexoCodes.ProviderNotFound);
    }
    [Fact]
    public async Task Dependencias_caidas_devuelven_503_sin_detalles_internos_ni_exito_falso()
    {
        using var root = Host(); using var host = root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IProviderAccounts>(); s.AddSingleton<IProviderAccounts, UnavailableAccounts>();
        }));
        using var client = host.CreateClient(); var owner = await Login(host, client); var provider = await Provider(host, owner);
        var response = await Publish(client, Body());
        await Error(response, HttpStatusCode.ServiceUnavailable, NexoCodes.ServerDependencyUnavailable);
        Assert.DoesNotContain("Internal dependency", await response.Content.ReadAsStringAsync());
        await Error(await client.GetAsync($"/v1/discovery/providers/{provider}/works"), HttpStatusCode.ServiceUnavailable, NexoCodes.ServerDependencyUnavailable);
        await Error(await client.PostAsync($"/v1/discovery/works/{Guid.NewGuid()}/recommend", null), HttpStatusCode.ServiceUnavailable, NexoCodes.ServerDependencyUnavailable);
        await Error(await client.DeleteAsync($"/v1/discovery/works/{Guid.NewGuid()}"), HttpStatusCode.ServiceUnavailable, NexoCodes.ServerDependencyUnavailable);
    }
}
