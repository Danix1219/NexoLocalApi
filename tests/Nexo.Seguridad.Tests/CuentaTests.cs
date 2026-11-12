using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;
using Nexo.Identity.Infrastructure.Messaging;
using Nexo.Identity.Infrastructure.Persistence;
using Nexo.Identity.Infrastructure.Safety;
using Nexo.Outbox;

namespace Nexo.Seguridad.Tests;

/// <summary>HTTP, OAuth, Mongo y Redis reales; solo la llamada cloud de moderacion es un doble.</summary>
[Collection("seguridad")]
[Trait("Category", "Seguridad")]
public sealed class CuentaTests(InfraDeSeguridad infra)
{
    [Fact]
    public async Task UbicacionSeGuardaSoloParaLaCuentaAutenticada()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var anonymous = await client.PutAsJsonAsync("/v1/discovery/me/location", new { lat = 20.05, lng = -99.34, radiusKm = 5, source = "gps" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var account = await Create(host, client);
        var invalid = await client.PutAsJsonAsync("/v1/discovery/me/location", new { lat = 80, lng = -99.34, radiusKm = 5, source = "gps" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var response = await client.PutAsJsonAsync("/v1/discovery/me/location", new { lat = 20.05, lng = -99.34, radiusKm = 5, source = "gps" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var database = host.Services.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery);
        var stored = await database.GetCollection<BsonDocument>("user_profiles")
            .Find(new BsonDocument("_id", new BsonBinaryData(Id(account), GuidRepresentation.Standard))).SingleAsync();
        Assert.Equal(5, stored["searchRadiusKm"].AsInt32);
        Assert.Equal(-99.34, stored["searchLocation"]["coordinates"][0].AsDouble);
        Assert.Equal(20.05, stored["searchLocation"]["coordinates"][1].AsDouble);
    }

    private sealed class Host(InfraDeSeguridad infra, bool runRelay = true, bool safetyConfigured = true) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:CodeHmacKey"] = Config.ClaveHmac,
                ["Mail:Host"] = "localhost",
                ["Mail:From"] = "pruebas@nexolocal.mx",
                ["Mongo:ConnectionString"] = infra.CadenaMongo,
                ["Redis:ConnectionString"] = infra.CadenaRedis,
                ["ContentSafety:Endpoint"] = safetyConfigured ? "https://moderacion.invalid" : "",
                ["ContentSafety:ApiKey"] = safetyConfigured ? Config.ClaveHmac : ""
            }));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, TestClientAddress>();
            services.AddHttpClient<IAccountProfileSafety, AccountProfileSafety>()
                .ConfigurePrimaryHttpMessageHandler(() => new ModerationHandler());
            if (!runRelay)
                foreach (var registration in services.Where(s => s.ServiceType == typeof(IHostedService) && s.ImplementationType == typeof(AccountEventRelay)).ToArray())
                    services.Remove(registration);
        });
    }

    private sealed class ModerationHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = await request.Content!.ReadAsStringAsync(ct);
            if (body.Contains("No Disponible", StringComparison.Ordinal)) return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            if (body.Contains("Respuesta Incompleta", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { categoriesAnalysis = Array.Empty<object>() }) };
            var severity = body.Contains("Bloqueado", StringComparison.Ordinal) ? 4 : 0;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    categoriesAnalysis = new[] { "Hate", "Sexual", "Violence", "SelfHarm" }
                    .Select(category => new { category, severity })
                })
            };
        }
    }

    private static string Email() => $"cuenta.{Guid.NewGuid():N}@nexolocal.mx";
    private static void Authenticate(HttpClient client, string token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private static Guid Id(Cuenta.Sesion session)
    {
        var part = session.AccessToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
        part = part.PadRight((part.Length + 3) / 4 * 4, '=');
        using var payload = JsonDocument.Parse(Convert.FromBase64String(part));
        return payload.RootElement.GetProperty("sub").GetGuid();
    }
    private static IdentityCollections Collections(Host host) => host.Services.GetRequiredService<IdentityCollections>();
    private static async Task<Cuenta.Sesion> Create(Host host, HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var session = await Cuenta.CrearAsync(host, client, Email());
        Authenticate(client, session.AccessToken);
        return session;
    }
    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(code, await Cuenta.Campo(response, "code"));
        Assert.False(string.IsNullOrWhiteSpace(await Cuenta.Campo(response, "traceId")));
    }

    [Theory]
    [InlineData("GET", "/me", null)]
    [InlineData("PATCH", "/me", "{}")]
    [InlineData("POST", "/me/mode", "{}")]
    [InlineData("POST", "/me/phone", "{}")]
    [InlineData("POST", "/devices", "{}")]
    [InlineData("DELETE", "/devices/019400c3-8d4f-7a12-b7e5-1f2a3b4c5d60", null)]
    [InlineData("POST", "/me/deletion", "{}")]
    [InlineData("DELETE", "/me/deletion", null)]
    public async Task Cada_operacion_exige_autenticacion(string method, string path, string? body)
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/v1/identity" + path);
        if (body is not null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Consulta_propia_proyecta_solo_datos_publicables_y_no_acepta_otro_id()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var account = await Create(host, client);
        var response = await client.GetAsync("/v1/identity/me?accountId=" + Guid.NewGuid());
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(Id(account), json.RootElement.GetProperty("accountId").GetGuid());
        Assert.Equal(account.Correo, json.RootElement.GetProperty("email").GetString());
        foreach (var forbidden in new[] { "passwordHash", "termsVersion", "pendingEvents", "revision", "authEpoch", "auditHash" })
            Assert.False(json.RootElement.TryGetProperty(forbidden, out _));
        Assert.True(response.Headers.CacheControl!.Private);
        Assert.True(response.Headers.CacheControl.NoStore);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    [InlineData("{\"name\":\"A\"}")]
    [InlineData("{\"email\":\"ajeno@example.com\"}")]
    [InlineData("{\"name\":\"Valido\",\"name\":\"Otro\"}")]
    [InlineData("{\"photoUploadId\":\"invalido\"}")]
    public async Task Edicion_rechaza_campos_invalidos_o_masivos(string body)
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var account = await Create(host, client);
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        await AssertError(await client.PatchAsync("/v1/identity/me", content), HttpStatusCode.BadRequest, "validation_failed");
        Assert.Equal("Persona De Prueba", (await Collections(host).Accounts.Find(a => a.Id == Id(account)).SingleAsync()).Name);
    }

    [Fact]
    public async Task Nombre_moderado_se_guarda_y_repetir_no_duplica_revision()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var account = await Create(host, client);
        var response = await client.PatchAsJsonAsync("/v1/identity/me", new { name = "Nuevo Nombre" });
        response.EnsureSuccessStatusCode();
        Assert.Equal("Nuevo Nombre", await Cuenta.Campo(response, "name"));
        var revision = (await Collections(host).Accounts.Find(a => a.Id == Id(account)).SingleAsync()).Revision;
        (await client.PatchAsJsonAsync("/v1/identity/me", new { name = "Nuevo Nombre" })).EnsureSuccessStatusCode();
        Assert.Equal(revision, (await Collections(host).Accounts.Find(a => a.Id == Id(account)).SingleAsync()).Revision);
    }

    [Theory]
    [InlineData("Nombre Bloqueado", HttpStatusCode.Forbidden, "content.inappropriate")]
    [InlineData("No Disponible", HttpStatusCode.ServiceUnavailable, "server.dependency_unavailable")]
    [InlineData("Respuesta Incompleta", HttpStatusCode.ServiceUnavailable, "server.dependency_unavailable")]
    public async Task Moderacion_fallida_o_rechazada_no_persiste(string name, HttpStatusCode status, string code)
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var account = await Create(host, client);
        await AssertError(await client.PatchAsJsonAsync("/v1/identity/me", new { name }), status, code);
        Assert.Equal("Persona De Prueba", (await Collections(host).Accounts.Find(a => a.Id == Id(account)).SingleAsync()).Name);
    }

    [Fact]
    public async Task Foto_exige_propiedad_aprobacion_y_finalidad_y_null_la_borra()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var owner = await Create(host, client);
        foreach (var (own, status, purpose) in new[] { (false, "approved", "avatar"), (true, "pending", "avatar"), (true, "approved", "work"), (true, "approved", "avatar") })
        {
            var upload = Guid.CreateVersion7();
            await Collections(host).Uploads.InsertOneAsync(new BsonDocument
            {
                ["_id"] = new BsonBinaryData(upload, GuidRepresentation.Standard),
                ["ownerId"] = new BsonBinaryData(own ? Id(owner) : Guid.NewGuid(), GuidRepresentation.Standard),
                ["status"] = status,
                ["purpose"] = purpose,
                ["url"] = "https://cdn.example.com/avatar.webp"
            });
            var response = await client.PatchAsJsonAsync("/v1/identity/me", new { photoUploadId = upload });
            if (!own || status != "approved" || purpose != "avatar")
                await AssertError(response, HttpStatusCode.NotFound, "upload.not_found");
            else
            {
                response.EnsureSuccessStatusCode();
                Assert.Equal("https://cdn.example.com/avatar.webp", await Cuenta.Campo(response, "photoUrl"));
            }
        }
        (await client.PatchAsJsonAsync("/v1/identity/me", new { photoUploadId = (string?)null })).EnsureSuccessStatusCode();
        Assert.Null((await Collections(host).Accounts.Find(a => a.Id == Id(owner)).SingleAsync()).PhotoUrl);
    }

    [Fact]
    public async Task Modo_exige_perfil_y_el_refresco_relee_la_cuenta()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var account = await Create(host, client);
        await AssertError(await client.PostAsJsonAsync("/v1/identity/me/mode", new { mode = "otro" }), HttpStatusCode.BadRequest, "validation_failed");
        var missing = await client.PostAsJsonAsync("/v1/identity/me/mode", new { mode = "provider" });
        await AssertError(missing, HttpStatusCode.Conflict, "provider.profile_required");
        Assert.Equal("createProvider", await Cuenta.Campo(missing, "nextStep"));
        await Collections(host).Accounts.UpdateOneAsync(a => a.Id == Id(account), Builders<Account>.Update.Set(a => a.HasProvider, true));
        var changed = await client.PostAsJsonAsync("/v1/identity/me/mode", new { mode = "provider" });
        changed.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await changed.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("tokenRefreshRequired").GetBoolean());
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
        client.DefaultRequestHeaders.Authorization = null;
        var refreshed = await Cuenta.RefrescarAsync(client, account.RefreshToken);
        refreshed.EnsureSuccessStatusCode();
        Authenticate(client, (await Cuenta.Campo(refreshed, "access_token"))!);
        var repeated = await client.PostAsJsonAsync("/v1/identity/me/mode", new { mode = "provider" });
        repeated.EnsureSuccessStatusCode();
        using var repeatedJson = JsonDocument.Parse(await repeated.Content.ReadAsStringAsync());
        Assert.False(repeatedJson.RootElement.GetProperty("tokenRefreshRequired").GetBoolean());
    }

    [Theory]
    [InlineData("+527715550100", true)]
    [InlineData("7715550100", true)]
    [InlineData("123", false)]
    [InlineData("+17715550100", false)]
    [InlineData("abcdefghij", false)]
    public async Task Movil_se_normaliza_como_declarado_sin_otp(string phone, bool valid)
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        await Create(host, client);
        var response = await client.PostAsJsonAsync("/v1/identity/me/phone", new { phone });
        if (!valid) { await AssertError(response, HttpStatusCode.BadRequest, "phone.invalid"); return; }
        response.EnsureSuccessStatusCode();
        Assert.Equal("+527715550100", await Cuenta.Campo(response, "phone"));
        Assert.Equal("declared", await Cuenta.Campo(response, "phoneVerificationStatus"));
        Assert.DoesNotContain("code", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dispositivo_repetido_no_duplica_y_borrado_ajeno_no_revela_existencia()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var owner = await Create(host, client);
        var token = "fcm-" + Guid.NewGuid().ToString("N");
        var registered = await client.PostAsJsonAsync("/v1/identity/devices", new { fcmToken = token, platform = "android", appVersion = "1.2.3" });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var deviceId = await Cuenta.Campo(registered, "deviceId");
        var again = await client.PostAsJsonAsync("/v1/identity/devices", new { fcmToken = token, platform = "android", appVersion = "1.2.4" });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(deviceId, await Cuenta.Campo(again, "deviceId"));
        Assert.DoesNotContain(token, await again.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        await Create(host, client);
        await AssertError(await client.DeleteAsync("/v1/identity/devices/" + deviceId), HttpStatusCode.NotFound, "resource.not_found");
        await AssertError(await client.DeleteAsync("/v1/identity/devices/" + Guid.NewGuid()), HttpStatusCode.NotFound, "resource.not_found");
        await AssertError(await client.PostAsJsonAsync("/v1/identity/devices", new { fcmToken = token, platform = "android", appVersion = "1.2.3" }), HttpStatusCode.Conflict, "device.token_owned_by_other_account");
        Authenticate(client, owner.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/v1/identity/devices/" + deviceId)).StatusCode);
        await AssertError(await client.DeleteAsync("/v1/identity/devices/" + deviceId), HttpStatusCode.NotFound, "resource.not_found");
    }

    [Theory]
    [InlineData("", "android", "1.0.0")]
    [InlineData("fcm", "ios", "1.0.0")]
    [InlineData("fcm", "android", "01.0.0")]
    [InlineData("fcm", "android", "1.0")]
    public async Task Dispositivo_invalido_responde_400(string token, string platform, string version)
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        await Create(host, client);
        await AssertError(await client.PostAsJsonAsync("/v1/identity/devices", new { fcmToken = token, platform, appVersion = version }), HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task Baja_cancelacion_y_refresco_no_reactivan_tokens_anteriores()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var account = await Create(host, client);
        await AssertError(await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = "Incorrecta9" }), HttpStatusCode.Unauthorized, "auth.invalid_credentials");
        var response = await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena, reason = "Prueba" });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await AssertError(await client.GetAsync("/v1/identity/me"), HttpStatusCode.Unauthorized, "auth.token_revoked");
        client.DefaultRequestHeaders.Authorization = null;
        var login = await Cuenta.IniciarSesionAsync(client, account.Correo, Config.Contrasena);
        login.EnsureSuccessStatusCode();
        var recovery = await Cuenta.Campo(login, "access_token");
        var refresh = await Cuenta.Campo(login, "refresh_token");
        Authenticate(client, recovery!);
        (await client.GetAsync("/v1/identity/me")).EnsureSuccessStatusCode();
        await AssertError(await client.PostAsJsonAsync("/v1/identity/me/phone", new { phone = "7715550100" }), HttpStatusCode.Forbidden, "account.pending_deletion");
        await AssertError(await client.PostAsJsonAsync("/v1/identity/email/verification", new { email = Email(), purpose = "changeEmail" }), HttpStatusCode.Forbidden, "account.pending_deletion");
        var cancelled = await client.DeleteAsync("/v1/identity/me/deletion");
        cancelled.EnsureSuccessStatusCode();
        Assert.Equal("queued", await Cuenta.Campo(cancelled, "restorationStatus"));
        await AssertError(await client.PatchAsJsonAsync("/v1/identity/me", new { name = "Otro Nombre" }), HttpStatusCode.Forbidden, "account.pending_deletion");
        client.DefaultRequestHeaders.Authorization = null;
        var renewed = await Cuenta.RefrescarAsync(client, refresh!);
        renewed.EnsureSuccessStatusCode();
        Authenticate(client, (await Cuenta.Campo(renewed, "access_token"))!);
        (await client.PostAsJsonAsync("/v1/identity/me/phone", new { phone = "7715550100" })).EnsureSuccessStatusCode();
        Authenticate(client, account.AccessToken);
        await AssertError(await client.GetAsync("/v1/identity/me"), HttpStatusCode.Unauthorized, "auth.token_revoked");
    }

    [Fact]
    public async Task Cancelar_sin_baja_es_409_y_ventana_vencida_es_410()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var account = await Create(host, client);
        await AssertError(await client.DeleteAsync("/v1/identity/me/deletion"), HttpStatusCode.Conflict, "account.not_pending_deletion");
        await Collections(host).Accounts.UpdateOneAsync(a => a.Id == Id(account), Builders<Account>.Update
            .Set(a => a.Status, AccountStatus.PendingDeletion).Set(a => a.DeletionRequestedAt, DateTime.UtcNow.AddDays(-31)));
        await AssertError(await client.DeleteAsync("/v1/identity/me/deletion"), HttpStatusCode.Gone, "account.deletion_already_executed");
        Assert.Equal(AccountStatus.PendingDeletion, (await Collections(host).Accounts.Find(a => a.Id == Id(account)).SingleAsync()).Status);
    }

    [Fact]
    public async Task Cambio_y_evento_son_atomicos_y_el_relay_es_idempotente()
    {
        using var host = new Host(infra, runRelay: false);
        using var client = host.CreateClient();
        var session = await Create(host, client);
        using var scope = host.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAccountManagementStore>();
        var snapshot = (await store.ReadAsync(Id(session), default))!;
        Assert.True(await store.ApplyAsync(snapshot, new AccountWrite("account.phone_updated", Phone: "+527715550100"), DateTime.UtcNow, default));
        Assert.False(await store.ApplyAsync(snapshot, new AccountWrite("account.phone_updated", Phone: "+527715550101"), DateTime.UtcNow, default));
        var saved = await Collections(host).Accounts.Find(a => a.Id == Id(session)).SingleAsync();
        Assert.Equal("+527715550100", saved.Phone);
        Assert.Equal(2, saved.PendingEvents.Count);
        var eventId = saved.PendingEvents[0].Id;
        var outbox = host.Services.GetRequiredService<IOutboxRepository>();
        // Simula entrega ya confirmada en Mongo pero todavia no retirada del documento de cuenta.
        await outbox.EnqueueAsync(new OutboxMessage { MessageId = eventId, Type = saved.PendingEvents[0].Type, Payload = saved.PendingEvents[0].Payload, OccurredAt = DateTimeOffset.UtcNow });
        var relay = new AccountEventRelay(Collections(host), outbox, NullLogger<AccountEventRelay>.Instance);
        await relay.RelayOnceAsync(default);
        await relay.RelayOnceAsync(default);
        Assert.Empty((await Collections(host).Accounts.Find(a => a.Id == Id(session)).SingleAsync()).PendingEvents);
        var database = host.Services.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Identity);
        Assert.Equal(1, await database.GetCollection<BsonDocument>("outbox").CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("_id", eventId)));
    }

    [Fact]
    public async Task Sin_configuracion_cloud_no_se_aprueba_el_nombre()
    {
        using var host = new Host(infra, safetyConfigured: false);
        using var client = host.CreateClient();
        var session = await Create(host, client);
        await AssertError(await client.PatchAsJsonAsync("/v1/identity/me", new { name = "Nombre Nuevo" }), HttpStatusCode.ServiceUnavailable, "server.dependency_unavailable");
        Assert.Equal("Persona De Prueba", (await Collections(host).Accounts.Find(a => a.Id == Id(session)).SingleAsync()).Name);
    }

    [Fact]
    public async Task Dispositivo_inactivo_se_reasigna_conservando_un_solo_registro()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var first = await Create(host, client);
        var token = "fcm-" + Guid.NewGuid().ToString("N");
        var created = await client.PostAsJsonAsync("/v1/identity/devices", new { fcmToken = token, platform = "android", appVersion = "1.0.0" });
        created.EnsureSuccessStatusCode();
        var deviceId = await Cuenta.Campo(created, "deviceId");
        await Collections(host).Devices.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("fcmToken", token),
            Builders<BsonDocument>.Update.Set("lastSeenAt", DateTime.UtcNow.AddMinutes(-2)));
        var second = await Create(host, client);
        var changed = await client.PostAsJsonAsync("/v1/identity/devices", new { fcmToken = token, platform = "android", appVersion = "1.0.1" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(deviceId, await Cuenta.Campo(changed, "deviceId"));
        var saved = await Collections(host).Devices.Find(Builders<BsonDocument>.Filter.Eq("fcmToken", token)).ToListAsync();
        Assert.Single(saved);
        Assert.Equal(Id(second), saved[0]["accountId"].AsBsonBinaryData.ToGuid());
        Authenticate(client, first.AccessToken);
        await AssertError(await client.DeleteAsync("/v1/identity/devices/" + deviceId), HttpStatusCode.NotFound, "resource.not_found");
        var indexes = await (await Collections(host).Devices.Indexes.ListAsync()).ToListAsync();
        Assert.Contains(indexes, i => i["name"] == "ttl_devices_last_seen" && i["expireAfterSeconds"].ToInt64() == 90L * 86400);
    }

    [Fact]
    public async Task Solicitud_repetida_no_adelanta_la_baja_y_el_motivo_tiene_limite()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var session = await Create(host, client);
        await AssertError(await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena, reason = new string('a', 301) }), HttpStatusCode.BadRequest, "validation_failed");
        var first = await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena });
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        using var json = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var effectiveAt = json.RootElement.GetProperty("effectiveAt").GetDateTime();
        client.DefaultRequestHeaders.Authorization = null;
        var login = await Cuenta.IniciarSesionAsync(client, session.Correo, Config.Contrasena);
        login.EnsureSuccessStatusCode();
        Authenticate(client, (await Cuenta.Campo(login, "access_token"))!);
        var repeat = await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena });
        await AssertError(repeat, HttpStatusCode.Conflict, "account.already_pending_deletion");
        using var again = JsonDocument.Parse(await repeat.Content.ReadAsStringAsync());
        Assert.Equal(effectiveAt, again.RootElement.GetProperty("effectiveAt").GetDateTime());
    }

    [Fact]
    public async Task Limite_user_es_por_cuenta_y_no_por_IP_y_devuelve_problem_details()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        await Create(host, client);
        for (var i = 0; i < 120; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/identity/me")).StatusCode);
        await AssertError(await client.GetAsync("/v1/identity/me"), HttpStatusCode.TooManyRequests, "rate.limit_exceeded");
        await Create(host, client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/identity/me")).StatusCode);
    }

    private sealed class NoticeRecorder : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = [];
        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Refresh_de_epoca_anterior_no_puede_recuperar_acceso_aunque_siga_persistido()
    {
        using var host = new Host(infra);
        using var client = host.CreateClient();
        var session = await Create(host, client);
        await Collections(host).Accounts.UpdateOneAsync(a => a.Id == Id(session), Builders<Account>.Update.Inc(a => a.AuthEpoch, 1));
        client.DefaultRequestHeaders.Authorization = null;
        var refreshed = await Cuenta.RefrescarAsync(client, session.RefreshToken);
        Assert.False(refreshed.IsSuccessStatusCode);
        Assert.Equal("nexo:auth.token_revoked", await Cuenta.Campo(refreshed, "error_uri"));
    }

    [Fact]
    public async Task Aviso_de_baja_y_auditoria_salen_del_evento_sin_enviar_correo_real()
    {
        using var host = new Host(infra, runRelay: false);
        using var client = host.CreateClient();
        var session = await Create(host, client);
        Assert.Equal(HttpStatusCode.Accepted,
            (await client.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena })).StatusCode);
        var saved = await Collections(host).Accounts.Find(a => a.Id == Id(session)).SingleAsync();
        var recorder = new NoticeRecorder();
        var handler = new IdentityOutboxHandler(recorder, NullLogger<IdentityOutboxHandler>.Instance, Collections(host));
        var notice = saved.PendingEvents.Single(e => e.Type == "identity.account_notice");
        await handler.HandleAsync(new OutboxMessage { MessageId = notice.Id, Type = notice.Type, Payload = notice.Payload });
        Assert.Single(recorder.Sent);
        Assert.Equal(session.Correo, recorder.Sent[0].To);
        Assert.DoesNotContain(Config.Contrasena, recorder.Sent[0].Body, StringComparison.Ordinal);
        var audit = saved.PendingEvents.Single(e => e.Type == "identity.audit");
        var message = new OutboxMessage { MessageId = audit.Id, Type = audit.Type, Payload = audit.Payload };
        await handler.HandleAsync(message);
        await handler.HandleAsync(message);
        var records = await Collections(host).AuditEvents.Find(Builders<BsonDocument>.Filter.Eq("_id", audit.Id)).ToListAsync();
        Assert.Single(records);
        Assert.Equal(saved.AuditHash, records[0]["hash"].AsString);
    }
}

/// <summary>Cada host de prueba representa una direccion distinta, sin desactivar limites reales.</summary>
internal sealed class TestClientAddress : IStartupFilter
{
    private static int sequence;
    private readonly IPAddress address = IPAddress.Parse($"2001:db8::{Interlocked.Increment(ref sequence):x}");

    public Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next) => app =>
    {
        Microsoft.AspNetCore.Builder.UseExtensions.Use(app, async (context, continuation) =>
        {
            context.Connection.RemoteIpAddress = address;
            await continuation();
        });
        next(app);
    };
}
