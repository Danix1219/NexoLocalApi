using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Nexo.Identity.Domain.Accounts;
using Nexo.Identity.Infrastructure.Persistence;
using Nexo.Outbox;

namespace Nexo.Identity.Infrastructure.Messaging;

/// <summary>Entrega al menos una vez; el mismo Id hace inocuo repetir tras un fallo de conexion.</summary>
public sealed class AccountEventRelay(IdentityCollections collections, IOutboxRepository outbox, ILogger<AccountEventRelay> logger) : BackgroundService
{
    public async Task RelayOnceAsync(CancellationToken ct)
    {
        var accounts = await collections.Accounts.Find(Builders<Account>.Filter.SizeGt(a => a.PendingEvents, 0))
            .Limit(50).Project(a => new { a.Id, a.PendingEvents }).ToListAsync(ct);
        foreach (var account in accounts)
        {
            foreach (var pending in account.PendingEvents)
            {
                try
                {
                    await outbox.EnqueueAsync(new OutboxMessage
                    {
                        MessageId = pending.Id,
                        Type = pending.Type,
                        Payload = pending.Payload,
                        OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(pending.OccurredAt, DateTimeKind.Utc))
                    }, ct);
                }
                catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    // Otra replica lo entrego, o se perdio la confirmacion de la escritura anterior.
                }
                await collections.Accounts.UpdateOneAsync(a => a.Id == account.Id,
                    Builders<Account>.Update.PullFilter(a => a.PendingEvents, e => e.Id == pending.Id), cancellationToken: ct);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RelayOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("No se pudo transferir la intencion de cuenta al outbox; se reintentara."); }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
