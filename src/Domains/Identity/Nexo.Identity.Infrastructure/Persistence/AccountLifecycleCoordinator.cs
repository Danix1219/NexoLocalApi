using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Accounts;
using Nexo.Common.Mongo;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;
using Nexo.Identity.Infrastructure.Messaging;

namespace Nexo.Identity.Infrastructure.Persistence;

/// <summary>
/// Lleva cada baja hasta el final (nv-12, FR-DEL-001…006): ocultar al solicitarla, restaurar si se
/// cancela y borrar al vencer los 30 días. La cuenta se anonimiza al final, cuando todos los dominios
/// confirmaron; un borrado parcial nunca se presenta como terminado.
/// </summary>
public sealed class AccountLifecycleCoordinator(
    IdentityCollections collections,
    [FromKeyedServices(NexoDatabaseKeys.Identity)] IMongoDatabase database,
    IEnumerable<IAccountDataLifecycle> domains,
    IAccountSubjectResolver subjects,
    ISessionTerminator sessions,
    IClock clock,
    ILogger<AccountLifecycleCoordinator> logger)
{
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);
    private const int BatchSize = 20;

    /// <summary>Atiende hasta un lote de cuentas con trabajo pendiente y devuelve cuántas terminó.</summary>
    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var done = 0;
        var attempted = new List<Guid>();
        for (var i = 0; i < BatchSize; i++)
        {
            var account = await ClaimAsync(attempted, ct);
            if (account is null) break;
            attempted.Add(account.Id);
            try
            {
                await ProcessAsync(account, ct);
                done++;
                await ReleaseAsync(account.Id, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                // El lease vence solo y otro ciclo continúa donde quedó. Sin datos personales en el registro.
                logger.LogWarning("La baja de una cuenta quedó pendiente ({Error}); se reintentará.", error.GetType().Name);
            }
        }

        return done;
    }

    private static bool Due(Account account, DateTime now) =>
        account.Status == AccountStatus.PendingDeletion &&
        account.DeletionRequestedAt is { } requested && requested + AccountManagement.DeletionGrace <= now;

    private async Task<Account?> ClaimAsync(List<Guid> skip, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var f = Builders<Account>.Filter;
        var pending = f.Eq(a => a.Status, AccountStatus.PendingDeletion);
        var work = f.Or(
            pending & f.Ne(a => a.SuspensionDone, true),
            pending & f.Lte(a => a.DeletionRequestedAt, now - AccountManagement.DeletionGrace),
            f.Eq(a => a.Status, AccountStatus.Active) & f.Ne(a => a.SuspendedAt, null));
        // `Eq null` también coincide con los documentos anteriores, que no tienen el campo.
        var free = f.Or(f.Eq(a => a.LifecycleLeaseUntil, null), f.Lt(a => a.LifecycleLeaseUntil, now));
        return await collections.Accounts.FindOneAndUpdateAsync(
            work & free & f.Nin(a => a.Id, skip),
            Builders<Account>.Update.Set(a => a.LifecycleLeaseUntil, now + Lease),
            new FindOneAndUpdateOptions<Account> { ReturnDocument = ReturnDocument.After }, ct);
    }

    private Task ReleaseAsync(Guid id, CancellationToken ct) => collections.Accounts.UpdateOneAsync(
        a => a.Id == id, Builders<Account>.Update.Set(a => a.LifecycleLeaseUntil, null), cancellationToken: ct);

    private async Task ProcessAsync(Account account, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var subject = new AccountSubject(account.Id, await subjects.ProviderIdAsync(account.Id, ct));
        if (Due(account, now)) await EraseAsync(account, subject, now, ct);
        else if (account.Status == AccountStatus.PendingDeletion) await SuspendAsync(subject, now, ct);
        else await RestoreAsync(subject, now, ct);
    }

    private async Task SuspendAsync(AccountSubject subject, DateTime now, CancellationToken ct)
    {
        // La marca va antes de tocar nada: si se cancela a media suspensión, queda anotado que hay que restaurar.
        await collections.Accounts.UpdateOneAsync(a => a.Id == subject.AccountId,
            Builders<Account>.Update.Set(a => a.SuspendedAt, now).Set(a => a.SuspensionDone, false), cancellationToken: ct);
        foreach (var domain in domains) await domain.SuspendAsync(subject, now, ct);
        await collections.Accounts.UpdateOneAsync(a => a.Id == subject.AccountId,
            Builders<Account>.Update.Set(a => a.SuspensionDone, true), cancellationToken: ct);
    }

    private async Task RestoreAsync(AccountSubject subject, DateTime now, CancellationToken ct)
    {
        foreach (var domain in domains) await domain.RestoreAsync(subject, now, ct);
        // Si volvió a pedir la baja mientras tanto, la marca se queda y el siguiente ciclo suspende otra vez.
        var updated = await collections.Accounts.UpdateOneAsync(
            a => a.Id == subject.AccountId && a.Status == AccountStatus.Active,
            Builders<Account>.Update.Set(a => a.SuspendedAt, null).Set(a => a.SuspensionDone, false), cancellationToken: ct);
        if (updated.MatchedCount == 0)
            await collections.Accounts.UpdateOneAsync(a => a.Id == subject.AccountId,
                Builders<Account>.Update.Set(a => a.SuspensionDone, false), cancellationToken: ct);
    }

    private async Task EraseAsync(Account account, AccountSubject subject, DateTime now, CancellationToken ct)
    {
        foreach (var domain in domains.Where(d => !account.ErasedDomains.Contains(d.Domain)))
        {
            await domain.EraseAsync(subject, now, ct);
            await collections.Accounts.UpdateOneAsync(a => a.Id == account.Id,
                Builders<Account>.Update.AddToSet(a => a.ErasedDomains, domain.Domain), cancellationToken: ct);
        }

        await EraseIdentityAsync(account, now, ct);
        logger.LogInformation("Una baja de cuenta se completó en todos los dominios.");
    }

    private async Task EraseIdentityAsync(Account account, DateTime now, CancellationToken ct)
    {
        var id = new BsonBinaryData(account.Id, GuidRepresentation.Standard);
        await sessions.RevokeAllForAccountAsync(account.Id, ct);
        await collections.Devices.DeleteManyAsync(new BsonDocument("accountId", id), ct);
        await database.GetCollection<BsonDocument>("profile_shares").DeleteOneAsync(new BsonDocument("_id", id), ct);

        // Mensajes pendientes o ya enviados que aún llevan el correo o datos del perfil.
        var personal = new BsonRegularExpression(Regex.Escape(account.Email) + "|" + account.Id.ToString("D"), "i");
        await database.GetCollection<BsonDocument>("outbox").DeleteManyAsync(new BsonDocument
        {
            ["Type"] = new BsonDocument("$ne", "identity.audit"),
            ["Payload"] = personal
        }, ct);

        var audit = JsonSerializer.Serialize(new { accountId = account.Id, type = "account.deleted", at = now, previousHash = account.AuditHash });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(audit)));
        var entry = JsonSerializer.Serialize(new { accountId = account.Id, type = "account.deleted", at = now, previousHash = account.AuditHash, hash });
        var u = Builders<Account>.Update;
        await collections.Accounts.UpdateOneAsync(
            a => a.Id == account.Id && a.Status == AccountStatus.PendingDeletion,
            u.Combine(
                u.Set(a => a.Status, AccountStatus.Deleted),
                u.Set(a => a.DeletedAt, now),
                u.Set(a => a.UpdatedAt, now),
                // El correo queda libre para volver a registrarse.
                u.Set(a => a.Email, $"deleted+{account.Id:N}@nexolocal.invalid"),
                u.Set(a => a.Name, "Usuario eliminado"),
                u.Set(a => a.PasswordHash, string.Empty),
                u.Set(a => a.Phone, null),
                u.Set(a => a.PhotoUrl, null),
                u.Set(a => a.DeletionReason, null),
                u.Set(a => a.LastLoginAt, null),
                u.Set(a => a.HasProvider, false),
                u.Set(a => a.SuspendedAt, null),
                // Los avisos anteriores se descartan. Quedan la auditoría, sin datos personales, y el aviso
                // final, que pierde la dirección en cuanto se envía.
                u.Set(a => a.PendingEvents, new List<PendingAccountEvent>
                {
                    new(Guid.CreateVersion7(), "identity.audit", entry, now),
                    new(Guid.CreateVersion7(), AccountDeletedNotice.Type,
                        JsonSerializer.Serialize(new AccountDeletedNotice(account.Email)), now)
                }),
                u.Set(a => a.AuditHash, hash),
                u.Inc(a => a.AuthEpoch, 1),
                u.Inc(a => a.Revision, 1)),
            cancellationToken: ct);
    }
}
