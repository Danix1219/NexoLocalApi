using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nexo.Identity.Endpoints.Http;

namespace Nexo.Integration;

/// <summary>
/// Contrato de 01-auth frente a lo que el equipo movil encontro contra Azure el 2026-11-06.
/// Cada prueba fija una decision documentada en `specs/endpoints/01-auth/`.
/// </summary>
[Collection("infra")]
[Trait("Category", "Integration")]
public sealed class AutenticacionTests(InfraFixture infra)
{
    private sealed class Host(string mongo, string redis) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:CodeHmacKey"] = "cHJ1ZWJhLWRlLWxsYXZlLWhtYWMtZGUtMzItYnl0ZXMh",
                ["Mail:Host"] = "localhost",
                ["Mail:From"] = "pruebas@nexolocal.mx",
                ["Mongo:ConnectionString"] = mongo,
                ["Redis:ConnectionString"] = redis
            }));
            return base.CreateHost(builder);
        }
    }

    private Host NuevoHost() => new(infra.MongoConnectionString, infra.RedisConnectionString);

    private static FormUrlEncodedContent Formulario(params (string Clave, string Valor)[] campos) =>
        new(campos.Select(c => new KeyValuePair<string, string>(c.Clave, c.Valor)));

    private static async Task<string?> Campo(HttpResponseMessage respuesta, string nombre)
    {
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return cuerpo.RootElement.TryGetProperty(nombre, out var valor) ? valor.GetString() : null;
    }

    // Punto 2 del reporte movil: el servidor acepta clientes anonimos y la app no manda client_id.
    [Fact]
    public async Task Token_sin_client_id_llega_a_validar_credenciales()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsync("/connect/token", Formulario(
            ("grant_type", "password"),
            ("username", "no.existe@nexolocal.mx"),
            ("password", "Nexo2026ok"),
            ("scope", "offline_access")));

        // invalid_grant, no invalid_client: la peticion paso el filtro de cliente y fallo donde debe.
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("invalid_grant", await Campo(respuesta, "error"));
        Assert.Equal("nexo:auth.invalid_credentials", await Campo(respuesta, "error_uri"));
    }

    // Punto 2: se decidio NO registrar `nexo-mobile`. Si alguien lo registra, esta prueba avisa
    // de que el contrato publicado dejo de ser cierto.
    [Fact]
    public async Task Token_con_client_id_lo_rechaza_porque_no_hay_clientes_registrados()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsync("/connect/token", Formulario(
            ("grant_type", "password"),
            ("username", "no.existe@nexolocal.mx"),
            ("password", "Nexo2026ok"),
            ("scope", "offline_access"),
            ("client_id", "nexo-mobile")));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("invalid_client", await Campo(respuesta, "error"));
    }

    // Punto 1: el registro no abre sesion. La app llama despues a /connect/token.
    [Fact]
    public void El_registro_no_devuelve_tokens()
    {
        var campos = typeof(AccountCreatedResponse).GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain(campos, nombre => nombre.Contains("Token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(campos, nombre => nombre.Contains("ExpiresIn", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            ["AccountId", "Email", "Name", "CurrentMode", "HasProvider", "NextStep"],
            campos);
    }

    // Punto 3: un ticket que no es de `signup` no sirve para crear la cuenta.
    [Fact]
    public async Task Registro_con_ticket_que_no_es_de_signup_responde_ticket_invalido()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var peticion = new HttpRequestMessage(HttpMethod.Post, "/v1/identity/accounts")
        {
            // El cuerpo es valido en todo lo demas: lo unico que debe fallar es el ticket. La
            // ubicacion se valida antes, asi que omitirla taparia lo que esta prueba vigila.
            Content = JsonContent.Create(new
            {
                verificationTicket = "ticket-que-no-existe",
                name = "Ana Lopez",
                password = "Nexo2026ok",
                acceptedTerms = true,
                searchLocation = new { lat = 19.4326, lng = -99.1332 },
                searchZoneLabel = "Centro",
                searchRadiusKm = 10,
                intent = "client"
            })
        };
        peticion.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var respuesta = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("email.ticket_invalid", await Campo(respuesta, "code"));
    }

    // Punto 5: Problem Details con `code` estable y `traceId`, no el formato por defecto de ASP.NET.
    [Fact]
    public async Task Los_errores_llevan_code_estable_y_traceId()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = "no-es-correo", purpose = "signup" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("email.invalid", await Campo(respuesta, "code"));
        Assert.False(string.IsNullOrWhiteSpace(await Campo(respuesta, "traceId")));
    }

    // Punto 5: sin la cabecera no se crea nada, y el motivo viaja con su `code`.
    [Fact]
    public async Task Registro_sin_Idempotency_Key_responde_con_su_code()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/v1/identity/accounts", new
        {
            verificationTicket = "da-igual",
            name = "Ana Lopez",
            password = "Nexo2026ok",
            acceptedTerms = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("request.idempotency_key_missing", await Campo(respuesta, "code"));
    }

    // Punto 5: sin access token no hay cierre de sesion.
    [Fact]
    public async Task Logout_sin_token_responde_401()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsync("/v1/identity/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
