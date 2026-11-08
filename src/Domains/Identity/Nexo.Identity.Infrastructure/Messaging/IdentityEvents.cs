using Nexo.Identity.Application.Ports;
using Nexo.Outbox;

namespace Nexo.Identity.Infrastructure.Messaging;

public sealed record EmailCodeRequested(string Email, string Code, string Purpose);

public sealed record AccountCreatedEvent(Guid AccountId, string Email, string Name, double Lat, double Lng, string ZoneLabel, int RadiusKm);

public sealed record PasswordChangedEvent(Guid AccountId);

public sealed record AuditEvent(string Type, Guid? AccountId, string? EmailHash, DateTime OccurredAt);

/// <summary>
/// Publica los eventos de Identity en el outbox de su propia base. El envio de correo y la
/// propagacion a otros dominios los hace el worker: asi el tiempo de respuesta no delata nada
/// y ningun modulo escribe en la base de otro.
/// </summary>
public sealed class OutboxIdentityEvents(IOutboxService outbox) : IIdentityEvents
{
    public const string EmailCodeType = "identity.email_code_requested";
    public const string AccountCreatedType = "account.created";
    public const string PasswordChangedType = "password.changed";
    public const string AuditType = "identity.audit";

    public Task EmailCodeRequestedAsync(string email, string code, string purpose, CancellationToken ct) =>
        outbox.EnqueueAsync(new EmailCodeRequested(email, code, purpose), EmailCodeType, ct);

    public Task AccountCreatedAsync(
        Guid accountId, string email, string name, double lat, double lng, string zoneLabel, int radiusKm, CancellationToken ct) =>
        outbox.EnqueueAsync(
            new AccountCreatedEvent(accountId, email, name, lat, lng, zoneLabel, radiusKm), AccountCreatedType, ct);

    public Task PasswordChangedAsync(Guid accountId, CancellationToken ct) =>
        outbox.EnqueueAsync(new PasswordChangedEvent(accountId), PasswordChangedType, ct);

    public Task AuditAsync(string type, Guid? accountId, string? emailHash, CancellationToken ct) =>
        outbox.EnqueueAsync(new AuditEvent(type, accountId, emailHash, DateTime.UtcNow), AuditType, ct);
}
