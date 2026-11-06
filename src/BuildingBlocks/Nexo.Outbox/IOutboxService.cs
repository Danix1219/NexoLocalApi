using System.Text.Json;

namespace Nexo.Outbox;

/// <summary>Encapsula serializacion y encolado para que el dominio no conozca transporte.</summary>
public interface IOutboxService
{
    Task EnqueueAsync<T>(T payload, string type, CancellationToken cancellationToken = default)
        where T : class;
}

public sealed class OutboxService(IOutboxRepository repository, TimeProvider? timeProvider = null) : IOutboxService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task EnqueueAsync<T>(T payload, string type, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        var message = new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(payload, payload.GetType()),
            OccurredAt = _timeProvider.GetUtcNow(),
        };

        await repository.EnqueueAsync(message, cancellationToken);
    }
}
