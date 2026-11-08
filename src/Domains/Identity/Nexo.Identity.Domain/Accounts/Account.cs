namespace Nexo.Identity.Domain.Accounts;

/// <summary>Estados de la cuenta. Se guardan como slug, no como entero, para que el dump sea legible.</summary>
public static class AccountStatus
{
    public const string Active = "active";
    public const string PendingDeletion = "pendingDeletion";
    public const string Deleted = "deleted";
}

/// <summary>Modo de uso de la app. El modo no concede permisos: cada endpoint verifica el recurso.</summary>
public static class AccountMode
{
    public const string Client = "client";
    public const string Provider = "provider";
}

/// <summary>
/// Cuenta de una persona. La identidad es el **correo**; el telefono es un dato opcional del perfil
/// (diccionario de tipos, seccion 2).
/// </summary>
public sealed class Account
{
    public const int NameMaxLength = 80;
    public const int NameMinLength = 2;

    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; }
    public required string CurrentMode { get; set; }
    public required bool HasProvider { get; set; }
    public required string Status { get; set; }
    public required DateTime EmailVerifiedAt { get; init; }
    public required DateTime TermsAcceptedAt { get; init; }
    public required string TermsVersion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public bool IsDeleted => Status == AccountStatus.Deleted;

    /// <summary>Una cuenta en baja puede recuperar contrasena: es la via para entrar y cancelarla.</summary>
    public bool CanSignIn => Status is AccountStatus.Active or AccountStatus.PendingDeletion;

    public static string? ValidateName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length < NameMinLength)
        {
            return $"El nombre necesita al menos {NameMinLength} caracteres.";
        }

        if (trimmed.Length > NameMaxLength)
        {
            return $"El nombre no puede pasar de {NameMaxLength} caracteres.";
        }

        // Ni solo espacios ni solo digitos: un nombre asi no identifica a nadie.
        return trimmed.All(char.IsDigit) ? "El nombre no puede ser solo numeros." : null;
    }
}

/// <summary>Normalizacion y validacion del correo, que es la llave unica de la cuenta.</summary>
public static class EmailAddress
{
    public const int MaxLength = 254;

    /// <summary>Recorta y pasa a minusculas invariantes: dos escrituras del mismo buzon son la misma cuenta.</summary>
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Forma minima de RFC 5321. No comprueba que el buzon exista; de eso se encarga el codigo.</summary>
    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxLength || email.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
        {
            return false;
        }

        var local = email[..at];
        var domain = email[(at + 1)..];
        if (local.Length > 64 || domain.Length > 255 || domain.StartsWith('.') || domain.EndsWith('.'))
        {
            return false;
        }

        var dot = domain.LastIndexOf('.');
        // Exige un TLD de al menos dos letras: "ana@localhost" no es una direccion entregable.
        return dot > 0 && domain.Length - dot - 1 >= 2 && !domain.Contains("..", StringComparison.Ordinal);
    }
}

/// <summary>Regla `pwOK` de la maqueta: 8 a 64 caracteres, con al menos una letra y un numero.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 64;

    public static bool IsStrongEnough(string? password) => Describe(password) is null;

    /// <summary>Devuelve el motivo del rechazo, o null si cumple.</summary>
    public static string? Describe(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return $"La contrasena necesita al menos {MinLength} caracteres.";
        }

        if (password.Length > MaxLength)
        {
            return $"La contrasena no puede pasar de {MaxLength} caracteres.";
        }

        if (!password.Any(char.IsLetter))
        {
            return "La contrasena necesita al menos una letra.";
        }

        return password.Any(char.IsDigit) ? null : "La contrasena necesita al menos un numero.";
    }
}

/// <summary>Proposito del codigo que se envia al correo; el ticket hereda el proposito y no se cruza.</summary>
public static class VerificationPurpose
{
    public const string Signup = "signup";
    public const string ChangeEmail = "changeEmail";
    public const string ResetPassword = "resetPassword";

    public static bool IsPublicRequest(string? purpose) =>
        purpose is Signup or ChangeEmail;

    public static bool Exists(string? purpose) =>
        purpose is Signup or ChangeEmail or ResetPassword;
}
