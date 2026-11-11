using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Nexo.Common;
using Nexo.Common.Mongo;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Common.Redis;
using Nexo.Discovery.Endpoints;
using Nexo.Discovery.Infrastructure;
using Nexo.Engagement.Endpoints;
using Nexo.Engagement.Infrastructure;
using Nexo.Identity.Endpoints;
using Nexo.Identity.Infrastructure;
using Nexo.Outbox;
using Nexo.Screens.Endpoints;
using Scalar.AspNetCore;

// Primero el .env global: todas las variables salen de ahi.
// Lo que ya este en el entorno gana, que es como llegan en Azure.
Nexo.Host.DotEnv.Load(AppContext.BaseDirectory);

await RunApiAsync(args);

static async Task RunApiAsync(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    // Request URLs may contain a private address or precise coordinates.
    builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

    builder.Services.AddCommon();
    builder.Services.AddScoped<Nexo.Discovery.Application.Providers.IProviderAccounts, Nexo.Host.ProviderAccountsBridge>();
    builder.Services.AddScoped<Nexo.Discovery.Application.Requests.IRequestClients, Nexo.Host.RequestClientsBridge>();
    builder.Services.AddSingleton<Nexo.Discovery.Application.Requests.IRequestInterests, Nexo.Host.RequestInterestsBridge>();
    builder.Services.AddSingleton<Nexo.Host.InterestDiscoveryResilience>();
    builder.Services.AddScoped<Nexo.Engagement.Application.Interests.IInterestRequestAccess, Nexo.Host.InterestRequestAccessBridge>();
    builder.Services.AddScoped<Nexo.Engagement.Application.Interests.IInterestDirectory, Nexo.Host.InterestDirectoryBridge>();
    builder.Services.AddScoped<Nexo.Engagement.Application.Interests.IInterestSafety, Nexo.Host.InterestSafetyBridge>();
    builder.Services.AddScoped<Nexo.Engagement.Application.Orders.IOrderProviderAccess, Nexo.Host.OrderProviderAccessBridge>();
    builder.Services.AddScoped<Nexo.Engagement.Application.Orders.IOrderSafety, Nexo.Host.OrderSafetyBridge>();
    builder.Services.AddSharedRedis(builder.Configuration);
    builder.Services.AddMongoDatabases(builder.Configuration);
    builder.Services.AddProblemDetails();

    // OpenIddict registra su esquema; aqui solo hace falta la pieza de autorizacion.
    builder.Services.AddAuthorization();
    builder.Services.AddHttpClient<Nexo.Identity.Infrastructure.Safety.AccountProfileSafety>(c=>c.Timeout=TimeSpan.FromSeconds(8));

    // OpenAPI del framework (.NET 10), sin Swashbuckle. Se publica solo fuera de produccion.
    // La version sellada en el ensamblado viaja al documento, de modo que se puede comprobar que
    // version esta viva pidiendo `/openapi/v1.json`, sin entrar al portal ni a los registros.
    builder.Services.AddOpenApi(opciones => opciones.AddDocumentTransformer((documento, _, _) =>
    {
        documento.Info.Title = "Nexo Local API";
        documento.Info.Version = Nexo.Host.VersionDelHost.Actual;
        return Task.CompletedTask;
    }));

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, Nexo.Host.NexoJsonContext.Default);
    });

    // Un cuerpo JSON invalido responde 400 en todos los entornos, no 500 solo en desarrollo.
    builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

    // Politicas de abuso por minuto, en memoria por replica. Las exactas viven en Redis.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, _) =>
            await NexoProblem.From(NexoCodes.RateLimitExceeded, "Demasiadas solicitudes.", context.HttpContext).ExecuteAsync(context.HttpContext);

        AddFixedWindow(options, NexoRateLimits.Public, NexoRateLimits.PublicPerMinute, ByIp);
        AddFixedWindow(options, NexoRateLimits.Auth, NexoRateLimits.AuthPerMinute, ByIp);
        AddFixedWindow(options, NexoRateLimits.User, NexoRateLimits.UserPerMinute, BySubject);
        AddFixedWindow(options, NexoRateLimits.Chat, NexoRateLimits.ChatPerMinute, BySubject);
    });

    static void AddFixedWindow(
        RateLimiterOptions options, string policy, int permitLimit, Func<HttpContext, string> partition) =>
        options.AddPolicy(policy, context => RateLimitPartition.GetFixedWindowLimiter(
            partition(context),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1) }));

    static string ByIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

    static string BySubject(HttpContext context) => context.User.FindFirst("sub")?.Value ?? ByIp(context);

    builder.Services.AddIdentityInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
    builder.Services.AddDiscoveryInfrastructure(builder.Configuration);
    builder.Services.AddScoped<Nexo.Identity.Application.Ports.IAccountProfileSafety,Nexo.Host.AccountMediaBridge>();
    builder.Services.AddEngagementInfrastructure();
    builder.Services.AddIdentityOutboxHandler(builder.Configuration);
    builder.Services.AddOutbox(NexoDatabaseKeys.Identity);

    // Un solo proceso: el despachador vive dentro de la API. El reclamo en Mongo es atomico,
    // asi que varias replicas no procesan el mismo mensaje.
    builder.Services.AddHostedService<OutboxDispatcher>();

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseResponseCompression();
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    // Lista negra de `jti`: el access token sigue siendo valido criptograficamente hasta su `exp`,
    // asi que sin esto el logout no surtiria efecto durante los minutos que le queden.
    app.Use(async (context, next) =>
    {
        var jti = context.User.FindFirst("jti")?.Value;
        if (!string.IsNullOrEmpty(jti))
        {
            var revocation = context.RequestServices.GetRequiredService<ISessionRevocationStore>();
            if (await revocation.IsRevokedAsync(jti, context.RequestAborted))
            {
                await NexoProblem
                    .From(NexoCodes.AuthTokenRevoked, "La sesion fue cerrada.", context)
                    .ExecuteAsync(context);
                return;
            }
        }

        await next();
    });

    // Techos compartidos en Redis para las dos instancias de App Service.
    app.UseIdentityAccountAccess();

    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api/v1") && !path.StartsWithSegments("/v1"))
        {
            await next();
            return;
        }

        var limiter = context.RequestServices.GetRequiredService<SharedRateLimiter>();
        var policy = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        var subject = context.User.FindFirst("sub")?.Value;
        var personal = policy is NexoRateLimits.User or NexoRateLimits.Chat && !string.IsNullOrEmpty(subject);
        var partition = personal ? $"{policy}:{subject}" : context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
        var maximum = personal && policy == NexoRateLimits.User ? NexoRateLimits.UserPerMinute : NexoRateLimits.PublicPerMinute;
        if (!await limiter.IsAllowedAsync(partition, maximum, TimeSpan.FromMinutes(1), context.RequestAborted))
        {
            await NexoProblem.From(NexoCodes.RateLimitExceeded, "Demasiadas solicitudes.", context).ExecuteAsync(context);
            return;
        }

        await next();
    });

    // Sondas de plataforma: no publican operaciones de negocio.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });

    // Contrato para el equipo movil. Se publica tambien en produccion, donde es la unica
    // referencia que tienen contra el entorno real: describe rutas y formas, nunca datos.
    // Para apagarlo basta Docs__Enabled=false en las variables del sitio.
    if (builder.Configuration.GetValue("Docs:Enabled", true))
    {
        app.MapOpenApi();
        app.MapScalarApiReference();

        // La raiz daba 404 y mandaba a buscar /swagger, que no existe aqui.
        app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
    }

    // Sin CORS a proposito: la app movil es nativa y CORS es un mecanismo del navegador.
    // Abrirlo sin un navegador que lo necesite solo añade superficie. Al aparecer Flutter Web
    // o la landing, se añade una lista de origenes permitidos, nunca AllowAnyOrigin.

    app.MapIdentityEndpoints();
    app.MapDiscoveryEndpoints();
    app.MapEngagementEndpoints();
    app.MapScreensEndpoints();

    await app.RunAsync();
}
