using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Accounts;
using Nexo.Common.Mongo;
using Nexo.Common.RateLimiting;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Engagement.Application.Conversations;
using Nexo.Engagement.Application.Orders;
using Nexo.Identity.Contracts;
using Nexo.Identity.Infrastructure.Messaging;
using Nexo.Identity.Infrastructure.Persistence;
using Nexo.Outbox;
using StackExchange.Redis;

namespace Nexo.Seguridad.Tests;

/// <summary>Baja de cuenta de punta a punta con MongoDB y Redis reales (nv-12, FR-DEL-001…006).</summary>
[Collection("seguridad")]
[Trait("Category", "Seguridad")]
public sealed class AccountDeletionTests(InfraDeSeguridad infra)
{
    private sealed class Safe : IOrderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(Result<bool>.Success(true));
    }

    private sealed class Cipher : IOrderAddressProtection
    {
        public Task<string> EncryptAsync(OrderAddress value, CancellationToken ct) => Task.FromResult(Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value)));
        public Task<OrderAddress> DecryptAsync(string cipher, CancellationToken ct) => Task.FromResult(JsonSerializer.Deserialize<OrderAddress>(Convert.FromBase64String(cipher))!);
    }

    /// <summary>Almacén de adjuntos en memoria: la prueba no depende de Azure.</summary>
    private sealed class Attachments : IPrivateChatAttachments
    {
        public static readonly ConcurrentDictionary<(Guid, Guid), byte[]> Stored = new();
        public Task PutAsync(Guid conversation, Guid attachment, byte[] ciphertext, CancellationToken ct)
        {
            Stored[(conversation, attachment)] = ciphertext;
            return Task.CompletedTask;
        }
        public Task<byte[]?> GetAsync(Guid conversation, Guid attachment, CancellationToken ct) =>
            Task.FromResult(Stored.GetValueOrDefault((conversation, attachment)));
        public Task DeleteAsync(Guid conversation, Guid attachment, CancellationToken ct)
        {
            Stored.TryRemove((conversation, attachment), out _);
            return Task.CompletedTask;
        }
    }

    private sealed class Mail : IEmailSender
    {
        public List<(string To, string Subject)> Sent { get; } = [];
        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            Sent.Add((to, subject));
            return Task.CompletedTask;
        }
    }

    /// <summary>Dominio que falla la primera vez al borrar, para comprobar que un borrado parcial no se da por hecho.</summary>
    private sealed class Flaky : IAccountDataLifecycle
    {
        public static int Failures;
        public string Domain => "flaky";
        public Task SuspendAsync(AccountSubject subject, DateTime now, CancellationToken ct) => Task.CompletedTask;
        public Task RestoreAsync(AccountSubject subject, DateTime now, CancellationToken ct) => Task.CompletedTask;
        public Task EraseAsync(AccountSubject subject, DateTime now, CancellationToken ct) =>
            Interlocked.Decrement(ref Failures) >= 0 ? throw new IOException("storage down") : Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Host(bool flaky = false) =>
        new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis).WithWebHostBuilder(b =>
        {
            // La prueba decide cuándo corre el ciclo.
            b.UseSetting("Accounts:Deletion:DisableWorker", "true");
            b.UseSetting("Chat:E2eeEnabled", "true");
            b.ConfigureServices(s =>
            {
                s.RemoveAll<IOrderSafety>(); s.AddSingleton<IOrderSafety, Safe>();
                s.RemoveAll<IOrderAddressProtection>(); s.AddSingleton<IOrderAddressProtection, Cipher>();
                s.RemoveAll<IPrivateChatAttachments>(); s.AddSingleton<IPrivateChatAttachments, Attachments>();
                if (flaky) s.AddScoped<IAccountDataLifecycle, Flaky>();
            });
        });

    private sealed record Person(Guid Id, string Email, HttpClient Http);

    private static async Task<Person> Join(WebApplicationFactory<Program> host)
    {
        var http = host.CreateClient();
        var email = $"baja-{Guid.NewGuid():N}@example.test";
        var session = await Cuenta.CrearAsync(host, http, email);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var me = await Read(await http.GetAsync("/v1/identity/me"));
        return new(me.GetProperty("accountId").GetGuid(), email, http);
    }

    private static async Task<JsonElement> Read(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode} {response.RequestMessage?.RequestUri} {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static Task<HttpResponseMessage> Post(HttpClient http, string url, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return http.SendAsync(request);
    }

    private static async Task Run(WebApplicationFactory<Program> host)
    {
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AccountLifecycleCoordinator>().RunOnceAsync(default);
    }

    private static async Task ForgetCodeWindows(WebApplicationFactory<Program> host, string email)
    {
        var redis = host.Services.GetRequiredService<IConnectionMultiplexer>();
        var hash = NexoRateLimits.HashEmail(email);
        foreach (var server in redis.GetServers())
            await foreach (var key in server.KeysAsync(pattern: $"*{hash}*")) await redis.GetDatabase().KeyDeleteAsync(key);
    }

    private static BsonBinaryData Bin(Guid id) => new(id, GuidRepresentation.Standard);

    private static IMongoCollection<BsonDocument> Data(WebApplicationFactory<Program> host, string database, string collection) =>
        host.Services.GetRequiredKeyedService<IMongoDatabase>(database).GetCollection<BsonDocument>(collection);

    private static async Task<BsonDocument?> One(WebApplicationFactory<Program> host, string database, string collection, BsonDocument filter) =>
        await Data(host, database, collection).Find(filter).FirstOrDefaultAsync();

    private sealed record Scenario(Person Leaving, Person Other, Guid ProviderId, Guid ConversationId, Guid PendingOrder,
        Guid ResolvedOrder, string Work, string Request);

    /// <summary>Un prestador con perfil, trabajo, solicitud, conversación, un pedido pendiente y otro resuelto.</summary>
    private static async Task<Scenario> Arrange(WebApplicationFactory<Program> host)
    {
        var leaving = await Join(host);
        var other = await Join(host);
        var providerId = Guid.CreateVersion7();
        using (var scope = host.Services.CreateScope())
        {
            Assert.True(await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(leaving.Id, new()
            {
                ProviderId = providerId,
                Name = "Taller que se va",
                Bio = "Reparaciones del hogar en el centro",
                Categories = ["plomeria"],
                Location = new(20.05, -99.34),
                PublicZone = new("Centro", new(20.05, -99.34)),
                Attention = "both",
                RadiusKm = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, 0, Guid.NewGuid().ToString(), "hash", true), default));
        }

        var work = Guid.CreateVersion7().ToString("D");
        await Data(host, NexoDatabaseKeys.Discovery, "works").InsertOneAsync(new BsonDocument
        {
            ["_id"] = work,
            ["ownerId"] = Bin(leaving.Id),
            ["providerId"] = Bin(providerId),
            ["moderation"] = new BsonDocument("status", "approved")
        });
        var request = Guid.CreateVersion7().ToString("D");
        await Data(host, NexoDatabaseKeys.Discovery, "service_requests").InsertOneAsync(new BsonDocument
        {
            ["_id"] = request,
            ["clientId"] = Bin(leaving.Id),
            ["status"] = "open",
            ["data"] = new BsonDocument("status", "open")
        });

        var conversationId = (await Read(await Post(other.Http, "/v1/engagement/conversations", new { providerId }))).GetProperty("conversationId").GetGuid();
        var messages = $"/v1/engagement/conversations/{conversationId}/messages";
        await Read(await Post(other.Http, messages, new { kind = "text", text = "Mensaje de quien se queda" }));
        await Read(await Post(leaving.Http, messages, new { kind = "text", text = "Mi telefono es 771 000 0000" }));

        async Task<Guid> Order() => (await Read(await Post(other.Http, "/v1/engagement/orders", new
        {
            providerId,
            origin = "direct",
            description = "Necesito reparar una fuga de agua en casa.",
            attention = "home",
            address = "Calle Hidalgo 123",
            location = new { lat = 20.0539, lng = -99.3403 }
        }))).GetProperty("orderId").GetGuid();
        var resolved = await Order();
        await Read(await leaving.Http.PostAsJsonAsync($"/v1/engagement/orders/{resolved}/accept", new { agreedPrice = new { amountCents = 25000 } }));
        // El cierre del pedido se fija directamente: aquí solo importa que exista uno resuelto.
        await Data(host, NexoDatabaseKeys.Engagement, "orders").UpdateOneAsync(new BsonDocument("_id", Bin(resolved)),
            new BsonDocument("$set", new BsonDocument { ["status"] = "resolved", ["resolvedAt"] = DateTime.UtcNow }));
        var pending = await Order();
        return new(leaving, other, providerId, conversationId, pending, resolved, work, request);
    }

    private static Task<HttpResponseMessage> Upload(Person person, Guid conversation, Guid attachment)
    {
        var content = new ByteArrayContent(new byte[64]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return person.Http.PutAsync($"/v1/engagement/chat-keys/conversations/{conversation}/attachments/{attachment}", content);
    }

    /// <summary>El aviso final sale una vez y el mensaje guardado pierde la dirección.</summary>
    private static async Task AssertFinalNotice(WebApplicationFactory<Program> host, string email, Guid account)
    {
        var filter = new BsonDocument
        {
            ["Type"] = AccountDeletedNotice.Type,
            ["Payload"] = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(email))
        };
        BsonDocument? stored = null;
        for (var attempt = 0; attempt < 40 && stored is null; attempt++)
        {
            stored = await One(host, NexoDatabaseKeys.Identity, "outbox", filter);
            if (stored is null) await Task.Delay(250);
        }
        Assert.NotNull(stored);

        var id = stored["_id"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard);
        var message = new OutboxMessage
        {
            MessageId = id,
            Type = AccountDeletedNotice.Type,
            Payload = stored["Payload"].AsString,
            OccurredAt = DateTimeOffset.UtcNow
        };
        var mail = new Mail();
        var handler = new IdentityOutboxHandler(mail, NullLogger<IdentityOutboxHandler>.Instance,
            host.Services.GetRequiredService<IdentityCollections>());
        await handler.HandleAsync(message);
        Assert.Equal((email, AccountDeletedNotice.Subject), Assert.Single(mail.Sent));

        var after = (await One(host, NexoDatabaseKeys.Identity, "outbox", new BsonDocument("_id", Bin(id))))!;
        Assert.Equal(AccountDeletedNotice.Erased, after["Payload"].AsString);
        Assert.DoesNotContain(account.ToString("D"), after["Payload"].AsString);

        // Una entrega repetida no vuelve a escribir a nadie.
        message.Payload = after["Payload"].AsString;
        await handler.HandleAsync(message);
        Assert.Single(mail.Sent);

        // Vencido el plazo de envío se descarta sin mandar el correo.
        message.Payload = JsonSerializer.Serialize(new AccountDeletedNotice(email));
        message.OccurredAt = DateTimeOffset.UtcNow - AccountDeletedNotice.Lifetime - TimeSpan.FromMinutes(1);
        await handler.HandleAsync(message);
        Assert.Single(mail.Sent);
    }

    private static Task<HttpResponseMessage> RequestDeletion(Person person) =>
        person.Http.PostAsJsonAsync("/v1/identity/me/deletion", new { password = Config.Contrasena, reason = "Prueba" });

    private static async Task<string> OrderStatus(WebApplicationFactory<Program> host, Guid id) =>
        (await One(host, NexoDatabaseKeys.Engagement, "orders", new BsonDocument("_id", Bin(id))))!["status"].AsString;

    private static Task Expire(WebApplicationFactory<Program> host, Guid account) =>
        Data(host, NexoDatabaseKeys.Identity, "accounts").UpdateOneAsync(new BsonDocument("_id", Bin(account)),
            new BsonDocument("$set", new BsonDocument("deletionRequestedAt", DateTime.UtcNow.AddDays(-31))));

    [Fact]
    public async Task Pedir_la_baja_oculta_y_detiene_y_cancelarla_restaura()
    {
        using var host = Host();
        var s = await Arrange(host);
        var publicProfile = $"/v1/discovery/providers/{s.ProviderId}";
        Assert.Equal(HttpStatusCode.OK, (await s.Other.Http.GetAsync(publicProfile)).StatusCode);

        var wrong = await s.Leaving.Http.PostAsJsonAsync("/v1/identity/me/deletion", new { password = "Incorrecta123" });
        Assert.False(wrong.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await RequestDeletion(s.Leaving)).StatusCode);
        // La sesión anterior queda invalidada de inmediato.
        Assert.Equal(HttpStatusCode.Unauthorized, (await s.Leaving.Http.GetAsync("/v1/identity/me")).StatusCode);
        await Run(host);

        Assert.Equal(HttpStatusCode.NotFound, (await s.Other.Http.GetAsync(publicProfile)).StatusCode);
        var work = new BsonDocument("_id", s.Work);
        var request = new BsonDocument("_id", s.Request);
        Assert.Equal("suspended", (await One(host, NexoDatabaseKeys.Discovery, "works", work))!["moderation"]["status"].AsString);
        Assert.Equal("closed", (await One(host, NexoDatabaseKeys.Discovery, "service_requests", request))!["status"].AsString);
        Assert.Equal("cancelled", await OrderStatus(host, s.PendingOrder));
        Assert.Equal("resolved", await OrderStatus(host, s.ResolvedOrder));
        var messages = $"/v1/engagement/conversations/{s.ConversationId}/messages";
        Assert.False((await Post(s.Other.Http, messages, new { kind = "text", text = "¿Sigues ahí?" })).IsSuccessStatusCode);

        // Entra en modo de recuperación: solo puede consultar su cuenta y cancelar.
        // Como la app: cierra la sesión anterior antes de volver a entrar.
        s.Leaving.Http.DefaultRequestHeaders.Authorization = null;
        var login = await Cuenta.IniciarSesionAsync(s.Leaving.Http, s.Leaving.Email, Config.Contrasena);
        var token = (await Read(login)).GetProperty("access_token").GetString();
        s.Leaving.Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal("pendingDeletion", (await Read(await s.Leaving.Http.GetAsync("/v1/identity/me"))).GetProperty("status").GetString());
        Assert.False((await s.Leaving.Http.GetAsync("/v1/engagement/orders")).IsSuccessStatusCode);
        await Read(await s.Leaving.Http.DeleteAsync("/v1/identity/me/deletion"));
        await Run(host);

        Assert.Equal(HttpStatusCode.OK, (await s.Other.Http.GetAsync(publicProfile)).StatusCode);
        Assert.Equal("approved", (await One(host, NexoDatabaseKeys.Discovery, "works", work))!["moderation"]["status"].AsString);
        Assert.True((await Post(s.Other.Http, messages, new { kind = "text", text = "Qué bueno que volviste" })).IsSuccessStatusCode);
        // Lo que el contrato declara irreversible no regresa.
        Assert.Equal("closed", (await One(host, NexoDatabaseKeys.Discovery, "service_requests", request))!["status"].AsString);
        Assert.Equal("cancelled", await OrderStatus(host, s.PendingOrder));
    }

    [Fact]
    public async Task A_los_30_dias_borra_en_todos_los_dominios_y_libera_el_correo()
    {
        using var host = Host();
        var s = await Arrange(host);
        // Cada persona sube un adjunto cifrado a la conversación.
        await Data(host, NexoDatabaseKeys.Engagement, "conversations").UpdateOneAsync(
            new BsonDocument("_id", Bin(s.ConversationId)), new BsonDocument("$set", new BsonDocument("encrypted", true)));
        var mineFile = Guid.CreateVersion7();
        var otherFile = Guid.CreateVersion7();
        Assert.Equal(HttpStatusCode.NoContent, (await Upload(s.Leaving, s.ConversationId, mineFile)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Upload(s.Other, s.ConversationId, otherFile)).StatusCode);
        // Nadie puede quedarse con el identificador de un archivo ajeno.
        Assert.Equal(HttpStatusCode.Conflict, (await Upload(s.Other, s.ConversationId, mineFile)).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await RequestDeletion(s.Leaving)).StatusCode);
        await Run(host);

        // Antes de vencer el plazo no se borra nada.
        await Run(host);
        var accountFilter = new BsonDocument("_id", Bin(s.Leaving.Id));
        Assert.Equal(s.Leaving.Email, (await One(host, NexoDatabaseKeys.Identity, "accounts", accountFilter))!["email"].AsString);
        Assert.True(Attachments.Stored.ContainsKey((s.ConversationId, mineFile)));

        await Expire(host, s.Leaving.Id);
        await Run(host);

        var account = (await One(host, NexoDatabaseKeys.Identity, "accounts", accountFilter))!;
        Assert.Equal("deleted", account["status"].AsString);
        Assert.Equal("Usuario eliminado", account["name"].AsString);
        Assert.EndsWith("@nexolocal.invalid", account["email"].AsString);
        Assert.Equal("", account["passwordHash"].AsString);
        // La dirección solo puede quedar en el aviso final mientras espera a pasar a la cola.
        var withoutNotice = account.DeepClone().AsBsonDocument;
        withoutNotice.Remove("pendingEvents");
        Assert.DoesNotContain(s.Leaving.Email, withoutNotice.ToJson());
        Assert.All(account.GetValue("pendingEvents", new BsonArray()).AsBsonArray
            .Where(e => e.ToJson().Contains(s.Leaving.Email)), e => Assert.Contains(AccountDeletedNotice.Type, e.ToJson()));
        Assert.False((await Cuenta.IniciarSesionAsync(s.Leaving.Http, s.Leaving.Email, Config.Contrasena)).IsSuccessStatusCode);
        // Salvo el aviso final sin enviar, ningún mensaje interno conserva el correo ni datos de la cuenta.
        var queue = await Data(host, NexoDatabaseKeys.Identity, "outbox").Find(new BsonDocument()).ToListAsync();
        Assert.NotEmpty(queue);
        Assert.DoesNotContain(queue, m => m["Type"].AsString != AccountDeletedNotice.Type &&
            m.ToJson().Contains(s.Leaving.Email, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(queue, m => m["Type"].AsString != "identity.audit" && m["Payload"].AsString.Contains(s.Leaving.Id.ToString("D")));

        Assert.Equal(HttpStatusCode.NotFound, (await s.Other.Http.GetAsync($"/v1/discovery/providers/{s.ProviderId}")).StatusCode);
        Assert.Null(await One(host, NexoDatabaseKeys.Discovery, "works", new BsonDocument("_id", s.Work)));
        Assert.Null(await One(host, NexoDatabaseKeys.Discovery, "service_requests", new BsonDocument("_id", s.Request)));
        var shell = (await One(host, NexoDatabaseKeys.Discovery, "provider_profiles", new BsonDocument("accountId", Bin(s.Leaving.Id))))!;
        Assert.Equal("Usuario eliminado", shell["data"]["name"].AsString);
        Assert.False(shell.Contains("locationGeo"));
        Assert.DoesNotContain("Taller que se va", shell.ToJson());

        Assert.Null(await One(host, NexoDatabaseKeys.Engagement, "orders", new BsonDocument("_id", Bin(s.PendingOrder))));
        var kept = (await One(host, NexoDatabaseKeys.Engagement, "orders", new BsonDocument("_id", Bin(s.ResolvedOrder))))!;
        Assert.Equal("resolved", kept["status"].AsString);
        Assert.Equal("", kept["description"].AsString);
        Assert.True(kept["addressCipher"].IsBsonNull);

        var thread = await Data(host, NexoDatabaseKeys.Engagement, "messages")
            .Find(new BsonDocument("conversationId", Bin(s.ConversationId))).ToListAsync();
        Assert.All(thread.Where(m => m["senderId"] == Bin(s.Leaving.Id) && m["kind"] != "system"), m =>
        {
            Assert.Equal("deleted", m["kind"].AsString);
            Assert.True(m["text"].IsBsonNull);
        });
        Assert.Contains(thread, m => m["senderId"] == Bin(s.Other.Id) && m.GetValue("text", BsonNull.Value) == "Mensaje de quien se queda");

        Assert.False(Attachments.Stored.ContainsKey((s.ConversationId, mineFile)));
        Assert.True(Attachments.Stored.ContainsKey((s.ConversationId, otherFile)));
        var owned = await Data(host, NexoDatabaseKeys.Engagement, "chat_attachments")
            .Find(new BsonDocument("conversationId", Bin(s.ConversationId))).ToListAsync();
        Assert.Equal(Bin(s.Other.Id), Assert.Single(owned)["uploaderId"]);

        // Donde aún aparezca como autora, el resto ve una cuenta sin nombre ni foto.
        using (var scope = host.Services.CreateScope())
        {
            var identity = (await scope.ServiceProvider.GetRequiredService<IRequestClientContract>()
                .ReadAsync([s.Leaving.Id], default))[s.Leaving.Id];
            Assert.False(identity.Active);
            Assert.Equal("Cuenta no disponible", identity.DisplayName);
            Assert.Null(identity.PhotoUrl);
        }

        await AssertFinalNotice(host, s.Leaving.Email, s.Leaving.Id);
        BsonDocument relayed = new();
        for (var attempt = 0; attempt < 40; attempt++)
        {
            relayed = (await One(host, NexoDatabaseKeys.Identity, "accounts", accountFilter))!;
            if (!relayed.ToJson().Contains(s.Leaving.Email)) break;
            await Task.Delay(250);
        }
        Assert.DoesNotContain(s.Leaving.Email, relayed.ToJson());

        // Las pantallas de la otra persona siguen respondiendo después del borrado.
        await Read(await s.Other.Http.GetAsync("/v1/engagement/orders?role=client&status=all"));
        await Read(await s.Other.Http.GetAsync($"/v1/engagement/orders/{s.ResolvedOrder}"));
        await Read(await s.Other.Http.GetAsync("/v1/engagement/conversations?role=client"));
        await Read(await s.Other.Http.GetAsync($"/v1/engagement/conversations/{s.ConversationId}/messages"));

        // Repetir el ciclo no cambia nada y el correo puede registrarse de nuevo.
        await Run(host);
        // En la vida real pasan 30 días: aquí se limpia la espera de reenvío del alta original.
        await ForgetCodeWindows(host, s.Leaving.Email);
        using var again = host.CreateClient();
        _ = await Cuenta.CrearAsync(host, again, s.Leaving.Email);
    }

    [Fact]
    public async Task Un_borrado_a_medias_no_se_da_por_terminado_y_se_reintenta()
    {
        Flaky.Failures = 1;
        using var host = Host(flaky: true);
        var person = await Join(host);
        Assert.Equal(HttpStatusCode.Accepted, (await RequestDeletion(person)).StatusCode);
        await Expire(host, person.Id);
        await Run(host);

        var filter = new BsonDocument("_id", Bin(person.Id));
        var partial = (await One(host, NexoDatabaseKeys.Identity, "accounts", filter))!;
        Assert.Equal("pendingDeletion", partial["status"].AsString);
        Assert.DoesNotContain("flaky", partial["erasedDomains"].AsBsonArray.Select(v => v.AsString));

        // El lease vence y otro ciclo termina lo que faltaba.
        await Data(host, NexoDatabaseKeys.Identity, "accounts").UpdateOneAsync(filter,
            new BsonDocument("$set", new BsonDocument("lifecycleLeaseUntil", DateTime.UtcNow.AddMinutes(-1))));
        await Run(host);
        Assert.Equal("deleted", (await One(host, NexoDatabaseKeys.Identity, "accounts", filter))!["status"].AsString);
    }
}
