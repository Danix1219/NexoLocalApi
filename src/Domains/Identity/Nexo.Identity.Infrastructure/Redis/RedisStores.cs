using System.Security.Cryptography;
using Nexo.Common.RateLimiting;
using Nexo.Identity.Application.Ports;
using StackExchange.Redis;

namespace Nexo.Identity.Infrastructure.Redis;

/// <summary>
/// Codigos de verificacion en Redis (paginacion-y-cache.md seccion 3). El codigo en claro nunca
/// se guarda; la clave caduca sola y un envio nuevo sobrescribe el anterior.
/// </summary>
public sealed class RedisEmailVerificationStore(IConnectionMultiplexer redis) : IEmailVerificationStore
{
    private static string CodeKey(string emailHash) => $"nexo:otp:email:{emailHash}";

    private static string ResendKey(string emailHash) => $"nexo:otp:resend:{emailHash}";

    public async Task<bool> TryOpenResendWindowAsync(string emailHash, TimeSpan window, CancellationToken ct) =>
        await redis.GetDatabase().StringSetAsync(ResendKey(emailHash), "1", window, When.NotExists);

    public async Task SaveCodeAsync(string emailHash, string codeHmac, string purpose, TimeSpan ttl, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var key = CodeKey(emailHash);

        await db.HashSetAsync(key,
        [
            new HashEntry("codeHash", codeHmac),
            new HashEntry("attempts", 0),
            new HashEntry("purpose", purpose),
            new HashEntry("sentAt", DateTime.UtcNow.ToString("O"))
        ]);

        await db.KeyExpireAsync(key, ttl);
    }

    public async Task<(CodeCheck Result, StoredCode? Code)> CheckAndConsumeAsync(
        string emailHash, string codeHmac, int maxAttempts, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var key = CodeKey(emailHash);
        var stored = await db.HashGetAllAsync(key);

        if (stored.Length == 0)
        {
            // Sin clave: caduco o ya se consumio. No se distingue, para no dar informacion.
            return (CodeCheck.Expired, null);
        }

        var expected = stored.FirstOrDefault(entry => entry.Name == "codeHash").Value;
        var purpose = stored.FirstOrDefault(entry => entry.Name == "purpose").Value;

        // Comparacion en tiempo constante: el tiempo de respuesta no debe filtrar el codigo.
        if (expected.HasValue && FixedTimeEquals(expected!, codeHmac))
        {
            await db.KeyDeleteAsync(key);
            return (CodeCheck.Ok, new StoredCode(purpose.HasValue ? purpose! : string.Empty));
        }

        var attempts = await db.HashIncrementAsync(key, "attempts");
        if (attempts >= maxAttempts)
        {
            // Quemado el codigo: hay que pedir otro.
            await db.KeyDeleteAsync(key);
            return (CodeCheck.TooManyAttempts, null);
        }

        return (CodeCheck.Invalid, null);
    }

    private static bool FixedTimeEquals(string left, string right) =>
        left.Length == right.Length
        && CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(left),
            System.Text.Encoding.UTF8.GetBytes(right));
}

/// <summary>Tickets de un solo uso; se leen con GETDEL para que dos peticiones no lo gasten a la vez.</summary>
public sealed class RedisVerificationTicketStore(IConnectionMultiplexer redis) : IVerificationTicketStore
{
    private static string Key(string ticket) => $"nexo:ticket:{ticket}";

    public async Task<string> IssueAsync(string email, string purpose, TimeSpan ttl, CancellationToken ct)
    {
        // Aleatorio y opaco: el ticket no lleva el correo legible, la asociacion vive en Redis.
        var ticket = "vt_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        await redis.GetDatabase().StringSetAsync(Key(ticket), $"{purpose}\u001f{email}", ttl);
        return ticket;
    }

    public async Task<VerificationTicket?> ConsumeAsync(string ticket, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return null;
        }

        var value = await redis.GetDatabase().StringGetDeleteAsync(Key(ticket));
        if (!value.HasValue)
        {
            return null;
        }

        var parts = ((string)value!).Split('\u001f', 2);
        return parts.Length == 2 ? new VerificationTicket(parts[1], parts[0]) : null;
    }
}

/// <summary>
/// Cuota exacta de la politica `otp`: 3 cada 15 min y 10 al dia, por correo y por IP.
/// Compartida por registro y recuperacion para que nadie duplique su cuota alternandolos.
/// </summary>
public sealed class RedisOtpQuota(IConnectionMultiplexer redis) : IOtpQuota
{
    public async Task<bool> TryConsumeAsync(string emailHash, string ip, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var now = DateTime.UtcNow;
        var partition = $"{emailHash}:{ip}";

        var shortKey = NexoRateLimits.Key(
            NexoRateLimits.Otp, partition, NexoRateLimits.WindowStamp(now, NexoRateLimits.OtpWindow));
        var dayKey = NexoRateLimits.Key(
            NexoRateLimits.Otp, partition, NexoRateLimits.WindowStamp(now, TimeSpan.FromDays(1)));

        var shortCount = await Increment(db, shortKey, NexoRateLimits.OtpWindow);
        var dayCount = await Increment(db, dayKey, TimeSpan.FromDays(1));

        return shortCount <= NexoRateLimits.OtpPerWindow && dayCount <= NexoRateLimits.OtpPerDay;
    }

    private static async Task<long> Increment(IDatabase db, string key, TimeSpan window)
    {
        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(key, window);
        }

        return count;
    }
}

/// <summary>Bloqueo por intentos fallidos: 5 fallos, 15 minutos. Temporal, nunca permanente.</summary>
public sealed class RedisLoginLockStore(IConnectionMultiplexer redis) : ILoginLockStore
{
    private static string Key(string emailHash) => $"nexo:lock:{emailHash}";

    public async Task<TimeSpan?> GetLockAsync(string emailHash, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var key = Key(emailHash);
        var value = await db.StringGetAsync(key);

        if (!value.HasValue || (long)value < 5)
        {
            return null;
        }

        return await db.KeyTimeToLiveAsync(key);
    }

    public async Task<TimeSpan?> RegisterFailureAsync(
        string emailHash, int maxAttempts, TimeSpan lockFor, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var key = Key(emailHash);
        var count = await db.StringIncrementAsync(key);

        // La ventana se refresca con cada fallo: cuenta intentos consecutivos, no desde el primero.
        await db.KeyExpireAsync(key, lockFor);

        return count >= maxAttempts ? lockFor : null;
    }

    public Task ClearAsync(string emailHash, CancellationToken ct) =>
        redis.GetDatabase().KeyDeleteAsync(Key(emailHash));
}
