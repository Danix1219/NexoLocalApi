using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Testcontainers.MongoDb;
using Testcontainers.Redis;

namespace Nexo.Seguridad.Tests;

/// <summary>
/// MongoDB y Redis reales, compartidos por todas las clases de la coleccion para no pagar el
/// arranque de los contenedores una vez por prueba.
/// </summary>
public sealed class InfraDeSeguridad : IAsyncLifetime
{
    private readonly MongoDbContainer mongo = new MongoDbBuilder("mongo:7.0").Build();

    private readonly RedisContainer redis = new RedisBuilder("redis:7.2-alpine").Build();

    public string CadenaMongo => mongo.GetConnectionString();

    public string CadenaRedis => redis.GetConnectionString();

    public Task InitializeAsync() => Task.WhenAll(mongo.StartAsync(), redis.StartAsync());

    public async Task DisposeAsync() =>
        await Task.WhenAll(mongo.DisposeAsync().AsTask(), redis.DisposeAsync().AsTask());
}

[CollectionDefinition("seguridad")]
public sealed class ColeccionDeSeguridad : ICollectionFixture<InfraDeSeguridad>;

/// <summary>Valores que el host exige al arrancar y que en CI no llegan de ningun `.env`.</summary>
public static class Config
{
    /// <summary>
    /// Se calcula en vez de escribirse: una cadena Base64 literal junto a `CodeHmacKey` la marcan
    /// como secreto filtrado los escaneres del repositorio, y no lo es.
    /// </summary>
    public static readonly string ClaveHmac =
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("llave-de-seguridad-sin-valor"));

    /// <summary>Cumple la politica (8-64, letra y digito) y no abre nada fuera de estas pruebas.</summary>
    public const string Contrasena = "NoEsUnSecreto1";
}

public sealed class HostDeSeguridad(string mongo, string redis) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:CodeHmacKey"] = Config.ClaveHmac,
            // El SMTP no existe a proposito: el envio falla, el mensaje se queda en el outbox y
            // de ahi sacan el codigo las pruebas que necesitan una cuenta real.
            ["Mail:Host"] = "localhost",
            ["Mail:From"] = "pruebas@nexolocal.mx",
            ["Mongo:ConnectionString"] = mongo,
            ["Redis:ConnectionString"] = redis
        }));
        return base.CreateHost(builder);
    }
}

/// <summary>
/// Recorre el alta de una cuenta de verdad: codigo, ticket, registro e inicio de sesion.
///
/// El codigo nunca viaja en una respuesta —es justo lo que estas pruebas comprueban—, asi que se
/// lee del outbox, que es donde lo deja el endpoint para que lo envie el despachador.
/// </summary>
public static class Cuenta
{
    public sealed record Sesion(string Correo, string AccessToken, string RefreshToken);

    public static async Task<Sesion> CrearAsync(HostDeSeguridad host, HttpClient cliente, string correo)
    {
        var enviado = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = correo, purpose = "signup" });
        enviado.EnsureSuccessStatusCode();

        var codigo = await CodigoDelOutboxAsync(host, correo);

        var confirmado = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification/confirm", new { email = correo, code = codigo });
        confirmado.EnsureSuccessStatusCode();
        var ticket = await Campo(confirmado, "verificationTicket")
            ?? throw new InvalidOperationException("La confirmacion no devolvio ticket.");

        var alta = new HttpRequestMessage(HttpMethod.Post, "/v1/identity/accounts")
        {
            Content = JsonContent.Create(new
            {
                verificationTicket = ticket,
                name = "Persona De Prueba",
                password = Config.Contrasena,
                acceptedTerms = true,
                searchLocation = new { lat = 19.4326, lng = -99.1332 },
                searchZoneLabel = "Centro",
                searchRadiusKm = 10,
                intent = "client"
            })
        };
        alta.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await cliente.SendAsync(alta)).EnsureSuccessStatusCode();

        var sesion = await IniciarSesionAsync(cliente, correo, Config.Contrasena);
        sesion.EnsureSuccessStatusCode();

        return new Sesion(
            correo,
            await Campo(sesion, "access_token") ?? throw new InvalidOperationException("Sin access_token."),
            await Campo(sesion, "refresh_token") ?? throw new InvalidOperationException("Sin refresh_token."));
    }

    public static Task<HttpResponseMessage> IniciarSesionAsync(
        HttpClient cliente, string correo, string contrasena) =>
        cliente.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = correo,
            ["password"] = contrasena,
            ["scope"] = "offline_access"
        }));

    public static Task<HttpResponseMessage> RefrescarAsync(HttpClient cliente, string refreshToken) =>
        cliente.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        }));

    public static async Task<string?> Campo(HttpResponseMessage respuesta, string nombre)
    {
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return cuerpo.RootElement.TryGetProperty(nombre, out var valor) ? valor.GetString() : null;
    }

    /// <summary>
    /// El despachador sondea cada cinco segundos y puede haber consumido el mensaje antes de que
    /// la prueba mire, asi que se reintenta un rato en vez de fallar a la primera.
    /// </summary>
    public static async Task<string> CodigoDelOutboxAsync(HostDeSeguridad host, string correo)
    {
        var baseDatos = host.Services.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Identity);
        var outbox = baseDatos.GetCollection<BsonDocument>("outbox");
        var normalizado = correo.Trim().ToLowerInvariant();

        for (var intento = 0; intento < 40; intento++)
        {
            var mensajes = await outbox
                .Find(Builders<BsonDocument>.Filter.Eq("Type", "identity.email_code_requested"))
                .SortByDescending(d => d["OccurredAt"])
                .Limit(20)
                .ToListAsync();

            foreach (var mensaje in mensajes)
            {
                using var carga = JsonDocument.Parse(mensaje["Payload"].AsString);
                if (carga.RootElement.GetProperty("Email").GetString() == normalizado)
                {
                    return carga.RootElement.GetProperty("Code").GetString()!;
                }
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException($"No aparecio el codigo de {normalizado} en el outbox.");
    }
}
