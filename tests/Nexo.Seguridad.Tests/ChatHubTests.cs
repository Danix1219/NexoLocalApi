using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Seguridad.Tests;

/// <summary>Real hub over the test server, Redis backplane included: ticket → connect → a sent message arrives live.</summary>
[Collection("seguridad")]
public sealed class ChatHubTests(InfraDeSeguridad infra)
{
    private sealed class Safe : IOrderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(Result<bool>.Success(true));
    }

    [Fact]
    public async Task Mensaje_llega_en_vivo_al_destinatario_y_el_ticket_es_de_un_solo_uso()
    {
        using var host = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis)
            .WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IOrderSafety>(); s.AddSingleton<IOrderSafety, Safe>(); }));
        using var client = host.CreateClient(); using var provider = host.CreateClient();
        await Login(host, client); var providerAccount = await Login(host, provider);
        var providerId = Guid.CreateVersion7();
        using (var scope = host.Services.CreateScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(providerAccount, new()
            {
                ProviderId = providerId,
                Name = "En vivo SA",
                Categories = ["plomeria"],
                Location = new(20.05, -99.34),
                PublicZone = new("Centro", new(20.05, -99.34)),
                Attention = "both",
                RadiusKm = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, 0, Guid.NewGuid().ToString(), "hash", true), default));

        var open = new HttpRequestMessage(HttpMethod.Post, "/v1/engagement/conversations") { Content = JsonContent.Create(new { providerId }) };
        open.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var conversationId = (await Read(await client.SendAsync(open))).GetProperty("conversationId").GetGuid();

        var ticket = (await Read(await provider.PostAsync("/v1/engagement/hubs/chat/ticket", null))).GetProperty("ticket").GetString()!;
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var live = Connect(host, ticket);
        live.On<JsonElement>("messageReceived", m => received.TrySetResult(m));
        await live.StartAsync();

        var send = new HttpRequestMessage(HttpMethod.Post, $"/v1/engagement/conversations/{conversationId}/messages")
        { Content = JsonContent.Create(new { kind = "text", text = "¿Puedes venir hoy?" }) };
        send.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        Assert.True((await client.SendAsync(send)).IsSuccessStatusCode);

        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(conversationId, message.GetProperty("conversationId").GetGuid());
        Assert.Equal("¿Puedes venir hoy?", message.GetProperty("text").GetString());
        Assert.False(message.GetProperty("mine").GetBoolean());

        // The same ticket was consumed by the first connection: a second one is closed by the server.
        await using var replay = Connect(host, ticket);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        replay.Closed += _ => { closed.TrySetResult(); return Task.CompletedTask; };
        try { await replay.StartAsync(); await closed.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (Exception) { /* rejected during the handshake: also a refusal */ }
        Assert.NotEqual(HubConnectionState.Connected, replay.State);
    }

    private static HubConnection Connect(WebApplicationFactory<Program> host, string ticket) => new HubConnectionBuilder()
        .WithUrl(new Uri(host.Server.BaseAddress, $"/hubs/chat?ticket={ticket}"), o =>
        {
            o.Transports = HttpTransportType.LongPolling;
            o.HttpMessageHandlerFactory = _ => host.Server.CreateHandler();
        }).Build();

    private static async Task<Guid> Login(WebApplicationFactory<Program> host, HttpClient http)
    {
        var session = await Cuenta.CrearAsync(host, http, $"hub-{Guid.NewGuid():N}@example.test");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (await Read(await http.GetAsync("/v1/identity/me"))).GetProperty("accountId").GetGuid();
    }
    private static async Task<JsonElement> Read(HttpResponseMessage r) => JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();
}

/// <summary>EP-082 without Azure OpenAI configured: the person gets 503 assist.unavailable and keeps writing by hand.</summary>
[Collection("seguridad")]
public sealed class AssistEndpointTests(InfraDeSeguridad infra)
{
    [Fact]
    public async Task Sin_credenciales_responde_no_disponible_y_exige_sesion()
    {
        using var host = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis);
        using var visitor = host.CreateClient(); using var user = host.CreateClient();
        var body = new { kind = "request", categoryId = "plomeria", userText = "se me rompió la llave", lengthLimit = 600 };
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await visitor.PostAsJsonAsync("/v1/assist/draft", body)).StatusCode);
        var session = await Cuenta.CrearAsync(host, user, $"assist-{Guid.NewGuid():N}@example.test");
        user.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var r = await user.PostAsJsonAsync("/v1/assist/draft", body);
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal("assist.unavailable", JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString());
    }
}
