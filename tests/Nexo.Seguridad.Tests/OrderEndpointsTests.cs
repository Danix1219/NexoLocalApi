using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Common.Mongo;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Seguridad.Tests;

[Collection("seguridad")]
public sealed class OrderEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class Safe : IOrderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(Result<bool>.Success(true));
    }
    private sealed class TestAddressProtection : IOrderAddressProtection
    {
        public Task<string> EncryptAsync(OrderAddress value, CancellationToken ct) => Task.FromResult(Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value)));
        public Task<OrderAddress> DecryptAsync(string cipher, CancellationToken ct) => Task.FromResult(JsonSerializer.Deserialize<OrderAddress>(Convert.FromBase64String(cipher))!);
    }
    private WebApplicationFactory<Program> Host()
    {
        var factory = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
        return factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IOrderSafety>(); s.AddSingleton<IOrderSafety, Safe>();
            s.RemoveAll<IOrderAddressProtection>(); s.AddSingleton<IOrderAddressProtection, TestAddressProtection>();
        }));
    }
    private static async Task<Guid> Login(WebApplicationFactory<Program> host, HttpClient client)
    {
        var session = await Cuenta.CrearAsync(host, client, $"orders-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var json = JsonDocument.Parse(await (await client.GetAsync("/v1/identity/me")).Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accountId").GetGuid();
    }
    private static async Task<Guid> Provider(WebApplicationFactory<Program> host, Guid owner)
    {
        using var scope = host.Services.CreateScope(); var profileId = Guid.CreateVersion7();
        var saved = await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(owner, new()
        {
            ProviderId = profileId, Name = "Proveedor para pedidos", Categories = ["plomeria"], Location = new(20.05, -99.34),
            PublicZone = new("Centro", new(20.05, -99.34)), Attention = "both", RadiusKm = 20,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        }, 0, Guid.NewGuid().ToString(), "hash", true), default);
        Assert.True(saved); return profileId;
    }
    private static async Task<JsonElement> Read(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string code)
    { Assert.Equal(status, response.StatusCode); Assert.Equal(code, (await Read(response)).GetProperty("code").GetString()); }

    [Fact]
    public async Task Pedido_privado_aceptacion_y_acceso_al_domicilio_seguro()
    {
        using var host = Host(); using var client = host.CreateClient(); using var provider = host.CreateClient();
        _ = await Login(host, client); var providerAccount = await Login(host, provider); var providerId = await Provider(host, providerAccount);
        var post = new HttpRequestMessage(HttpMethod.Post, "/v1/engagement/orders")
        {
            Content = JsonContent.Create(new { providerId, origin = "direct", description = "Necesito reparar una fuga de agua en casa.", attention = "home",
                address = "Calle Hidalgo 123", addressNote = "Portón verde", location = new { lat = 20.0539, lng = -99.3403 } })
        };
        post.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var createdResponse = await client.SendAsync(post); Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await Read(createdResponse); var orderId = created.GetProperty("orderId").GetGuid();
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredKeyedService<MongoDB.Driver.IMongoDatabase>(NexoDatabaseKeys.Engagement);
            var stored = await db.GetCollection<MongoDB.Bson.BsonDocument>("orders").Find(new MongoDB.Bson.BsonDocument("_id", new MongoDB.Bson.BsonBinaryData(orderId, MongoDB.Bson.GuidRepresentation.Standard))).SingleAsync();
            Assert.False(stored.Contains("address")); Assert.False(stored.Contains("point")); Assert.True(stored["addressCipher"].IsString);
        }
        var providerPending = await Read(await provider.GetAsync($"/v1/engagement/orders/{orderId}"));
        Assert.Equal(JsonValueKind.Null, providerPending.GetProperty("address").ValueKind);
        var clientDetail = await Read(await client.GetAsync($"/v1/engagement/orders/{orderId}"));
        Assert.Equal("Calle Hidalgo 123", clientDetail.GetProperty("address").GetString());
        Assert.Equal(HttpStatusCode.OK, (await provider.PostAsJsonAsync($"/v1/engagement/orders/{orderId}/accept", new { agreedPrice = new { amountCents = 25000 } })).StatusCode);
        var providerAccepted = await Read(await provider.GetAsync($"/v1/engagement/orders/{orderId}"));
        Assert.Equal("Calle Hidalgo 123", providerAccepted.GetProperty("address").GetString());
        var list = await Read(await provider.GetAsync("/v1/engagement/orders?role=provider&status=active"));
        Assert.Equal(JsonValueKind.Null, list.GetProperty("items")[0].GetProperty("address").ValueKind);
    }

    [Fact]
    public async Task Visitante_no_consulta_pedidos_y_un_tercero_recibe_404()
    {
        using var host = Host(); using var visitor = host.CreateClient(); using var client = host.CreateClient(); using var other = host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/v1/engagement/orders")).StatusCode);
        _ = await Login(host, client); _ = await Login(host, other);
        await Error(await other.GetAsync($"/v1/engagement/orders/{Guid.CreateVersion7()}"), HttpStatusCode.NotFound, NexoCodes.ResourceNotFound);
    }
}
