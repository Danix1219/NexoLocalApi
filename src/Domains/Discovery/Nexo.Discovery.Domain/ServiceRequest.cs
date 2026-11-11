namespace Nexo.Discovery.Domain;

// The exact point is stored separately by the adapter; it cannot appear in public data.
public sealed record ServiceRequest(Guid RequestId, string Category, string Title, string Description,
    string Urgency, string Contact, ProviderPoint PublicPoint, string ZoneLabel, string? ImageUrl,
    string Status, int InterestCount, int ReachableProviders, DateTime CreatedAt, DateTime CheckedAt,
    DateTime? ClosedAt = null, Guid? AssignedProviderId = null, string[]? ImageUrls = null)
{
    public string UrgencyText => Urgency switch { "today" => "Hoy", "week" => "Esta semana", _ => "Sin prisa" };
    public string[] Photos => ImageUrls is { Length: > 0 } ? ImageUrls : ImageUrl is { Length: > 0 } ? [ImageUrl] : [];
}
