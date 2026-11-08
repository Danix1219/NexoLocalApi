namespace Nexo.Outbox;

/// <summary>Mensaje persistido en la base del dominio que lo produce; nunca se publica directo a la cola.</summary>
public sealed class OutboxMessage
{
    public Guid MessageId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? ProcessingStartedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
