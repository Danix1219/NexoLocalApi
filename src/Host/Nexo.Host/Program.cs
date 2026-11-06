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

var role = (Environment.GetEnvironmentVariable("NEXO_ROLE") ?? "api").Trim().ToLowerInvariant();

if (role == "worker")
{
    await RunWorkerAsync(args);
}
else if (role == "api")
{
    await RunApiAsync(args);
}
else
{
    throw new InvalidOperationException("NEXO_ROLE debe ser 'api' o 'worker'.");
}

static async Task RunWorkerAsync(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddCommon();
    builder.Services.AddSharedRedis(builder.Configuration);
    builder.Services.AddMongoDatabases(builder.Configuration);

    // Un unico despachador por outbox; el host de API no lo arranca.
    builder.Services.AddOutbox(NexoDatabaseKeys.Identity);
    builder.Services.AddHostedService<OutboxDispatcher>();

    await builder.Build().RunAsync();
}

static async Task RunApiAsync(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddCommon();
    builder.Services.AddSharedRedis(builder.Configuration);
    builder.Services.AddMongoDatabases(builder.Configuration);
    builder.Services.AddProblemDetails();

    // OpenAPI del framework (.NET 10), sin Swashbuckle. Se publica solo fuera de produccion.
    builder.Services.AddOpenApi();

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
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, Nexo.Host.NexoJsonContext.Default);
    });

    // Un cuerpo JSON invalido responde 400 en todos los entornos, no 500 solo en desarrollo.
    builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1)
            }));
    });

    builder.Services.AddIdentityInfrastructure();
    builder.Services.AddDiscoveryInfrastructure();
    builder.Services.AddEngagementInfrastructure();
    builder.Services.AddOutbox(NexoDatabaseKeys.Identity);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseResponseCompression();
    app.UseRateLimiter();

    // Techos compartidos en Redis para las dos instancias de App Service.
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api/v1") && !path.StartsWithSegments("/v1"))
        {
            await next();
            return;
        }

        var limiter = context.RequestServices.GetRequiredService<SharedRateLimiter>();
        var partition = context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
        if (!await limiter.IsAllowedAsync(partition, 60, TimeSpan.FromMinutes(1), context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
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

    // Fuera de produccion: contrato para el equipo movil. En produccion no se expone,
    // porque publicar la superficie completa de la API no aporta nada al cliente ya compilado.
    if (!app.Environment.IsProduction())
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
