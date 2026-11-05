using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using NexoLocal.Community.Modules.Feed;

namespace NexoLocal.Community.Adapters.Persistence;

/// <summary>Conexion de la base propia de S03. La cadena llega por secreto, nunca por el repositorio.</summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return $"{SectionName}:ConnectionString ausente. Cargarla por user-secrets o variable {SectionName}__ConnectionString; no se versiona.";
        }

        if (!ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase)
            && !ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
        {
            return $"{SectionName}:ConnectionString debe empezar por mongodb:// o mongodb+srv:// (Atlas).";
        }

        return string.IsNullOrWhiteSpace(Database)
            ? $"{SectionName}:Database ausente. S03 solo usa su propia base."
            : null;
    }
}

public static class CommunityRegistration
{
    public static IServiceCollection AddCommunityMongo(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Validate(options => options.Validate() is null, "Configuracion de Mongo invalida.")
            .ValidateOnStart();

        // El motivo exacto importa al arrancar: sin el, falta saber si falla la cadena o la base.
        services.AddSingleton<IValidateOptions<MongoOptions>>(new MongoOptionsValidator());

        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ApplicationName = "NexoLocal.Community";
            return new MongoClient(settings);
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.Database);
        });

        services.AddSingleton<PostCollection>();
        services.AddScoped<MongoFeedReader>();
        services.AddHostedService<CommunityIndexBootstrap>();
        services.AddHealthChecks().AddCheck<CommunityMongoHealthCheck>("mongo", tags: ["ready"]);
        return services;
    }
}

/// <summary>Convierte el motivo de MongoOptions.Validate en el fallo de arranque, sin imprimir el secreto.</summary>
public sealed class MongoOptionsValidator : IValidateOptions<MongoOptions>
{
    public ValidateOptionsResult Validate(string? name, MongoOptions options) =>
        options.Validate() is { } problem ? ValidateOptionsResult.Fail(problem) : ValidateOptionsResult.Success;
}

public sealed class PostCollection
{
    public const string PostsName = "posts";

    private static readonly object MapLock = new();
    private static bool mapped;

    public PostCollection(IMongoDatabase database)
    {
        EnsureClassMaps();
        Posts = database.GetCollection<Post>(PostsName);
    }

    public IMongoCollection<Post> Posts { get; }

    /// <summary>Indices del inicio, creacion idempotente (nv-22).</summary>
    public Task EnsureIndexesAsync(CancellationToken ct) =>
        Posts.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys
                        .Ascending(p => p.Status)
                        .Ascending(p => p.Zone)
                        .Ascending(p => p.CategoryId)
                        .Descending(p => p.CreatedAt)
                        .Descending(p => p.Id),
                    new CreateIndexOptions { Name = "ix_posts_feed" })
            ],
            cancellationToken: ct);

    private static void EnsureClassMaps()
    {
        lock (MapLock)
        {
            if (mapped)
            {
                return;
            }

            ConventionRegistry.Register(
                "nexolocal-community",
                new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
                type => type.Namespace?.StartsWith("NexoLocal.Community.Modules", StringComparison.Ordinal) == true);

            BsonClassMap.TryRegisterClassMap<Post>(map =>
            {
                map.AutoMap();
                map.MapIdMember(p => p.Id);
            });

            mapped = true;
        }
    }
}

/// <summary>Lectura del inicio. Solo trabajos publicados y solo campos publicos.</summary>
public sealed class MongoFeedReader(PostCollection collection)
{
    public async Task<FeedPage> ReadAsync(FeedQuery query, CancellationToken ct)
    {
        var filters = new List<FilterDefinition<Post>>
        {
            Builders<Post>.Filter.Eq(p => p.Status, Post.Published)
        };

        if (query.Zone is not null)
        {
            filters.Add(Builders<Post>.Filter.Eq(p => p.Zone, query.Zone));
        }

        if (query.CategoryId is not null)
        {
            filters.Add(Builders<Post>.Filter.Eq(p => p.CategoryId, query.CategoryId));
        }

        // Cursor invalido: se ignora y se sirve la primera pagina, sin fallar la peticion.
        if (FeedCursor.TryDecode(query.Cursor, out var cursor))
        {
            filters.Add(Builders<Post>.Filter.Or(
                Builders<Post>.Filter.Lt(p => p.CreatedAt, cursor.CreatedAt),
                Builders<Post>.Filter.And(
                    Builders<Post>.Filter.Eq(p => p.CreatedAt, cursor.CreatedAt),
                    Builders<Post>.Filter.Lt(p => p.Id, cursor.PostId))));
        }

        // Se pide uno mas para saber si hay pagina siguiente sin contar la coleccion entera.
        var posts = await collection.Posts
            .Find(Builders<Post>.Filter.And(filters))
            .SortByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Limit(query.Limit + 1)
            .ToListAsync(ct);

        var hasMore = posts.Count > query.Limit;
        if (hasMore)
        {
            posts.RemoveAt(posts.Count - 1);
        }

        var items = posts
            .Select(p => new FeedCard(
                p.Id, p.Title, p.Summary, p.CategoryId, p.Zone, p.ProviderId, p.ProviderName, p.CoverMediaId, p.CreatedAt))
            .ToList();

        var next = hasMore && posts.Count > 0
            ? new FeedCursor(posts[^1].CreatedAt, posts[^1].Id).Encode()
            : null;

        return new FeedPage(items, next, Partial: false);
    }
}

public sealed class CommunityIndexBootstrap(PostCollection collection, ILogger<CommunityIndexBootstrap> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await collection.EnsureIndexesAsync(cancellationToken);
            logger.LogInformation("Indices de S03 verificados.");
        }
        catch (Exception error)
        {
            logger.LogError(error, "No se pudieron verificar los indices de S03; revisar la conexion antes de operar.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class CommunityMongoHealthCheck(IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Mongo responde.");
        }
        catch (Exception error)
        {
            return HealthCheckResult.Unhealthy("Mongo no responde.", error);
        }
    }
}
