using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Application.Ports;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IAccountRepository
{
    Task<Account?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);

    Task<Account?> FindByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Devuelve false si el correo ya existe: la unicidad la decide el indice, no una lectura previa.</summary>
    Task<bool> TryInsertAsync(Account account, CancellationToken ct);

    Task UpdatePasswordAsync(Guid accountId, string passwordHash, DateTime changedAt, CancellationToken ct);

    Task UpdateLastLoginAsync(Guid accountId, DateTime at, CancellationToken ct);
}

/// <summary>Resultado de comprobar el codigo de 6 digitos.</summary>
public enum CodeCheck
{
    Ok,
    Invalid,
    Expired,
    TooManyAttempts
}

public sealed record StoredCode(string Purpose);

/// <summary>
/// Codigos de verificacion en Redis. El codigo en claro no se guarda nunca: solo su HMAC.
/// </summary>
public interface IEmailVerificationStore
{
    /// <summary>Abre la ventana de reenvio de 60 s; false si ya hay una abierta.</summary>
    Task<bool> TryOpenResendWindowAsync(string emailHash, TimeSpan window, CancellationToken ct);

    Task SaveCodeAsync(string emailHash, string codeHmac, string purpose, TimeSpan ttl, CancellationToken ct);

    /// <summary>Comprueba y consume: al acertar borra la clave, al fallar incrementa los intentos.</summary>
    Task<(CodeCheck Result, StoredCode? Code)> CheckAndConsumeAsync(
        string emailHash, string codeHmac, int maxAttempts, CancellationToken ct);
}

public sealed record VerificationTicket(string Email, string Purpose);

public interface IVerificationTicketStore
{
    Task<string> IssueAsync(string email, string purpose, TimeSpan ttl, CancellationToken ct);

    /// <summary>Un solo uso: leer y borrar en la misma operacion.</summary>
    Task<VerificationTicket?> ConsumeAsync(string ticket, CancellationToken ct);
}

/// <summary>Cuota exacta de la politica `otp`, compartida por registro y recuperacion.</summary>
public interface IOtpQuota
{
    Task<bool> TryConsumeAsync(string emailHash, string ip, CancellationToken ct);
}

/// <summary>Bloqueo por intentos fallidos de inicio de sesion; aparte del rate limit.</summary>
public interface ILoginLockStore
{
    Task<TimeSpan?> GetLockAsync(string emailHash, CancellationToken ct);

    /// <summary>Suma un fallo y devuelve el bloqueo si se alcanzo el tope.</summary>
    Task<TimeSpan?> RegisterFailureAsync(string emailHash, int maxAttempts, TimeSpan lockFor, CancellationToken ct);

    Task ClearAsync(string emailHash, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

public interface IVerificationCodeFactory
{
    /// <summary>Seis digitos con generador criptografico.</summary>
    string NewCode();

    /// <summary>HMAC del codigo; una fuga de Redis no permite iniciar sesion.</summary>
    string Protect(string code);
}

/// <summary>Publicacion de eventos de dominio; el adaptador los pone en el outbox.</summary>
public interface IIdentityEvents
{
    Task EmailCodeRequestedAsync(string email, string code, string purpose, CancellationToken ct);

    Task AccountCreatedAsync(Guid accountId, string email, string name, double lat, double lng, string zoneLabel, int radiusKm, CancellationToken ct);

    Task PasswordChangedAsync(Guid accountId, CancellationToken ct);

    Task AuditAsync(string type, Guid? accountId, string? emailHash, CancellationToken ct);
}

/// <summary>Cierre de sesiones: revoca en el servidor de tokens y en la lista negra de `jti`.</summary>
public interface ISessionTerminator
{
    Task RevokeFamilyAsync(string refreshToken, CancellationToken ct);

    Task RevokeAllForAccountAsync(Guid accountId, CancellationToken ct);

    Task BlacklistAccessTokenAsync(string jti, TimeSpan remaining, CancellationToken ct);
}

/// <summary>Baja del dispositivo para que no sigan llegando notificaciones tras cerrar sesion.</summary>
public interface IDeviceRegistry
{
    Task RemoveAsync(Guid accountId, string? fcmToken, bool allDevices, CancellationToken ct);
}
