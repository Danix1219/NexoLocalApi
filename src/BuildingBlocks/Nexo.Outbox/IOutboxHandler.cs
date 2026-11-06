using Microsoft.Extensions.Logging;

namespace Nexo.Outbox;

/// <summary>Consumidor logico del outbox; debe ser idempotente por MessageId.</summary>
public interface IOutboxHandler
{
    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

/// <summary>Handler por defecto mientras cada dominio registra el suyo.</summary>
public sealed class LoggingOutboxHandler(ILogger<LoggingOutboxHandler> logger) : IOutboxHandler
{
    public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Outbox {MessageId} de tipo {Type} entregado a un handler de registro; el dominio aun no registra consumidor.",
            message.MessageId,
            message.Type);
        return Task.CompletedTask;
    }
}
