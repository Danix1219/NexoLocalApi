using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Common.Redis;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Infrastructure.Persistence;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.MongoDb.Models;

namespace Nexo.Identity.Infrastructure.Security;

/// <summary>
/// Servidor OAuth 2.0 de Identity (01-auth/post-connect-token-*.md).
/// Cliente publico y anonimo: la app movil no puede guardar un secreto, asi que no se finge uno.
/// </summary>
public static class OpenIddictSetup
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public const string ApiScope = "nexo_api";

    public static IServiceCollection AddIdentityAuthorizationServer(
        this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var certificates = configuration.GetSection(IdentitySecretsOptions.SectionName);
        services.AddOpenIddict()
            // La base concreta la enlaza OpenIddictDatabaseBinder: aqui no se resuelve el contenedor.
            .AddCore(options => options.UseMongoDb())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("connect/token");

                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();

                // La app es un cliente publico: no hay secreto que proteger en el telefono.
                options.AcceptAnonymousClients();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    ApiScope);

                options.SetAccessTokenLifetime(AccessTokenLifetime);
                options.SetRefreshTokenLifetime(RefreshTokenLifetime);

                // Rotacion obligatoria: cada uso del refresh emite uno nuevo y quema el anterior.
                // Es lo que convierte un robo de refresh en una sesion cortada, no en acceso libre.
                options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);

                // Refresh opaco y validado contra el almacen, como pide la spec: ademas hace que
                // revocarlo surta efecto de inmediato. Un refresh autocontenido seguiria sirviendo
                // aunque se borrara su registro.
                options.UseReferenceRefreshTokens();

                // El access token viaja como JWT legible para que la app lea `exp`; va firmado,
                // no cifrado, porque no lleva datos sensibles.
                options.DisableAccessTokenEncryption();

                // Certificados por variable de entorno (PFX en Base64). Fuera de Development,
                // faltar uno es un fallo de arranque: con un certificado de desarrollo en
                // produccion cualquiera podria firmar tokens validos.
                var signing = ReadCertificate(certificates, "SigningCertificate");
                var encryption = ReadCertificate(certificates, "EncryptionCertificate");

                if (signing is not null)
                {
                    options.AddSigningCertificate(signing);
                }
                else if (isDevelopment)
                {
                    options.AddDevelopmentSigningCertificate();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Falta Identity__SigningCertificateBase64; sin el no se firman tokens en produccion.");
                }

                if (encryption is not null)
                {
                    options.AddEncryptionCertificate(encryption);
                }
                else if (isDevelopment)
                {
                    options.AddDevelopmentEncryptionCertificate();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Falta Identity__EncryptionCertificateBase64; sin el no se cifran los tokens de refresco.");
                }

                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Esquema por omision: sin esto, un endpoint con RequireAuthorization no sabe a quien
        // pedirle las credenciales y revienta en vez de responder 401.
        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        // OpenIddict guarda sus tokens en la base de Identity, no en una base aparte.
        services.AddSingleton<IConfigureOptions<OpenIddictMongoDbOptions>, OpenIddictDatabaseBinder>();

        services.AddScoped<ISessionTerminator, OpenIddictSessionTerminator>();
        return services;
    }

    /// <summary>Lee un PFX en Base64 de la configuracion; null si no esta definido.</summary>
    private static X509Certificate2? ReadCertificate(IConfiguration section, string name)
    {
        var raw = section[$"{name}Base64"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return X509CertificateLoader.LoadPkcs12(
            Convert.FromBase64String(raw),
            section[$"{name}Password"],
            X509KeyStorageFlags.EphemeralKeySet);
    }
}

/// <summary>Enlaza el almacen de OpenIddict a la base keyed de Identity.</summary>
public sealed class OpenIddictDatabaseBinder(IServiceProvider provider)
    : IConfigureOptions<OpenIddictMongoDbOptions>
{
    public void Configure(OpenIddictMongoDbOptions options) =>
        options.Database = provider.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Identity);
}

/// <summary>
/// Cierre de sesiones real: revoca en el almacen de OpenIddict y mete el `jti` en la lista negra.
/// Sin la lista negra el logout no surtiria efecto hasta que caducara el access token.
/// </summary>
public sealed class OpenIddictSessionTerminator(
    IOpenIddictTokenManager tokens,
    IOpenIddictAuthorizationManager authorizations,
    ISessionRevocationStore revocation,
    IdentityCollections collections,
    ILogger<OpenIddictSessionTerminator> logger) : ISessionTerminator
{
    public async Task BlacklistAccessTokenAsync(string jti, TimeSpan remaining, CancellationToken ct)
    {
        await revocation.RevokeAsync(jti, remaining, ct);

        // Respaldo auditable con TTL; Redis es lo que consulta el pipeline en cada peticion.
        await collections.RevokedTokens.InsertOneAsync(
            new MongoDB.Bson.BsonDocument
            {
                { "_id", jti },
                { "expiresAt", DateTime.UtcNow.Add(remaining) }
            },
            cancellationToken: ct);
    }

    /// <summary>Revoca la familia a la que pertenece ese refresh: la autorizacion y sus tokens.</summary>
    public async Task RevokeFamilyAsync(string refreshToken, CancellationToken ct)
    {
        var token = await tokens.FindByReferenceIdAsync(refreshToken, ct);
        if (token is null)
        {
            logger.LogInformation("Refresh no encontrado al cerrar sesion; no es un error.");
            return;
        }

        var authorizationId = await tokens.GetAuthorizationIdAsync(token, ct);
        if (string.IsNullOrEmpty(authorizationId))
        {
            await tokens.TryRevokeAsync(token, ct);
            return;
        }

        var authorization = await authorizations.FindByIdAsync(authorizationId, ct);
        if (authorization is not null)
        {
            await authorizations.TryRevokeAsync(authorization, ct);
        }

        await foreach (var familyToken in tokens.FindByAuthorizationIdAsync(authorizationId, ct))
        {
            await tokens.TryRevokeAsync(familyToken, ct);
        }
    }

    public async Task RevokeAllForAccountAsync(Guid accountId, CancellationToken ct)
    {
        var subject = accountId.ToString();

        await foreach (var authorization in authorizations.FindBySubjectAsync(subject, ct))
        {
            await authorizations.TryRevokeAsync(authorization, ct);
        }

        await foreach (var token in tokens.FindBySubjectAsync(subject, ct))
        {
            await tokens.TryRevokeAsync(token, ct);
        }
    }
}
