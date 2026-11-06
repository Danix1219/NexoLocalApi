namespace Nexo.Identity.Domain.Verification;

public enum ChallengePurpose
{
    Registration,
    Recovery
}

/// <summary>Desafio de correo de un uso (ep-03). El codigo solo vive hasheado y nunca sale en una respuesta.</summary>
public sealed class EmailChallenge
{
    public const int MaxAttempts = 5;

    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string CodeHash { get; init; }
    public required ChallengePurpose Purpose { get; init; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? ConsumedAt { get; set; }

    public bool CanAttempt(DateTime now) => ConsumedAt is null && Attempts < MaxAttempts && ExpiresAt > now;
}

/// <summary>Prueba de control del correo: un uso, corta duracion y proposito fijo.</summary>
public sealed class EmailProof
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string TokenHash { get; init; }
    public required ChallengePurpose Purpose { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? ConsumedAt { get; set; }

    public bool IsUsable(DateTime now, ChallengePurpose expected) =>
        ConsumedAt is null && ExpiresAt > now && Purpose == expected;
}
