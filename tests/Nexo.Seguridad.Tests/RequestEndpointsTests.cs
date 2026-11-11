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
using Nexo.Outbox;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Requests;
using Nexo.Discovery.Domain;
using Nexo.Engagement.Application.Interests;

namespace Nexo.Seguridad.Tests;

[Collection("seguridad")]
public sealed class RequestEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class Safety(string? error = null, bool image = false, string? onlyText = null) : IProviderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(error == null || (onlyText != null && text != onlyText)
            ? Result<bool>.Success(true) : Result<bool>.Failure(error, "No aprobado/disponible"));
        public Task<Result<ProviderImage>> ImageAsync(Guid owner, Guid id, string purpose, CancellationToken ct) => Task.FromResult(image
            ? Result<ProviderImage>.Success(new("https://cdn.test/image.webp", null, 1, 1)) : Result<ProviderImage>.Failure(NexoCodes.UploadNotFound, "No disponible"));
    }
    private sealed class MissingSummary : IRequestInterests
    {
        public Task<RequestInterestResult> ReadAsync(Guid[] requests, Guid? providerId, CancellationToken ct) => Task.FromResult(new RequestInterestResult(false, new Dictionary<Guid, RequestInterest>()));
    }
    private sealed class UnavailableInterestAccess : IInterestRequestAccess
    {
        public Task<InterestRequestAccess> ForProviderAsync(Guid requestId, Guid providerId, CancellationToken ct) => throw new InterestUnavailableException();
        public Task<InterestRequestContext?> OwnedAsync(Guid requestId, Guid clientId, CancellationToken ct) => throw new InterestUnavailableException();
    }
    private WebApplicationFactory<Program> Host(string? error = null, bool image = false, bool summaryUnavailable = false, string? onlyText = null, bool interestUnavailable = false)
    {
        var host = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
        return host.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IProviderSafety>(); s.AddSingleton<IProviderSafety>(new Safety(error, image, onlyText));
            if (summaryUnavailable) { s.RemoveAll<IRequestInterests>(); s.AddSingleton<IRequestInterests, MissingSummary>(); }
            if (interestUnavailable) { s.RemoveAll<IInterestRequestAccess>(); s.AddSingleton<IInterestRequestAccess, UnavailableInterestAccess>(); }
        }));
    }
    private static object Body(string title = "Fuga en el baño", string cat = "plomeria", string urgency = "today", string contact = "chat", double lat = 20.05, Guid? image = null, string? description = null) => new
    {
        category = cat, title, description = description ?? "Necesito reparar la tubería debajo del lavabo.", urgency, contact,
        location = new { lat, lng = -99.34 }, imageUploadId = image
    };
    private static async Task<JsonElement> Json(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    private static async Task<Guid> Login(WebApplicationFactory<Program> host, HttpClient client)
    {
        var session = await Cuenta.CrearAsync(host, client, $"requests-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (await Json(await client.GetAsync("/v1/identity/me"))).GetProperty("accountId").GetGuid();
    }
    private static async Task<Guid> Provider(WebApplicationFactory<Program> host, Guid owner, int radius = 5)
    {
        using var scope = host.Services.CreateScope(); var id = Guid.CreateVersion7();
        Assert.True(await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(owner, new()
        {
            ProviderId = id, Name = "Plomería de pruebas", Categories = ["plomeria"], Location = new(20.05, -99.34), PublicZone = new("Zona", new(20.056, -99.346)),
            Attention = radius == 0 ? "shop" : "home", RadiusKm = radius, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        }, 0, Guid.NewGuid().ToString(), "hash", true), default));
        return id;
    }
    private static Task<HttpResponseMessage> Publish(HttpClient client, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/discovery/requests") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString()); return client.SendAsync(request);
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string code)
    { Assert.Equal(status, response.StatusCode); Assert.Equal(code, (await Json(response)).GetProperty("code").GetString()); }
    [Fact]
    public async Task Visitante_no_publica_ni_lee_listas_privadas_ni_cierra()
    {
        using var host = Host(); using var client = host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Publish(client, Body())).StatusCode);
        foreach (var path in new[] { "/v1/discovery/requests/mine", "/v1/discovery/requests/opportunities" }) Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync($"/v1/discovery/requests/{Guid.NewGuid()}/close", new { })).StatusCode);
    }
    [Fact]
    public async Task Listados_entregan_descripcion_completa_y_excerpt_compatible()
    {
        using var host=Host();using var client=host.CreateClient();using var pro=host.CreateClient();
        await Login(host,client);var providerOwner=await Login(host,pro);await Provider(host,providerOwner);
        var full="Necesito reparar una fuga de agua. "+new string('x',320);
        var title="Caso"+Guid.NewGuid().ToString("N");
        var created=await Publish(client,Body(title,description:full));Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        var own=(await Json(await client.GetAsync("/v1/discovery/requests/mine"))).GetProperty("items")[0];
        Assert.Equal(full,own.GetProperty("description").GetString());
        Assert.Equal(full[..200],own.GetProperty("descriptionExcerpt").GetString());
        var other=(await Json(await pro.GetAsync("/v1/discovery/requests/opportunities?q="+title))).GetProperty("items")[0];
        Assert.Equal(full,other.GetProperty("description").GetString());
        Assert.False(other.TryGetProperty("location",out _));
    }
    [Fact]
    public async Task Editar_solo_propia_abierta_actualiza_listas_y_no_expone_ubicacion()
    {
        using var host = Host(); using var client = host.CreateClient(); using var other = host.CreateClient();
        await Login(host, client); await Login(host, other);
        var created = await Json(await Publish(client, Body("Fuga original" + Guid.NewGuid().ToString("N"))));
        var id = created.GetProperty("requestId").GetGuid();
        var path = $"/v1/discovery/requests/{id}";
        var edit = new { title = "Fuga corregida", description = "Necesito revisar una fuga de agua en cocina.",
            urgency = "week", contact = "chat", imageUploadIds = Array.Empty<Guid>() };
        await Error(await other.PatchAsJsonAsync(path, edit), HttpStatusCode.NotFound, NexoCodes.RequestNotFound);
        var response = await client.PatchAsJsonAsync(path, edit);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await Json(response);
        Assert.Equal("Fuga corregida", updated.GetProperty("title").GetString());
        Assert.False(updated.TryGetProperty("location", out _));
        var mine = (await Json(await client.GetAsync("/v1/discovery/requests/mine"))).GetProperty("items")[0];
        Assert.Equal("Fuga corregida", mine.GetProperty("title").GetString());
        Assert.Equal(edit.description, mine.GetProperty("description").GetString());
        Assert.Equal("chat", mine.GetProperty("contact").GetString());
        Assert.Empty(mine.GetProperty("imageUrls").EnumerateArray());
        await client.PostAsJsonAsync($"{path}/close", new { });
        await Error(await client.PatchAsJsonAsync(path, edit), HttpStatusCode.Conflict, NexoCodes.RequestAlreadyClosed);
    }
    [Fact]
    public async Task Publicar_idempotente_listar_por_contrato_y_cerrar_por_dueno_no_habilita_resena()
    {
        using var host = Host(); using var client = host.CreateClient(); using var pro = host.CreateClient();
        var owner = await Login(host, client); var proOwner = await Login(host, pro); var provider = await Provider(host, proOwner);
        var title = "Caso" + Guid.NewGuid().ToString("N"); var key = Guid.NewGuid().ToString();
        var createdResponse = await Publish(client, Body(title), key); Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await Json(createdResponse); var id = created.GetProperty("requestId").GetGuid();
        Assert.EndsWith(id.ToString(), createdResponse.Headers.Location!.ToString()); Assert.False(created.TryGetProperty("location", out _));
        Assert.True(created.GetProperty("reachableProviders").GetInt32() >= 1); Assert.Equal(0, created.GetProperty("interestCount").GetInt32());
        var again = await Json(await Publish(client, Body(title), key)); Assert.Equal(id, again.GetProperty("requestId").GetGuid()); Assert.Equal(created.GetProperty("publicPoint").GetRawText(), again.GetProperty("publicPoint").GetRawText());
        await Error(await Publish(client, Body(title, urgency: "week"), key), HttpStatusCode.Conflict, NexoCodes.RequestIdempotencyKeyReused);
        Assert.Empty((await Json(await pro.GetAsync("/v1/discovery/requests/mine"))).GetProperty("items").EnumerateArray());
        using var scope = host.Services.CreateScope(); var engagement = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement);
        await engagement.GetCollection<BsonDocument>("interests").InsertOneAsync(new BsonDocument { ["requestId"] = new BsonBinaryData(id, GuidRepresentation.Standard),
            ["providerId"] = new BsonBinaryData(provider, GuidRepresentation.Standard), ["clientId"] = new BsonBinaryData(owner, GuidRepresentation.Standard), ["createdAt"] = DateTime.UtcNow });
        var mine = await Json(await client.GetAsync("/v1/discovery/requests/mine")); var item = mine.GetProperty("items")[0];
        Assert.True(mine.GetProperty("interestSummaryAvailable").GetBoolean()); Assert.Equal(1, item.GetProperty("interestCount").GetInt32()); Assert.Single(item.GetProperty("interestedPreview").EnumerateArray());
        Assert.DoesNotContain("location", item.GetRawText()); Assert.DoesNotContain("phone", item.GetRawText());
        var opp = await Json(await pro.GetAsync("/v1/discovery/requests/opportunities?q=" + title));
        var opportunity = Assert.Single(opp.GetProperty("items").EnumerateArray()); Assert.True(opportunity.GetProperty("iAmInterested").GetBoolean());
        Assert.Equal("Persona De Prueba", opportunity.GetProperty("client").GetProperty("displayName").GetString());
        foreach (var field in new[] { "location", "clientId", "phone", "email", "contact" }) Assert.False(opportunity.TryGetProperty(field, out _), field);
        await Error(await pro.PostAsJsonAsync($"/v1/discovery/requests/{id}/close", new { }), HttpStatusCode.NotFound, NexoCodes.RequestNotFound);
        var closedResponse = await client.PostAsJsonAsync($"/v1/discovery/requests/{id}/close", new { outcome = "solvedHere", pickedProviderId = provider });
        Assert.Equal(HttpStatusCode.OK, closedResponse.StatusCode); var closed = await Json(closedResponse);
        Assert.False(closed.GetProperty("canReview").GetBoolean()); Assert.Equal(JsonValueKind.Null, closed.GetProperty("reviewProviderId").ValueKind);
        var repeat = await client.PostAsJsonAsync($"/v1/discovery/requests/{id}/close", new { });
        await Error(repeat, HttpStatusCode.Conflict, NexoCodes.RequestAlreadyClosed); Assert.Equal(closed.GetProperty("closedAt").GetDateTime(), (await Json(repeat)).GetProperty("closedAt").GetDateTime());
        Assert.Empty((await Json(await pro.GetAsync("/v1/discovery/requests/opportunities?q=" + title))).GetProperty("items").EnumerateArray());
        var map = await Json(await pro.GetAsync("/v1/discovery/map/nearby?lat=20.05&lng=-99.34&kind=requests"));
        Assert.DoesNotContain(map.GetProperty("points").EnumerateArray(), p => p.GetProperty("id").GetGuid() == id);
    }
    [Fact]
    public async Task Oportunidades_filtran_rubro_radio_urgencia_propias_y_cursor_sigue_tras_cerrar_ancla()
    {
        using var host = Host(); using var client = host.CreateClient(); using var pro = host.CreateClient();
        await Login(host, client); var proOwner = await Login(host, pro); await Provider(host, proOwner, 0);
        var prefix = "Filtros" + Guid.NewGuid().ToString("N");
        var first = await Json(await Publish(client, Body(prefix + " hoy"))); var id = first.GetProperty("requestId").GetGuid();
        Assert.Equal(HttpStatusCode.Created, (await Publish(client, Body(prefix + " pintura", "pintura", "week"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Publish(client, Body(prefix + " lejos", urgency: "noRush", lat: 21.05))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Publish(pro, Body(prefix + " propia"))).StatusCode);
        async Task<JsonElement> Query(string suffix) => await Json(await pro.GetAsync("/v1/discovery/requests/opportunities?q=" + Uri.EscapeDataString(prefix) + suffix));
        Assert.Equal(1, (await Query("")).GetProperty("total").GetInt32());
        Assert.Equal(2, (await Query("&myServiceOnly=false")).GetProperty("total").GetInt32());
        Assert.Equal(2, (await Query("&inRadius=false")).GetProperty("total").GetInt32());
        Assert.Equal(1, (await Query("&myServiceOnly=false&inRadius=false&today=true")).GetProperty("total").GetInt32());
        await Error(await pro.GetAsync("/v1/discovery/requests/opportunities?cat=pintura"), HttpStatusCode.BadRequest, NexoCodes.ValidationFailed);
        var page = await Query("&myServiceOnly=false&inRadius=false&limit=1"); var cursor = page.GetProperty("nextCursor").GetString();
        Assert.InRange(cursor!.Length, 1, 64); Assert.Equal(id, page.GetProperty("items")[0].GetProperty("requestId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/v1/discovery/requests/{id}/close", null)).StatusCode);
        var next = await Query("&myServiceOnly=false&inRadius=false&limit=1&cursor=" + cursor); Assert.NotEqual(id, next.GetProperty("items")[0].GetProperty("requestId").GetGuid());
        await Error(await pro.GetAsync("/v1/discovery/requests/opportunities?q=diferente&cursor=" + cursor), HttpStatusCode.BadRequest, NexoCodes.RequestCursorInvalid);
        var mine = await Json(await client.GetAsync("/v1/discovery/requests/mine?limit=1")); var ownCursor = mine.GetProperty("nextCursor").GetString();
        await Error(await pro.GetAsync("/v1/discovery/requests/mine?cursor=" + ownCursor), HttpStatusCode.BadRequest, NexoCodes.RequestCursorInvalid);
    }
    [Theory]
    [InlineData(NexoCodes.ContentInappropriate, HttpStatusCode.Forbidden)]
    [InlineData(NexoCodes.ServerDependencyUnavailable, HttpStatusCode.ServiceUnavailable)]
    public async Task Moderacion_cerrada_no_guarda_solicitud(string error, HttpStatusCode status)
    {
        using var host = Host(error); using var client = host.CreateClient(); await Login(host, client);
        await Error(await Publish(client, Body()), status, error);
        Assert.Empty((await Json(await client.GetAsync("/v1/discovery/requests/mine"))).GetProperty("items").EnumerateArray());
    }
    [Theory]
    [InlineData(false, HttpStatusCode.NotFound, NexoCodes.UploadNotFound)]
    [InlineData(true, HttpStatusCode.ServiceUnavailable, NexoCodes.ServerDependencyUnavailable)]
    public async Task Foto_ajena_o_sin_ciclo_real_no_se_publica(bool approved, HttpStatusCode status, string error)
    {
        using var host = Host(image: approved); using var client = host.CreateClient(); await Login(host, client);
        await Error(await Publish(client, Body(image: Guid.NewGuid())), status, error);
    }
    [Fact]
    public async Task Validaciones_contacto_sin_perfil_y_cuenta_en_baja()
    {
        using var host = Host(); using var client = host.CreateClient(); using var pro = host.CreateClient();
        var owner = await Login(host, client); var proOwner = await Login(host, pro); await Provider(host, proOwner);
        await Error(await Publish(client, Body(contact: "chatCall")), HttpStatusCode.BadRequest, NexoCodes.PhoneInvalid);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/v1/identity/me/phone", new { phone = "7715550198" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Publish(client, Body(contact: "chatCall"))).StatusCode);
        await Error(await client.GetAsync("/v1/discovery/requests/opportunities"), HttpStatusCode.Forbidden, NexoCodes.ProviderProfileRequired);
        foreach (var suffix in new[] { "mine?status=invalid", "mine?limit=51", "opportunities?today=wrong", "opportunities?inRadius=true&inRadius=false" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/v1/discovery/requests/" + suffix)).StatusCode);
        await Error(await client.PostAsJsonAsync("/v1/discovery/requests", Body()), HttpStatusCode.BadRequest, NexoCodes.RequestIdempotencyKeyMissing);
        var title = "Baja" + Guid.NewGuid().ToString("N"); Assert.Equal(HttpStatusCode.Created, (await Publish(client, Body(title))).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena })).StatusCode);
        Assert.Empty((await Json(await pro.GetAsync("/v1/discovery/requests/opportunities?q=" + title))).GetProperty("items").EnumerateArray());
    }
    [Fact]
    public async Task Resumen_no_disponible_degrada_sin_fingir_interes_y_mapa_usa_punto_difuminado()
    {
        using var host = Host(summaryUnavailable: true); using var client = host.CreateClient(); using var pro = host.CreateClient();
        await Login(host, client); var proOwner = await Login(host, pro); await Provider(host, proOwner);
        var title = "Resumen" + Guid.NewGuid().ToString("N"); var created = await Json(await Publish(client, Body(title))); var id = created.GetProperty("requestId").GetGuid();
        var mine = await Json(await client.GetAsync("/v1/discovery/requests/mine")); Assert.False(mine.GetProperty("interestSummaryAvailable").GetBoolean());
        var opp = await Json(await pro.GetAsync("/v1/discovery/requests/opportunities?q=" + title)); Assert.False(opp.GetProperty("interestSummaryAvailable").GetBoolean());
        Assert.Equal(JsonValueKind.Null, opp.GetProperty("items")[0].GetProperty("iAmInterested").ValueKind);
        var map = await Json(await pro.GetAsync("/v1/discovery/map/nearby?lat=20.05&lng=-99.34&radiusKm=5&kind=requests"));
        var point = map.GetProperty("points").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == id);
        Assert.False(point.GetProperty("exact").GetBoolean()); Assert.Equal(created.GetProperty("publicPoint").GetRawText(), point.GetProperty("point").GetRawText());
        Assert.DoesNotContain("clientId", point.GetRawText()); Assert.DoesNotContain("location", point.GetRawText());
    }

    [Fact]
    public async Task Interes_se_expresa_lista_retirada_y_deja_eventos_durables()
    {
        using var host = Host(); using var client = host.CreateClient(); using var pro = host.CreateClient(); using var stranger = host.CreateClient();
        await Login(host, client); var providerOwner = await Login(host, pro); await Login(host, stranger);
        var providerId = await Provider(host, providerOwner);
        var title = "Interes " + Guid.NewGuid().ToString("N");
        var request = await Json(await Publish(client, Body(title)));
        var id = request.GetProperty("requestId").GetGuid();
        var path = $"/v1/engagement/requests/{id}";

        Assert.Equal(HttpStatusCode.Unauthorized, (await host.CreateClient().PostAsJsonAsync(path + "/interest", new { note = "Puedo ayudarte" })).StatusCode);
        var firstResponse = await pro.PostAsJsonAsync(path + "/interest", new { note = "Puedo ayudarte" });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = await Json(firstResponse);
        Assert.True(first.GetProperty("interested").GetBoolean()); Assert.Equal(1, first.GetProperty("interestCount").GetInt32());
        Assert.False(first.GetProperty("canMessage").GetBoolean());

        var repeated = await pro.PostAsJsonAsync(path + "/interest", new { note = "Puedo ayudarte" });
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode); Assert.Equal(1, (await Json(repeated)).GetProperty("interestCount").GetInt32());

        await Error(await stranger.GetAsync(path + "/interested"), HttpStatusCode.NotFound, NexoCodes.RequestNotFound);
        var listResponse = await client.GetAsync(path + "/interested?sort=recent&limit=10");
        Assert.True(listResponse.StatusCode == HttpStatusCode.OK, await listResponse.Content.ReadAsStringAsync());
        Assert.Contains("private", listResponse.Headers.CacheControl?.ToString());
        Assert.Contains("no-store", listResponse.Headers.CacheControl?.ToString());
        var list = await Json(listResponse);
        var item = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal(providerId, item.GetProperty("providerId").GetGuid()); Assert.Equal("Puedo ayudarte", item.GetProperty("note").GetString());
        foreach (var hidden in new[] { "phone", "email", "location" }) Assert.False(item.TryGetProperty(hidden, out _), hidden);
        Assert.True(item.GetProperty("actions").GetProperty("canOrder").GetBoolean());

        var withdrawn = await pro.DeleteAsync(path + "/interest"); Assert.Equal(HttpStatusCode.OK, withdrawn.StatusCode);
        Assert.Equal(0, (await Json(withdrawn)).GetProperty("interestCount").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await pro.DeleteAsync(path + "/interest")).StatusCode);
        Assert.Empty((await Json(await client.GetAsync(path + "/interested"))).GetProperty("items").EnumerateArray());
        using var scope = host.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement);
        var events = await database.GetCollection<OutboxMessage>("outbox").Find(FilterDefinition<OutboxMessage>.Empty).ToListAsync();
        Assert.Contains(events, x => x.Type == "interest.expressed" && x.Payload.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, x => x.Type == "interest.withdrawn" && x.Payload.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.Single(events, x => x.Type == "interest.withdrawn" && x.Payload.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Retirar_interes_con_pedido_activo_no_oculta_el_interes_y_luego_es_idempotente()
    {
        using var host = Host(); using var owner = host.CreateClient(); using var provider = host.CreateClient();
        await Login(host, owner); var providerAccount = await Login(host, provider); var providerId = await Provider(host, providerAccount);
        var request = await Json(await Publish(owner, Body("Pedido activo " + Guid.NewGuid().ToString("N"))));
        var requestId = request.GetProperty("requestId").GetGuid(); var path = $"/v1/engagement/requests/{requestId}";
        Assert.Equal(HttpStatusCode.Created, (await provider.PostAsJsonAsync(path + "/interest", new { note = "Interés activo" })).StatusCode);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement);
            await db.GetCollection<BsonDocument>("orders").InsertOneAsync(new BsonDocument
            {
                ["_id"] = Guid.NewGuid().ToString("D"), ["requestId"] = new BsonBinaryData(requestId, GuidRepresentation.Standard),
                ["providerId"] = new BsonBinaryData(providerId, GuidRepresentation.Standard), ["status"] = "pending", ["createdAt"] = DateTime.UtcNow
            });
        }
        await Error(await provider.DeleteAsync(path + "/interest"), HttpStatusCode.Conflict, NexoCodes.InterestCannotWithdrawAssigned);
        var stillVisible = await Json(await owner.GetAsync(path + "/interested")); Assert.Single(stillVisible.GetProperty("items").EnumerateArray());

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement);
            await db.GetCollection<BsonDocument>("orders").UpdateManyAsync(
                new BsonDocument { ["requestId"] = new BsonBinaryData(requestId, GuidRepresentation.Standard), ["providerId"] = new BsonBinaryData(providerId, GuidRepresentation.Standard) },
                new BsonDocument("$set", new BsonDocument("status", "cancelled")));
        }
        Assert.Equal(HttpStatusCode.OK, (await provider.DeleteAsync(path + "/interest")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await provider.DeleteAsync(path + "/interest")).StatusCode);
        Assert.Empty((await Json(await owner.GetAsync(path + "/interested"))).GetProperty("items").EnumerateArray());
        using var verify = host.Services.CreateScope();
        var outbox = verify.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement).GetCollection<OutboxMessage>("outbox");
        var events = await outbox.Find(FilterDefinition<OutboxMessage>.Empty).ToListAsync();
        Assert.Single(events, x => x.Type == "interest.withdrawn" && x.Payload.Contains(requestId.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Falla_del_puente_de_discovery_responde_503_sin_persistir_interes()
    {
        using var host = Host(interestUnavailable: true); using var owner = host.CreateClient(); using var provider = host.CreateClient();
        await Login(host, owner); var providerAccount = await Login(host, provider); await Provider(host, providerAccount);
        var request = await Json(await Publish(owner, Body("Discovery caído " + Guid.NewGuid().ToString("N"))));
        var id = request.GetProperty("requestId").GetGuid();
        await Error(await provider.PostAsJsonAsync($"/v1/engagement/requests/{id}/interest", new { note = "" }),
            HttpStatusCode.ServiceUnavailable, NexoCodes.ServerDependencyUnavailable);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Engagement);
        Assert.Empty(await db.GetCollection<BsonDocument>("interests").Find(new BsonDocument("requestId", new BsonBinaryData(id, GuidRepresentation.Standard))).ToListAsync());
    }

    [Fact]
    public async Task Interes_rechaza_perfil_ausente_solicitud_propia_fuera_de_radio_y_nota_moderada()
    {
        using var host = Host(NexoCodes.ContentInappropriate, onlyText: "nota-bloqueada");
        using var owner = host.CreateClient(); using var provider = host.CreateClient(); using var noProfile = host.CreateClient();
        var ownerId = await Login(host, owner); var providerOwner = await Login(host, provider); await Login(host, noProfile);
        var providerId = await Provider(host, providerOwner);
        await Provider(host, ownerId);
        var ownTitle = "Propia " + Guid.NewGuid().ToString("N");
        var own = await Json(await Publish(owner, Body(ownTitle)));
        var ownResponse = await owner.PostAsJsonAsync($"/v1/engagement/requests/{own.GetProperty("requestId").GetGuid()}/interest", new { note = "Puedo ayudarte" });
        await Error(ownResponse, HttpStatusCode.Forbidden, NexoCodes.InterestOwnRequest);

        var publicTitle = "Radio " + Guid.NewGuid().ToString("N");
        var far = await Json(await Publish(owner, Body(publicTitle, lat: 21.05)));
        await Error(await provider.PostAsJsonAsync($"/v1/engagement/requests/{far.GetProperty("requestId").GetGuid()}/interest", new { note = "Puedo ayudarte" }),
            HttpStatusCode.NotFound, NexoCodes.RequestNotFound);
        var near = await Json(await Publish(owner, Body("Nota " + Guid.NewGuid().ToString("N"))));
        await Error(await provider.PostAsJsonAsync($"/v1/engagement/requests/{near.GetProperty("requestId").GetGuid()}/interest", new { note = "nota-bloqueada" }),
            HttpStatusCode.Forbidden, NexoCodes.ContentInappropriate);
        await Error(await noProfile.PostAsJsonAsync($"/v1/engagement/requests/{near.GetProperty("requestId").GetGuid()}/interest", new { note = "" }),
            HttpStatusCode.Conflict, NexoCodes.ProviderProfileRequired);
    }

    [Fact]
    public async Task Interes_rechaza_solicitud_asignada_y_pagina_interesados_con_cursor()
    {
        using var host = Host(); using var owner = host.CreateClient(); using var firstProvider = host.CreateClient(); using var secondProvider = host.CreateClient();
        await Login(host, owner); var firstAccount = await Login(host, firstProvider); var secondAccount = await Login(host, secondProvider);
        var firstId = await Provider(host, firstAccount); var secondId = await Provider(host, secondAccount);
        var request = await Json(await Publish(owner, Body("Asignada " + Guid.NewGuid().ToString("N"))));
        var id = request.GetProperty("requestId").GetGuid(); var path = $"/v1/engagement/requests/{id}";
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery);
            await db.GetCollection<BsonDocument>("service_requests").UpdateOneAsync(new BsonDocument("_id", id.ToString("D")),
                new BsonDocument("$set", new BsonDocument { ["status"] = "assigned", ["data.status"] = "assigned" }));
        }
        await Error(await firstProvider.PostAsJsonAsync(path + "/interest", new { note = "Puedo ayudarte" }), HttpStatusCode.Conflict, NexoCodes.InterestRequestAssigned);

        // Restaurar el estado abierto permite verificar el cursor con dos prestadores en la misma solicitud.
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery);
            await db.GetCollection<BsonDocument>("service_requests").UpdateOneAsync(new BsonDocument("_id", id.ToString("D")),
                new BsonDocument("$set", new BsonDocument { ["status"] = "open", ["data.status"] = "open" }));
        }
        Assert.Equal(HttpStatusCode.Created, (await firstProvider.PostAsJsonAsync(path + "/interest", new { note = "Uno" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await secondProvider.PostAsJsonAsync(path + "/interest", new { note = "Dos" })).StatusCode);
        var firstPage = await Json(await owner.GetAsync(path + "/interested?sort=recent&limit=1"));
        Assert.True(firstPage.GetProperty("hasMore").GetBoolean());
        var cursor = firstPage.GetProperty("nextCursor").GetString(); Assert.NotNull(cursor);
        var secondPage = await Json(await owner.GetAsync(path + "/interested?sort=recent&limit=1&cursor=" + cursor));
        Assert.False(secondPage.GetProperty("hasMore").GetBoolean());
        Assert.NotEqual(firstPage.GetProperty("items")[0].GetProperty("providerId").GetGuid(), secondPage.GetProperty("items")[0].GetProperty("providerId").GetGuid());
        await Error(await owner.GetAsync(path + "/interested?cursor=not-a-guid"), HttpStatusCode.BadRequest, NexoCodes.RequestCursorInvalid);
        Assert.Contains(firstPage.GetProperty("items").EnumerateArray().Concat(secondPage.GetProperty("items").EnumerateArray()),
            x => x.GetProperty("providerId").GetGuid() == firstId);
        Assert.Contains(firstPage.GetProperty("items").EnumerateArray().Concat(secondPage.GetProperty("items").EnumerateArray()),
            x => x.GetProperty("providerId").GetGuid() == secondId);
    }
}
