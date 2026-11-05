namespace NexoLocal.Identity.Domain.Accounts;

/// <summary>Cuenta unica de S01. El identificador es el correo normalizado (ep-02).</summary>
public sealed class Account
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public required string ConsentVersion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Normaliza para la llave unica: sin espacios y en minusculas invariantes.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Validacion minima de forma; no comprueba existencia del buzon.</summary>
    public static bool IsEmailShaped(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
        {
            return false;
        }

        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
        {
            return false;
        }

        var domain = email[(at + 1)..];
        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.') && !email.Contains(' ');
    }
}
