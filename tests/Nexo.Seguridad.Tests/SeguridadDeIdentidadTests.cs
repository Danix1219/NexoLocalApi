using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Seguridad.Tests;

/// <summary>
/// Guardas de seguridad sobre propiedades ya implementadas (lineamiento 7).
///
/// No comprueban funcionalidad: comprueban que no se pierda una proteccion al cambiar el codigo.
/// Cada una fija algo que, si se rompiera, no se notaria probando la aplicacion a mano.
/// </summary>
[Collection("seguridad")]
[Trait("Category", "Seguridad")]
public sealed class SeguridadDeIdentidadTests(InfraDeSeguridad infra)
{
    private HostDeSeguridad NuevoHost() => new(infra.CadenaMongo, infra.CadenaRedis);

    private static string CorreoNuevo() => $"seg.{Guid.NewGuid():N}@nexolocal.mx";

    [Fact]
    public async Task Cambiar_correo_sin_sesion_no_encola_un_codigo()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();
        var correo = CorreoNuevo();

        var respuesta = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = correo, purpose = "changeEmail" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("auth.token_expired", await Cuenta.Campo(respuesta, "code"));
        using var scope = host.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredKeyedService<MongoDB.Driver.IMongoDatabase>(
            Nexo.Common.Mongo.NexoDatabaseKeys.Identity);
        var messages = database.GetCollection<MongoDB.Bson.BsonDocument>("outbox");
        Assert.Equal(0, await messages.CountDocumentsAsync(
            MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Regex(
                "Payload", new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(correo)))));
    }

    [Fact]
    public async Task Recuperacion_no_se_solicita_desde_la_ruta_de_registro()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = CorreoNuevo(), purpose = "resetPassword" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("validation_failed", await Cuenta.Campo(respuesta, "code"));
    }

    [Fact]
    public async Task Cambiar_correo_con_sesion_encola_la_verificacion_del_nuevo_buzon()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();
        var sesion = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        var correo = CorreoNuevo();

        var respuesta = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = correo, purpose = "changeEmail" });

        Assert.Equal(HttpStatusCode.Accepted, respuesta.StatusCode);
        Assert.Equal(6, (await Cuenta.CodigoDelOutboxAsync(host, correo)).Length);
    }

    // ---------------------------------------------------------------------
    // No se pueden enumerar cuentas
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Pedir_codigo_responde_igual_exista_o_no_la_cuenta()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var registrada = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());

        var conCuenta = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = registrada.Correo, purpose = "signup" });
        var sinCuenta = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = CorreoNuevo(), purpose = "signup" });

        // Mismo codigo y mismo cuerpo: cualquier diferencia permitiria saber que correos existen.
        Assert.Equal(conCuenta.StatusCode, sinCuenta.StatusCode);
        Assert.Equal(
            await conCuenta.Content.ReadAsStringAsync(),
            await sinCuenta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Recuperar_contrasena_responde_igual_exista_o_no_la_cuenta()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var registrada = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());

        var conCuenta = await cliente.PostAsJsonAsync(
            "/v1/identity/password/forgot", new { email = registrada.Correo });
        var sinCuenta = await cliente.PostAsJsonAsync(
            "/v1/identity/password/forgot", new { email = CorreoNuevo() });

        Assert.Equal(conCuenta.StatusCode, sinCuenta.StatusCode);
        Assert.Equal(
            await conCuenta.Content.ReadAsStringAsync(),
            await sinCuenta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Contrasena_incorrecta_y_cuenta_inexistente_dan_el_mismo_error()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var registrada = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());

        var malaContrasena = await Cuenta.IniciarSesionAsync(cliente, registrada.Correo, "OtraClave9");
        var noExiste = await Cuenta.IniciarSesionAsync(cliente, CorreoNuevo(), "OtraClave9");

        Assert.Equal(malaContrasena.StatusCode, noExiste.StatusCode);
        Assert.Equal(
            await Cuenta.Campo(malaContrasena, "error"),
            await Cuenta.Campo(noExiste, "error"));
        Assert.Equal(
            await Cuenta.Campo(malaContrasena, "error_description"),
            await Cuenta.Campo(noExiste, "error_description"));
    }

    // ---------------------------------------------------------------------
    // Fuerza bruta
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Cinco_intentos_fallidos_bloquean_la_cuenta()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var sesion = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());

        string? ultimo = null;
        for (var intento = 0; intento < 6; intento++)
        {
            var fallo = await Cuenta.IniciarSesionAsync(cliente, sesion.Correo, "ClaveQueNoEs9");
            ultimo = await Cuenta.Campo(fallo, "error_uri");
        }

        Assert.Equal("nexo:auth.account_locked", ultimo);

        // El bloqueo tiene que valer tambien con la contrasena correcta: si no, no protege nada.
        var conLaBuena = await Cuenta.IniciarSesionAsync(cliente, sesion.Correo, Config.Contrasena);
        Assert.Equal("nexo:auth.account_locked", await Cuenta.Campo(conLaBuena, "error_uri"));
    }

    // ---------------------------------------------------------------------
    // Sesiones
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Sin_token_no_se_cierra_sesion()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsync("/v1/identity/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Tras_cerrar_sesion_el_access_token_deja_de_servir()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var sesion = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);

        var cierre = await cliente.PostAsJsonAsync("/v1/identity/logout", new { refreshToken = sesion.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, cierre.StatusCode);

        // El token sigue siendo valido criptograficamente hasta su `exp`: sin la lista negra en
        // Redis, el cierre de sesion no surtiria efecto durante los minutos que le queden.
        var despues = await cliente.PostAsync("/v1/identity/logout", null);
        Assert.NotEqual(HttpStatusCode.NoContent, despues.StatusCode);
    }

    [Fact]
    public async Task Tras_cerrar_sesion_el_refresh_token_deja_de_servir()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var sesion = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        await cliente.PostAsJsonAsync("/v1/identity/logout", new { refreshToken = sesion.RefreshToken });

        cliente.DefaultRequestHeaders.Authorization = null;
        var renovado = await Cuenta.RefrescarAsync(cliente, sesion.RefreshToken);

        Assert.False(renovado.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Reusar_un_refresh_token_ya_rotado_revoca_la_familia()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var sesion = await Cuenta.CrearAsync(host, cliente, CorreoNuevo());

        var primera = await Cuenta.RefrescarAsync(cliente, sesion.RefreshToken);
        primera.EnsureSuccessStatusCode();
        var nuevo = await Cuenta.Campo(primera, "refresh_token");
        Assert.False(string.IsNullOrWhiteSpace(nuevo));

        // Reusar el viejo es la senal de que alguien lo robo: no basta con rechazarlo, hay que
        // invalidar tambien el que se entrego en su lugar.
        var reuso = await Cuenta.RefrescarAsync(cliente, sesion.RefreshToken);
        Assert.False(reuso.IsSuccessStatusCode);

        var conElNuevo = await Cuenta.RefrescarAsync(cliente, nuevo!);
        Assert.False(conElNuevo.IsSuccessStatusCode);
    }

    // ---------------------------------------------------------------------
    // Lo que nunca debe salir en una respuesta
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Ninguna_respuesta_del_alta_filtra_la_contrasena_ni_su_hash()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();
        var correo = CorreoNuevo();

        var enviado = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = correo, purpose = "signup" });
        var cuerpoEnvio = await enviado.Content.ReadAsStringAsync();

        var sesion = await Cuenta.CrearAsync(host, cliente, correo);
        var login = await Cuenta.IniciarSesionAsync(cliente, correo, Config.Contrasena);
        var cuerpoLogin = await login.Content.ReadAsStringAsync();

        foreach (var cuerpo in new[] { cuerpoEnvio, cuerpoLogin })
        {
            Assert.DoesNotContain(Config.Contrasena, cuerpo, StringComparison.Ordinal);
            Assert.DoesNotContain("argon2id", cuerpo, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("passwordHash", cuerpo, StringComparison.OrdinalIgnoreCase);
        }

        Assert.NotEqual(string.Empty, sesion.AccessToken);
    }

    [Fact]
    public async Task El_codigo_de_verificacion_no_viaja_en_la_respuesta()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();
        var correo = CorreoNuevo();

        var enviado = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = correo, purpose = "signup" });
        var cuerpo = await enviado.Content.ReadAsStringAsync();

        // Se compara contra el codigo real, sacado del outbox. Buscar la palabra "code" no valdria:
        // la respuesta lleva `codeLength`, que es publico y le dice a la app cuantos digitos pedir.
        var codigo = await Cuenta.CodigoDelOutboxAsync(host, correo);

        // Si el codigo viajara aqui, cualquiera podria tomar una cuenta ajena pidiendo un codigo
        // para el correo de otro.
        Assert.DoesNotContain(codigo, cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("\"code\":", cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Los_errores_no_exponen_trazas_ni_rutas_internas()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = "no-es-correo", purpose = "signup" });
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"code\":\"email.invalid\"", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("/home/runner", cuerpo, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------
    // Un ticket sirve para una sola cosa
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Un_ticket_de_signup_no_sirve_para_restablecer_la_contrasena()
    {
        using var host = NuevoHost();
        using var cliente = host.CreateClient();
        var correo = CorreoNuevo();

        // Un ticket de `signup` recien emitido, sin llegar a crear la cuenta.
        await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification", new { email = correo, purpose = "signup" });
        var confirmado = await cliente.PostAsJsonAsync(
            "/v1/identity/email/verification/confirm",
            new { email = correo, code = await Cuenta.CodigoDelOutboxAsync(host, correo) });
        confirmado.EnsureSuccessStatusCode();
        var ticket = await Cuenta.Campo(confirmado, "verificationTicket");

        // La contrasena se valida antes que el ticket, asi que esta tiene que ser valida para que
        // la comprobacion llegue al proposito y no se quede en `password.too_weak`.
        var intento = await cliente.PostAsJsonAsync("/v1/identity/password/reset", new
        {
            verificationTicket = ticket,
            password = "OtraClaveValida9",
            passwordConfirm = "OtraClaveValida9"
        });

        Assert.Equal(HttpStatusCode.BadRequest, intento.StatusCode);
        Assert.Equal("email.ticket_invalid", await Cuenta.Campo(intento, "code"));
    }
}
