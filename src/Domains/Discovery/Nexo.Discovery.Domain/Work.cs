namespace Nexo.Discovery.Domain;

// No exact location or customer information belongs in a work's persisted model.
public sealed record Work(Guid WorkId, Guid ProviderId, string Category, string Title, string Text,
    string? Problem, string? Did, string? Time, string? Tag, ProviderPoint PublicPoint, string ZoneLabel,
    ProviderImage[] Images, int Recs, DateTime CreatedAt, DateTime CheckedAt);
