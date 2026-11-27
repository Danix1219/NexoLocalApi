namespace Nexo.Common.Accounts;

/// <summary>Cuenta sobre la que actúa la baja. El perfil de prestador lo resuelve quien lo posee.</summary>
public sealed record AccountSubject(Guid AccountId, Guid? ProviderId);

/// <summary>
/// Parte de la baja de cuenta que le toca a un dominio, siempre sobre su propia base (nv-12).
/// Las tres operaciones son idempotentes: tras un fallo se repiten completas.
/// </summary>
public interface IAccountDataLifecycle
{
    /// <summary>Nombre estable; se guarda como acuse de que el dominio terminó de borrar.</summary>
    string Domain { get; }

    /// <summary>Al pedir la baja: deja de mostrarse y se detiene lo pendiente.</summary>
    Task SuspendAsync(AccountSubject subject, DateTime now, CancellationToken ct);

    /// <summary>Al cancelar dentro del plazo: vuelve lo que se puede recuperar.</summary>
    Task RestoreAsync(AccountSubject subject, DateTime now, CancellationToken ct);

    /// <summary>Vencido el plazo: borrado definitivo. No tiene vuelta atrás.</summary>
    Task EraseAsync(AccountSubject subject, DateTime now, CancellationToken ct);
}

public interface IAccountSubjectResolver
{
    Task<Guid?> ProviderIdAsync(Guid accountId, CancellationToken ct);
}
