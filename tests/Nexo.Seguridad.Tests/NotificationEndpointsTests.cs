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
using Nexo.Engagement.Application.Notifications;
using Nexo.Engagement.Application.Orders;
using Nexo.Engagement.Infrastructure.Notifications;
using Nexo.Outbox;

namespace Nexo.Seguridad.Tests;

/// <summary>Real flow: an order event in the Engagement outbox becomes a bell notice for the right person only.</summary>
[Collection("seguridad")]
public sealed class NotificationEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class Safe : IOrderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(Result<bool>.Success(true));
    }
    private sealed class Pushes : IPushSender
    {
        public List<(Guid Account, string Title)> Sent { get; } = [];
        public Task SendAsync(Guid account, string title, string body, string? deepLink, CancellationToken ct) { Sent.Add((account, title)); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Evento_de_pedido_genera_aviso_solo_para_el_destinatario_y_se_marca_leido()
    {
        var pushes = new Pushes();
        using var host = new HostDeSeguridad(infra.CadenaMongo, infra.CadenaRedis).WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IOrderSafety>(); s.AddSingleton<IOrderSafety, Safe>();
            s.RemoveAll<IPushSender>(); s.AddSingleton<IPushSender>(pushes);
        }));
        using var client = host.CreateClient(); using var provider = host.CreateClient();
        await Login(host, client); var providerAccount = await Login(host, provider);
        var providerId = Guid.CreateVersion7();
        using (var scope = host.Services.CreateScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(providerAccount, new()
            {
                ProviderId = providerId,
                Name = "Avisos SA",
                Categories = ["plomeria"],
                Location = new(20.05, -99.34),
                PublicZone = new("Centro", new(20.05, -99.34)),
                Attention = "both",
                RadiusKm = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, 0, Guid.NewGuid().ToString(), "hash", true), default));

        var post = new HttpRequestMessage(HttpMethod.Post, "/v1/engagement/orders")
        { Content = JsonContent.Create(new { providerId, origin = "direct", description = "Cambiar la regadera del baño.", attention = "shop" }) };
        post.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var orderId = (await Read(await client.SendAsync(post))).GetProperty("orderId").GetGuid();

        // Drain the outbox now instead of waiting for the background poll.
        using (var scope = host.Services.CreateScope())
            await new EngagementOutboxDispatcher(scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<OutboxDispatcher>.Instance).DispatchOnceAsync();

        var bell = await Read(await provider.GetAsync("/v1/engagement/notifications"));
        Assert.Equal(1, bell.GetProperty("unreadCount").GetInt32());
        var item = bell.GetProperty("items")[0];
        Assert.Equal("order.created", item.GetProperty("type").GetString());
        Assert.Equal($"nexolocal://orders/{orderId}", item.GetProperty("deepLink").GetString());
        Assert.Contains(pushes.Sent, p => p.Account == providerAccount);
        Assert.Equal(0, (await Read(await client.GetAsync("/v1/engagement/notifications"))).GetProperty("unreadCount").GetInt32());

        // A foreign id is ignored silently; both or neither field is a 400.
        Assert.Equal(0, (await Read(await client.PostAsJsonAsync("/v1/engagement/notifications/read", new { notificationIds = new[] { item.GetProperty("notificationId").GetGuid() } })))
            .GetProperty("markedCount").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await provider.PostAsJsonAsync("/v1/engagement/notifications/read", new { })).StatusCode);
        var read = await Read(await provider.PostAsJsonAsync("/v1/engagement/notifications/read", new { all = true }));
        Assert.Equal(1, read.GetProperty("markedCount").GetInt32()); Assert.Equal(0, read.GetProperty("unreadCount").GetInt32());
    }

    private static async Task<Guid> Login(WebApplicationFactory<Program> host, HttpClient client)
    {
        var session = await Cuenta.CrearAsync(host, client, $"notif-{Guid.NewGuid():N}@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (await Read(await client.GetAsync("/v1/identity/me"))).GetProperty("accountId").GetGuid();
    }
    private static async Task<JsonElement> Read(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
}
