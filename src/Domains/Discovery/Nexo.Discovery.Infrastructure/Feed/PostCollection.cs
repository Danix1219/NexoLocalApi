using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Nexo.Discovery.Domain.Feed;

namespace Nexo.Discovery.Infrastructure.Feed;

/// <summary>Coleccion `posts` de Discovery; el dominio no conoce MongoDB.</summary>
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

    /// <summary>Indices del inicio, creacion idempotente.</summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        Posts.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Post>(
                    Builders<Post>.IndexKeys
                        .Ascending(post => post.Status)
                        .Ascending(post => post.Zone)
                        .Ascending(post => post.CategoryId)
                        .Descending(post => post.CreatedAt)
                        .Descending(post => post.Id),
                    new CreateIndexOptions { Name = "ix_posts_feed" })
            ],
            cancellationToken: cancellationToken);

    private static void EnsureClassMaps()
    {
        lock (MapLock)
        {
            if (mapped)
            {
                return;
            }

            ConventionRegistry.Register(
                "nexo-discovery",
                new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
                type => type.Namespace?.StartsWith("Nexo.Discovery.Domain", StringComparison.Ordinal) == true);

            BsonClassMap.TryRegisterClassMap<Post>(map =>
            {
                map.AutoMap();
                map.MapIdMember(post => post.Id);
            });

            mapped = true;
        }
    }
}
