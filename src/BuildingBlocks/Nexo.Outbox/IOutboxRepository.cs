namespace Nexo.Outbox;

/// <summary>Persistencia del outbox con reclamo atomico por mensaje.</summary>
public interface IOutboxRepository
{
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int max, CancellationToken cancellationToken = default);

    Task<bool> TryBeginProcessingAsync(Guid messageId, DateTimeOffset now, TimeSpan staleAfter, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid messageId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset now, CancellationToken cancellationToken = default);
}
