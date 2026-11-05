using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NexoLocal.Identity.Api.Http;
using NexoLocal.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Base propia de S01. La cadena de Atlas llega por user-secrets o variable de entorno; nunca por appsettings.
builder.Services.AddIdentityMongo(builder.Configuration);
builder.Services.AddIdentityFeatures();
builder.Services.AddProblemDetails();

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

// Endpoint de infraestructura, no de negocio: no figura en el inventario de operaciones.
app.MapHealthChecks("/health/ready");

app.MapGroup("/api/v1").RequireRateLimiting("auth").MapAuthEndpoints();

app.Run();
