using Nexo.Discovery.Application.Feed;

namespace Nexo.Discovery.Application.Ports;

/// <summary>Lectura del inicio; la infraestructura decide la base y los indices.</summary>
public interface IFeedReader
{
    Task<FeedPage> ReadAsync(FeedQuery query, CancellationToken cancellationToken = default);
}
