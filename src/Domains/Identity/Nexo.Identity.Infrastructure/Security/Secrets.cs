using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using Nexo.Identity.Application.Ports;

namespace Nexo.Identity.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>Llaves de identidad. En produccion llegan de Key Vault; aqui, de configuracion protegida.</summary>
public sealed class IdentitySecretsOptions
{
    public const string SectionName = "Identity";

    /// <summary>Llave HMAC del codigo de verificacion, en Base64. Nunca se versiona.</summary>
    public string CodeHmacKey { get; set; } = string.Empty;

    public string? Validate() => string.IsNullOrWhiteSpace(CodeHmacKey)
        ? $"{SectionName}:CodeHmacKey ausente. Cargarla por user-secrets o variable {SectionName}__CodeHmacKey."
        : null;
}

/// <summary>
/// Argon2id con los parametros de la spec: 4 iteraciones, 64 MB, paralelismo 2, sal de 16 bytes.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 4;
    private const int MemoryKib = 65536;
    private const int Parallelism = 2;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string Prefix = "argon2id";

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt);
        return $"${Prefix}$v=19$m={MemoryKib},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        // Partes: ["", "argon2id", "v=19", "m=...,t=...,p=...", salt, hash]
        var parts = passwordHash.Split('$');
        if (parts.Length != 6 || parts[1] != Prefix)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[4]);
            expected = Convert.FromBase64String(parts[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (!TryReadParameters(parts[3], out var memory, out var iterations, out var parallelism))
        {
            return false;
        }

        var actual = Derive(password, salt, memory, iterations, parallelism, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryReadParameters(string segment, out int memory, out int iterations, out int parallelism)
    {
        memory = MemoryKib;
        iterations = Iterations;
        parallelism = Parallelism;

        foreach (var pair in segment.Split(','))
        {
            var kv = pair.Split('=');
            if (kv.Length != 2 || !int.TryParse(kv[1], out var value) || value <= 0)
            {
                return false;
            }

            switch (kv[0])
            {
                case "m": memory = value; break;
                case "t": iterations = value; break;
                case "p": parallelism = value; break;
                default: return false;
            }
        }

        return true;
    }

    private static byte[] Derive(
        string password,
        byte[] salt,
        int memory = MemoryKib,
        int iterations = Iterations,
        int parallelism = Parallelism,
        int size = HashSize)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memory,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };

        return argon.GetBytes(size);
    }
}

/// <summary>
/// Codigo de 6 digitos con generador criptografico. Solo se guarda su HMAC: una fuga de Redis
/// no permite iniciar sesion.
/// </summary>
public sealed class VerificationCodeFactory(IOptions<IdentitySecretsOptions> options) : IVerificationCodeFactory
{
    private readonly byte[] key = Convert.FromBase64String(options.Value.CodeHmacKey);

    public string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string Protect(string code) =>
        Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(code))).ToLowerInvariant();
}
