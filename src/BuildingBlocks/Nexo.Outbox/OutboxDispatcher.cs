using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexo.Outbox;

/// <summary>Worker unico: reclama, entrega y marca; nunca procesa dos veces el mismo MessageId.</summary>
public sealed class OutboxDispatcher : BackgroundService
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeProvider _timeProvider;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcher> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<int> DispatchOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var handler = scope.ServiceProvider.GetRequiredService<IOutboxHandler>();
        var pending = await repository.GetPendingAsync(BatchSize, cancellationToken);
        var delivered = 0;

        foreach (var message in pending)
        {
            var now = _timeProvider.GetUtcNow();
            if (!await repository.TryBeginProcessingAsync(message.MessageId, now, StaleAfter, cancellationToken))
            {
                continue;
            }

            try
            {
                await handler.HandleAsync(message, cancellationToken);
                await repository.MarkProcessedAsync(message.MessageId, _timeProvider.GetUtcNow(), cancellationToken);
                delivered++;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fallo al entregar el mensaje de outbox {MessageId}.", message.MessageId);
                await repository.MarkFailedAsync(
                    message.MessageId,
                    exception.Message,
                    _timeProvider.GetUtcNow(),
                    cancellationToken);
            }
        }

        return delivered;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchOnceAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "El despachador de outbox fallo en el ciclo actual.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
