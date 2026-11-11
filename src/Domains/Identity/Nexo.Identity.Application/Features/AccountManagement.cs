using System.Text.RegularExpressions;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Application.Features;

public sealed record AccountPatch(bool HasName, string? Name, bool HasPhoto, Guid? PhotoUploadId);
public sealed record ModeChanged(string CurrentMode, bool HasProvider, bool TokenRefreshRequired, string Toast);
public sealed record PhoneDeclared(string Phone, string PhoneVerificationStatus);
public sealed record DeletionRequested(string Status, DateTime DeletionRequestedAt, DateTime EffectiveAt, int DaysRemaining, bool CanCancel, bool AlreadyPending);
public sealed record DeletionCancelled(string Status, DateTime CancelledAt, string RestorationStatus);

/// <summary>Casos de cuenta propia. El estado se relee; un claim de modo no concede permisos.</summary>
public sealed partial class AccountManagement(
    IAccountManagementStore store, IAccountRepository accounts, IAccountProfileSafety safety,
    IDeviceRegistry devices, IPasswordHasher passwords,
    ISessionTerminator sessions, IIdentityEvents events, IClock clock)
{
    public static readonly TimeSpan DeletionGrace = TimeSpan.FromDays(30);

    public async Task<Result<AccountView>> GetAsync(Guid id, CancellationToken ct)
    {
        var account = await store.ReadAsync(id, ct);
        return account is null || account.Status == AccountStatus.Deleted
            ? Result<AccountView>.Failure(NexoCodes.ResourceNotFound, "La cuenta no existe.")
            : Result<AccountView>.Success(account);
    }

    public async Task<Result<AccountView>> PatchAsync(Guid id, AccountPatch patch, CancellationToken ct)
    {
        if ((!patch.HasName && !patch.HasPhoto) ||
            (patch.HasName && Account.ValidateName(patch.Name) is not null) || patch.PhotoUploadId == Guid.Empty)
        {
            return Result<AccountView>.Failure(NexoCodes.ValidationFailed, "Revisa los campos de la cuenta.");
        }

        var current = await ActiveAsync(id, ct);
        if (current.IsFailure) return Result<AccountView>.Failure(current.Error!);
        var account = current.Value!;
        var name = patch.HasName ? patch.Name!.Trim() : null;
        if (name is not null && name != account.Name)
        {
            var allowed = await safety.CheckNameAsync(name, ct);
            if (allowed.IsFailure) return Result<AccountView>.Failure(allowed.Error!);
            if (allowed.Value != true) return Result<AccountView>.Failure(NexoCodes.ContentInappropriate, "El nombre no puede publicarse.");
        }

        string? url = null;
        if (patch.PhotoUploadId is { } uploadId)
        {
            var photo = await safety.ResolvePhotoAsync(id, uploadId, ct);
            if (photo.IsFailure) return Result<AccountView>.Failure(photo.Error!);
            url = photo.Value;
        }

        if ((!patch.HasName || name == account.Name) && (!patch.HasPhoto || url == account.PhotoUrl))
            return current;

        if (!await store.ApplyAsync(account, new AccountWrite("account.profile_updated", name, patch.HasPhoto, url), clock.UtcNow, ct))
            return Conflict<AccountView>();
        return Result<AccountView>.Success(account with { Name = name ?? account.Name, PhotoUrl = patch.HasPhoto ? url : account.PhotoUrl });
    }

    public async Task<Result<ModeChanged>> ChangeModeAsync(Guid id, string? mode, string? tokenMode, CancellationToken ct)
    {
        if (mode is not (AccountMode.Client or AccountMode.Provider))
            return Result<ModeChanged>.Failure(NexoCodes.ValidationFailed, "El modo no es valido.");
        var current = await ActiveAsync(id, ct);
        if (current.IsFailure) return Result<ModeChanged>.Failure(current.Error!);
        var account = current.Value!;
        if (mode == AccountMode.Provider && !account.HasProvider)
            return Result<ModeChanged>.Failure(NexoCodes.ProviderProfileRequired, "Crea primero tu perfil profesional.");
        if (mode != account.CurrentMode &&
            !await store.ApplyAsync(account, new AccountWrite("account.mode_changed", Mode: mode), clock.UtcNow, ct))
            return Conflict<ModeChanged>();
        return Result<ModeChanged>.Success(new ModeChanged(mode, account.HasProvider, tokenMode != mode,
            mode == AccountMode.Provider ? "Modo Proveedor" : "Modo Cliente"));
    }

    public async Task<Result<PhoneDeclared>> SetPhoneAsync(Guid id, string? phone, CancellationToken ct)
    {
        var value = phone?.Trim();
        if (value is null || !PhoneFormat().IsMatch(value))
            return Result<PhoneDeclared>.Failure(NexoCodes.PhoneInvalid, "Escribe un celular mexicano de diez digitos.");
        var normalized = value.StartsWith("+52", StringComparison.Ordinal) ? value : "+52" + value;
        var current = await ActiveAsync(id, ct);
        if (current.IsFailure) return Result<PhoneDeclared>.Failure(current.Error!);
        if (current.Value!.EmailVerifiedAt == default)
            return Result<PhoneDeclared>.Failure(NexoCodes.AuthEmailNotVerified, "Verifica primero tu correo.");
        if (current.Value.Phone != normalized &&
            !await store.ApplyAsync(current.Value, new AccountWrite("account.phone_updated", Phone: normalized), clock.UtcNow, ct))
            return Conflict<PhoneDeclared>();
        return Result<PhoneDeclared>.Success(new PhoneDeclared(normalized, "declared"));
    }

    public async Task<Result<DeviceRegistration>> RegisterDeviceAsync(Guid id, string? token, string? platform, string? version, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096 || token.Any(char.IsWhiteSpace) || platform != "android" ||
            string.IsNullOrEmpty(version) || version.Length > 20 || !SemVer().IsMatch(version))
            return Result<DeviceRegistration>.Failure(NexoCodes.ValidationFailed, "Revisa el dispositivo y la version.");
        var current = await ActiveAsync(id, ct);
        if (current.IsFailure) return Result<DeviceRegistration>.Failure(current.Error!);
        var device = await devices.RegisterAsync(id, token, platform, version, clock.UtcNow, ct);
        if (device is null)
            return Result<DeviceRegistration>.Failure(NexoCodes.DeviceTokenOwnedByOtherAccount, "No se pudo reasignar ese dispositivo.");
        var after = await store.ReadAsync(id, ct);
        if (after is null || after.Status != AccountStatus.Active || after.AuthEpoch != current.Value!.AuthEpoch)
        {
            await devices.RemoveAsync(id, token, false, ct);
            return Result<DeviceRegistration>.Failure(NexoCodes.AccountPendingDeletion, "La cuenta cambio de estado.");
        }
        if (device.Reassigned) await events.AuditAsync("device.reassigned", id, null, ct);
        return Result<DeviceRegistration>.Success(device);
    }

    public async Task<Result<bool>> DeleteDeviceAsync(Guid id, Guid deviceId, CancellationToken ct)
    {
        if (deviceId == Guid.Empty) return Result<bool>.Failure(NexoCodes.ValidationFailed, "Identificador invalido.");
        var current = await ActiveAsync(id, ct);
        if (current.IsFailure) return Result<bool>.Failure(current.Error!);
        if (!await devices.DeleteAsync(id, deviceId, ct))
            return Result<bool>.Failure(NexoCodes.ResourceNotFound, "El dispositivo no existe.");
        await events.AuditAsync("device.removed", id, null, ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<DeletionRequested>> RequestDeletionAsync(Guid id, string? password, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(password) || password.Length is < 8 or > 64 || reason?.Length > 300)
            return Result<DeletionRequested>.Failure(NexoCodes.ValidationFailed, "Revisa la contrasena y el motivo.");
        var current = await GetAsync(id, ct);
        if (current.IsFailure) return Result<DeletionRequested>.Failure(current.Error!);
        // Solo esta accion destructiva necesita leer el hash.
        var account = await accounts.FindByIdAsync(id, ct);
        if (account is null || !passwords.Verify(password, account.PasswordHash))
            return Result<DeletionRequested>.Failure(NexoCodes.AuthInvalidCredentials, "Las credenciales no son validas.");
        // Mongo guarda milisegundos: la respuesta inicial y la repetida deben conservar la misma fecha.
        var instant = clock.UtcNow;
        var now = new DateTime(instant.Ticks - instant.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        var already = current.Value!.Status == AccountStatus.PendingDeletion;
        var requestedAt = already ? current.Value.DeletionRequestedAt : now;
        if (requestedAt is null) return Result<DeletionRequested>.Failure(NexoCodes.ServerDependencyUnavailable, "Estado de baja incompleto.");
        if (!already && !await store.ApplyAsync(current.Value,
                new AccountWrite("account.deletion_requested", Status: AccountStatus.PendingDeletion,
                    DeletionRequestedAt: now, DeletionReason: reason?.Trim()), now, ct))
            return Conflict<DeletionRequested>();
        // Reintentar una solicitud pendiente reejecuta estos efectos idempotentes si hubo un fallo.
        await devices.RemoveAsync(id, null, true, ct);
        await sessions.RevokeAllForAccountAsync(id, ct);
        var effective = requestedAt.Value + DeletionGrace;
        return Result<DeletionRequested>.Success(new DeletionRequested(AccountStatus.PendingDeletion, requestedAt.Value,
            effective, Math.Clamp((int)Math.Ceiling((effective - now).TotalDays), 0, 30), effective > now, already));
    }

    public async Task<Result<DeletionCancelled>> CancelDeletionAsync(Guid id, CancellationToken ct)
    {
        var current = await GetAsync(id, ct);
        if (current.IsFailure) return Result<DeletionCancelled>.Failure(current.Error!);
        var account = current.Value!;
        if (account.Status != AccountStatus.PendingDeletion)
            return Result<DeletionCancelled>.Failure(NexoCodes.AccountNotPendingDeletion, "No hay una baja pendiente.");
        var now = clock.UtcNow;
        if (account.DeletionRequestedAt is null || account.DeletionRequestedAt + DeletionGrace <= now)
            return Result<DeletionCancelled>.Failure(NexoCodes.AccountDeletionAlreadyExecuted, "Vencio la ventana para cancelar.");
        if (!await store.ApplyAsync(account, new AccountWrite("account.deletion_cancelled", Status: AccountStatus.Active), now, ct))
            return Conflict<DeletionCancelled>();
        return Result<DeletionCancelled>.Success(new DeletionCancelled(AccountStatus.Active, now, "queued"));
    }

    private async Task<Result<AccountView>> ActiveAsync(Guid id, CancellationToken ct)
    {
        var result = await GetAsync(id, ct);
        return result.IsSuccess && result.Value!.Status != AccountStatus.Active
            ? Result<AccountView>.Failure(NexoCodes.AccountPendingDeletion, "La cuenta esta en proceso de baja.") : result;
    }

    private static Result<T> Conflict<T>() => Result<T>.Failure(NexoCodes.AccountConcurrentUpdate, "La cuenta cambio; consulta y reintenta.");

    [GeneratedRegex(@"^(?:\+52)?[0-9]{10}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneFormat();

    [GeneratedRegex(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex SemVer();
}
