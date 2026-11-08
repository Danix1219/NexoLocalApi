using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Outbox;

namespace Nexo.Common.Unit;

public sealed class OutboxIdempotencyTests
{
    [Fact]
    public async Task Dispatcher_delivers_each_message_once()
    {
        var repository = new InMemoryOutboxRepository();
        var handler = new CountingHandler();
        await repository.EnqueueAsync(NewMessage());

        using var provider = BuildProvider(repository, handler);
        var dispatcher = NewDispatcher(provider);

        Assert.Equal(1, await dispatcher.DispatchOnceAsync());
        Assert.Equal(0, await dispatcher.DispatchOnceAsync());
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task Concurrent_dispatchers_claim_the_same_message_once()
    {
        var repository = new InMemoryOutboxRepository();
        var handler = new CountingHandler();
        await repository.EnqueueAsync(NewMessage());

        using var provider = BuildProvider(repository, handler);
        var first = NewDispatcher(provider).DispatchOnceAsync();
        var second = NewDispatcher(provider).DispatchOnceAsync();

        var delivered = await Task.WhenAll(first, second);

        Assert.Equal(1, delivered.Sum());
        Assert.Equal(1, handler.Count);
    }

    private static OutboxMessage NewMessage() => new()
    {
        MessageId = Guid.NewGuid(),
        Type = "identity.session.created",
        Payload = "{}",
        OccurredAt = TimeProvider.System.GetUtcNow(),
    };

    private static ServiceProvider BuildProvider(IOutboxRepository repository, IOutboxHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(handler);
        services.AddSingleton(TimeProvider.System);
        return services.BuildServiceProvider();
    }

    private static OutboxDispatcher NewDispatcher(IServiceProvider provider) => new(
        provider.GetRequiredService<IServiceScopeFactory>(),
        NullLogger<OutboxDispatcher>.Instance);

    private sealed class CountingHandler : IOutboxHandler
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOutboxRepository : IOutboxRepository
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, OutboxMessage> _messages = [];

        public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _messages[message.MessageId] = message;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int max, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                IReadOnlyList<OutboxMessage> pending = _messages.Values
                    .Where(message => message.ProcessedAt is null)
                    .OrderBy(message => message.OccurredAt)
                    .Take(max)
                    .ToList();
                return Task.FromResult(pending);
            }
        }

        public Task<bool> TryBeginProcessingAsync(
            Guid messageId,
            DateTimeOffset now,
            TimeSpan staleAfter,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (!_messages.TryGetValue(messageId, out var message) || message.ProcessedAt is not null)
                {
                    return Task.FromResult(false);
                }

                if (message.ProcessingStartedAt is not null && message.ProcessingStartedAt >= now.Subtract(staleAfter))
                {
                    return Task.FromResult(false);
                }

                message.ProcessingStartedAt = now;
                return Task.FromResult(true);
            }
        }

        public Task MarkProcessedAsync(Guid messageId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_messages.TryGetValue(messageId, out var message))
                {
                    message.ProcessedAt = now;
                    message.ProcessingStartedAt = null;
                }
            }

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            Guid messageId,
            string error,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_messages.TryGetValue(messageId, out var message))
                {
                    message.ProcessingStartedAt = null;
                    message.LastError = error;
                    message.Attempts++;
                }
            }

            return Task.CompletedTask;
        }
    }
}
