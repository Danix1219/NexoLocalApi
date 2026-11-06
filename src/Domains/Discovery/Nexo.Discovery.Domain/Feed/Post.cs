namespace Nexo.Discovery.Domain.Feed;

/// <summary>Trabajo publicado; Discovery conserva solo lo que la tarjeta publica necesita.</summary>
public sealed class Post
{
    public const string Published = "published";

    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required string CategoryId { get; init; }
    public required string Zone { get; init; }
    public required string ProviderId { get; init; }
    public required string ProviderName { get; init; }
    public string? CoverMediaId { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
