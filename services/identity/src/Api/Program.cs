using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NexoLocal.Identity.Api.Http;
using NexoLocal.Identity.Infrastructure.Cache;
using NexoLocal.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Base propia de S01. La cadena de Atlas llega por user-secrets o variable de entorno; nunca por appsettings.
builder.Services.AddIdentityMongo(builder.Configuration);
builder.Services.AddIdentityFeatures();

// Techo global del limite: el de abajo es por instancia y el servicio corre con dos replicas.
builder.Services.AddSharedRateLimit(builder.Configuration);
builder.Services.AddProblemDetails();

// Contrato navegable de S01: documento OpenAPI y UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new() { Title = "NexoLocal Identity (S01)", Version = "v1" }));

// Un cuerpo JSON invalido responde 400 en todos los entornos, no 500 solo en desarrollo.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

// Limite por IP sobre la superficie de autenticacion (regla de ep-03 y ep-04).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            // Varios clientes legitimos comparten una IP tras NAT: el limite acota el abuso, no el uso normal.
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1)
        }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();

// Publicado en todo entorno: el expediente de evaluacion exige el contrato navegable.
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "NexoLocal Identity v1"));

// Endpoint de infraestructura, no de negocio: no figura en el inventario de operaciones.
app.MapHealthChecks("/health/ready");

var api = app.MapGroup("/api/v1").RequireRateLimiting("auth");

// Mismo limite de ep-03/ep-04, contado en Redis para que valga igual con una o con dos instancias.
api.AddEndpointFilter(async (context, next) =>
{
    var limiter = context.HttpContext.RequestServices.GetService<SharedRateLimiter>();
    if (limiter is null)
    {
        return await next(context);
    }

    var partition = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    return await limiter.TryAcquireAsync(partition, 60, TimeSpan.FromMinutes(1))
        ? await next(context)
        : Results.StatusCode(StatusCodes.Status429TooManyRequests);
});

api.MapAuthEndpoints();

app.Run();
