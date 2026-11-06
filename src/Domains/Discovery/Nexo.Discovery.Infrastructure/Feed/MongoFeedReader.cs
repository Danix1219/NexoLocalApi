using MongoDB.Driver;
using Nexo.Discovery.Application.Feed;
using Nexo.Discovery.Application.Ports;
using Nexo.Discovery.Domain.Feed;

namespace Nexo.Discovery.Infrastructure.Feed;

/// <summary>Lectura del inicio. Solo trabajos publicados y solo campos publicos.</summary>
public sealed class MongoFeedReader(PostCollection collection) : IFeedReader
{
    public async Task<FeedPage> ReadAsync(FeedQuery query, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Post>>
        {
            Builders<Post>.Filter.Eq(post => post.Status, Post.Published)
        };

        if (query.Zone is not null)
        {
            filters.Add(Builders<Post>.Filter.Eq(post => post.Zone, query.Zone));
        }

        if (query.CategoryId is not null)
        {
            filters.Add(Builders<Post>.Filter.Eq(post => post.CategoryId, query.CategoryId));
        }

        // Cursor invalido: se ignora y se sirve la primera pagina, sin fallar la peticion.
        if (FeedCursor.TryDecode(query.Cursor, out var cursor))
        {
            filters.Add(Builders<Post>.Filter.Or(
                Builders<Post>.Filter.Lt(post => post.CreatedAt, cursor.CreatedAt),
                Builders<Post>.Filter.And(
                    Builders<Post>.Filter.Eq(post => post.CreatedAt, cursor.CreatedAt),
                    Builders<Post>.Filter.Lt(post => post.Id, cursor.PostId))));
        }

        // Se pide uno mas para saber si hay pagina siguiente sin contar la coleccion entera.
        var posts = await collection.Posts
            .Find(Builders<Post>.Filter.And(filters))
            .SortByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Limit(query.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = posts.Count > query.Limit;
        if (hasMore)
        {
            posts.RemoveAt(posts.Count - 1);
        }

        var items = posts
            .Select(post => new FeedCard(
                post.Id,
                post.Title,
                post.Summary,
                post.CategoryId,
                post.Zone,
                post.ProviderId,
                post.ProviderName,
                post.CoverMediaId,
                post.CreatedAt))
            .ToList();

        var next = hasMore && posts.Count > 0
            ? new FeedCursor(posts[^1].CreatedAt, posts[^1].Id).Encode()
            : null;

        return new FeedPage(items, next, Partial: false);
    }
}
