using System.Security.Cryptography;
using NexoLocal.Identity.Application.Ports;

namespace NexoLocal.Identity.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>PBKDF2-SHA256 de la BCL. Sin paquete externo y con sal por cuenta.</summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

/// <summary>
/// Tokens opacos: el valor solo existe en la respuesta y en la memoria del cliente;
/// la base guarda unicamente su SHA-256.
/// </summary>
public sealed class SecretGenerator : ISecretGenerator
{
    private const int TokenBytes = 32;

    public string NewId() => Guid.NewGuid().ToString("n");

    public string NewToken() => Base64Url(RandomNumberGenerator.GetBytes(TokenBytes));

    public string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
