using NexoLocal.Community.Adapters.Persistence;
using NexoLocal.Community.Modules.Feed;

var builder = WebApplication.CreateBuilder(args);

// Base propia de S03. La cadena de Atlas llega por user-secrets o variable de entorno.
builder.Services.AddCommunityMongo(builder.Configuration);
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Endpoint de infraestructura, no de negocio: no figura en el inventario de operaciones.
app.MapHealthChecks("/health/ready");

// ep-01 / EP-030: inicio por zona y modo. Solo DTO publico; el modo de UI no concede privilegios.
app.MapGet("/api/v1/feed", async (
    string? mode,
    string? zone,
    string? category,
    string? tab,
    string? cursor,
    int? limit,
    MongoFeedReader reader,
    CancellationToken ct) =>
{
    var query = FeedQuery.Create(mode, zone, category, tab, cursor, limit);
    var page = await reader.ReadAsync(query, ct);
    return Results.Ok(page);
});

app.Run();
