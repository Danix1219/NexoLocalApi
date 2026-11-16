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
using Nexo.Engagement.Application.Orders;

namespace Nexo.Seguridad.Tests;

/// <summary>Chat (10) and reviews (09) over real HTTP, JWT, Mongo and Redis; only Content Safety is replaced.</summary>
[Collection("seguridad")]
public sealed class ChatAndReviewEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class Safe : IOrderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(Result<bool>.Success(true));
    }

    private WebApplicationFactory<Program> Host() => new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis)
        .WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IOrderSafety>(); s.AddSingleton<IOrderSafety, Safe>(); }));

    private static async Task<Guid> Login(WebApplicationFactory<Program> host, HttpClient client)
    {
        var session = await Cuenta.CrearAsync(host, client, $"chat-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (await Read(await client.GetAsync("/v1/identity/me"))).GetProperty("accountId").GetGuid();
    }

    private static async Task<Guid> Provider(WebApplicationFactory<Program> host, Guid owner)
    {
        using var scope = host.Services.CreateScope(); var id = Guid.CreateVersion7();
        Assert.True(await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(owner, new()
        {
            ProviderId = id,
            Name = "Plomería del Chat",
            Categories = ["plomeria"],
            Location = new(20.05, -99.34),
            PublicZone = new("Centro", new(20.05, -99.34)),
            Attention = "both",
            RadiusKm = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, 0, Guid.NewGuid().ToString(), "hash", true), default));
        return id;
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }

    private static async Task<JsonElement> Read(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string code)
    { Assert.Equal(status, response.StatusCode); Assert.Equal(code, (await Read(response)).GetProperty("code").GetString()); }

    [Fact]
    public async Task Chat_cliente_inicia_prestador_solo_si_fue_elegido_y_bloqueo_unilateral()
    {
        using var host = Host(); using var client = host.CreateClient(); using var provider = host.CreateClient(); using var stranger = host.CreateClient();
        await Login(host, client); var providerAccount = await Login(host, provider); await Login(host, stranger);
        var providerId = await Provider(host, providerAccount);

        // A provider cannot start a chat with anyone who never ordered from them.
        await Error(await Post(provider, "/v1/engagement/conversations", new { clientId = Guid.CreateVersion7() }), HttpStatusCode.Forbidden, NexoCodes.ConversationNotAllowed);
        await Error(await Post(provider, "/v1/engagement/conversations", new { providerId }), HttpStatusCode.BadRequest, NexoCodes.ConversationSelf);

        var opened = await Post(client, "/v1/engagement/conversations", new { providerId });
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var conversationId = (await Read(opened)).GetProperty("conversationId").GetGuid();
        var again = await Post(client, "/v1/engagement/conversations", new { providerId });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(conversationId, (await Read(again)).GetProperty("conversationId").GetGuid());

        var send = new HttpRequestMessage(HttpMethod.Post, $"/v1/engagement/conversations/{conversationId}/messages") { Content = JsonContent.Create(new { kind = "text", text = "Hola, ¿puedes venir mañana?" }) };
        var key = Guid.NewGuid().ToString(); send.Headers.Add("Idempotency-Key", key);
        var sent = await Read(await client.SendAsync(send));
        var retry = new HttpRequestMessage(HttpMethod.Post, $"/v1/engagement/conversations/{conversationId}/messages") { Content = JsonContent.Create(new { kind = "text", text = "Hola, ¿puedes venir mañana?" }) };
        retry.Headers.Add("Idempotency-Key", key);
        Assert.Equal(sent.GetProperty("messageId").GetGuid(), (await Read(await client.SendAsync(retry))).GetProperty("messageId").GetGuid());

        var inbox = await Read(await provider.GetAsync("/v1/engagement/conversations?role=provider"));
        Assert.Equal(1, inbox.GetProperty("totalUnread").GetInt32());
        var row = inbox.GetProperty("items")[0];
        Assert.Equal("Hola, ¿puedes venir mañana?", row.GetProperty("lastPreview").GetString());
        Assert.Equal("client", row.GetProperty("counterpart").GetProperty("kind").GetString());

        // Third parties cannot read or write.
        await Error(await stranger.GetAsync($"/v1/engagement/conversations/{conversationId}/messages"), HttpStatusCode.NotFound, NexoCodes.ConversationNotFound);

        var thread = await Read(await provider.GetAsync($"/v1/engagement/conversations/{conversationId}/messages"));
        Assert.False(thread.GetProperty("items")[0].GetProperty("mine").GetBoolean());
        var read = await Read(await provider.PostAsJsonAsync($"/v1/engagement/conversations/{conversationId}/read", new { upToMessageId = sent.GetProperty("messageId").GetGuid() }));
        Assert.Equal(1, read.GetProperty("markedCount").GetInt32()); Assert.Equal(0, read.GetProperty("totalUnread").GetInt32());
        var mine = await Read(await client.GetAsync($"/v1/engagement/conversations/{conversationId}/messages"));
        Assert.NotEqual(JsonValueKind.Null, mine.GetProperty("items")[0].GetProperty("readAt").ValueKind);

        Assert.Equal(HttpStatusCode.OK, (await provider.PostAsJsonAsync($"/v1/engagement/conversations/{conversationId}/block", new { blocked = true })).StatusCode);
        await Error(await Post(client, $"/v1/engagement/conversations/{conversationId}/messages", new { kind = "text", text = "¿Sigues ahí?" }), HttpStatusCode.Forbidden, NexoCodes.ConversationBlocked);
        await Error(await client.PostAsJsonAsync($"/v1/engagement/conversations/{conversationId}/block", new { blocked = false }), HttpStatusCode.Forbidden, NexoCodes.ConversationBlockedByOther);
        Assert.Equal(HttpStatusCode.OK, (await provider.PostAsJsonAsync($"/v1/engagement/conversations/{conversationId}/block", new { blocked = false })).StatusCode);
    }

    [Fact]
    public async Task Opinion_solo_tras_pedido_resuelto_una_vez_y_sin_ligar_al_cliente()
    {
        using var host = Host(); using var client = host.CreateClient(); using var provider = host.CreateClient(); using var anonymous = host.CreateClient();
        await Login(host, client); var providerAccount = await Login(host, provider);
        var providerId = await Provider(host, providerAccount);
        var order = await Read(await Post(client, "/v1/engagement/orders", new { providerId, origin = "direct", description = "Cambiar la llave del lavabo.", attention = "shop" }));
        var orderId = order.GetProperty("orderId").GetGuid();

        await Error(await Post(client, "/v1/engagement/reviews", new { orderId, stars = 5 }), HttpStatusCode.Forbidden, NexoCodes.ReviewOrderNotResolved);
        Assert.Equal(HttpStatusCode.OK, (await provider.PostAsJsonAsync($"/v1/engagement/orders/{orderId}/accept", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await provider.PostAsJsonAsync($"/v1/engagement/orders/{orderId}/resolve", new { })).StatusCode);
        Assert.True((await Read(await client.GetAsync($"/v1/engagement/orders/{orderId}"))).GetProperty("canReview").GetBoolean());

        await Error(await Post(provider, "/v1/engagement/reviews", new { orderId, stars = 1 }), HttpStatusCode.NotFound, NexoCodes.ResourceNotFound);
        var created = await Post(client, "/v1/engagement/reviews", new { orderId, stars = 4, text = "Muy puntual y dejó todo limpio.", recommend = true });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(4, (await Read(created)).GetProperty("providerRating").GetProperty("avg").GetDouble());
        await Error(await Post(client, "/v1/engagement/reviews", new { orderId, stars = 5 }), HttpStatusCode.Conflict, NexoCodes.ReviewAlreadyExists);
        Assert.False((await Read(await client.GetAsync($"/v1/engagement/orders/{orderId}"))).GetProperty("canReview").GetBoolean());

        var raw = await (await anonymous.GetAsync($"/v1/engagement/providers/{providerId}/reviews")).Content.ReadAsStringAsync();
        var list = JsonDocument.Parse(raw).RootElement;
        Assert.Equal(1, list.GetProperty("summary").GetProperty("count").GetInt32());
        Assert.Equal("userOpinion", list.GetProperty("trustLevel").GetString());
        Assert.DoesNotContain(orderId.ToString(), raw);
        Assert.DoesNotContain("clientId", raw);
    }
}
