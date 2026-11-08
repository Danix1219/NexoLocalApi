using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Application.Features;

public sealed record SearchLocation(double Lat, double Lng, string ZoneLabel, int RadiusKm);

public sealed record RegisterAccount(
    string? VerificationTicket,
    string? Name,
    string? Password,
    bool AcceptedTerms,
    SearchLocation? Location,
    string? Intent);

public sealed record AccountCreated(
    Guid AccountId,
    string Email,
    string Name,
    string CurrentMode,
    bool HasProvider,
    string NextStep);

/// <summary>
/// Alta de cuenta consumiendo el ticket del correo verificado (01-auth/post-accounts.md).
/// Toda cuenta nace en Modo Cliente; el perfil profesional se crea despues.
/// </summary>
public sealed class AccountRegistrationService(
    IAccountRepository accounts,
    IVerificationTicketStore tickets,
    IPasswordHasher hasher,
    IIdentityEvents events,
    IClock clock)
{
    public const string TermsVersion = "2026-10-20";
    private static readonly int[] AllowedRadius = [2, 5, 10, 20];

    public async Task<Result<AccountCreated>> CreateAsync(RegisterAccount request, CancellationToken ct)
    {
        if (Account.ValidateName(request.Name) is { } nameProblem)
        {
            return Result<AccountCreated>.Failure(NexoCodes.ValidationFailed, nameProblem);
        }

        if (PasswordPolicy.Describe(request.Password) is { } passwordProblem)
        {
            return Result<AccountCreated>.Failure(NexoCodes.PasswordTooWeak, passwordProblem);
        }

        if (!request.AcceptedTerms)
        {
            return Result<AccountCreated>.Failure(
                NexoCodes.ValidationFailed, "Hay que aceptar los terminos y el aviso de privacidad.");
        }

        if (ValidateLocation(request.Location) is { } locationProblem)
        {
            return Result<AccountCreated>.Failure(NexoCodes.ValidationFailed, locationProblem);
        }

        var ticket = await tickets.ConsumeAsync(request.VerificationTicket ?? string.Empty, ct);
        if (ticket is null || ticket.Purpose != VerificationPurpose.Signup)
        {
            return Result<AccountCreated>.Failure(
                NexoCodes.EmailTicketInvalid, "La verificacion del correo caduco; repite el paso del codigo.");
        }

        var now = clock.UtcNow;
        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            Email = ticket.Email,
            PasswordHash = hasher.Hash(request.Password!),
            Name = request.Name!.Trim(),
            CurrentMode = AccountMode.Client,
            HasProvider = false,
            Status = AccountStatus.Active,
            EmailVerifiedAt = now,
            TermsAcceptedAt = now,
            TermsVersion = TermsVersion,
            CreatedAt = now,
            UpdatedAt = now
        };

        // La carrera de dos registros simultaneos la resuelve el indice unico, no una lectura previa.
        if (!await accounts.TryInsertAsync(account, ct))
        {
            return Result<AccountCreated>.Failure(
                NexoCodes.EmailAlreadyRegistered, "Ese correo ya tiene una cuenta. Inicia sesion.");
        }

        var location = request.Location!;
        await events.AccountCreatedAsync(
            account.Id, account.Email, account.Name, location.Lat, location.Lng, location.ZoneLabel, location.RadiusKm, ct);
        await events.AuditAsync("account.created", account.Id, null, ct);

        // `intent` solo dice a la app que pantalla abrir; no concede permisos.
        var nextStep = request.Intent == AccountMode.Provider ? "createProvider" : "home";

        return Result<AccountCreated>.Success(new AccountCreated(
            account.Id, account.Email, account.Name, account.CurrentMode, account.HasProvider, nextStep));
    }

    private static string? ValidateLocation(SearchLocation? location)
    {
        if (location is null)
        {
            return "Falta la ubicacion de busqueda.";
        }

        if (location.Lat is < -90 or > 90 || location.Lng is < -180 or > 180)
        {
            return "Las coordenadas estan fuera de rango.";
        }

        if (string.IsNullOrWhiteSpace(location.ZoneLabel) || location.ZoneLabel.Length > 60)
        {
            return "La zona es obligatoria y no puede pasar de 60 caracteres.";
        }

        return AllowedRadius.Contains(location.RadiusKm)
            ? null
            : "El radio debe ser 2, 5, 10 o 20 kilometros.";
    }
}
