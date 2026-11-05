namespace NexoLocal.Identity.Domain.Sessions;

/// <summary>Sesion de un dispositivo. El refresh rota por familia: un replay revoca la familia entera (ep-05).</summary>
public sealed class Session
{
    public required string Id { get; init; }
    public required string AccountId { get; init; }
    public required string DeviceId { get; init; }
    public required string FamilyId { get; init; }
    public required string AccessTokenHash { get; set; }
    public required string RefreshTokenHash { get; set; }

    /// <summary>Refresh inmediatamente anterior. Sin el, un replay no se distingue de un token inventado.</summary>
    public string? PreviousRefreshTokenHash { get; set; }
    public DateTime AccessExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && RefreshExpiresAt > now;
}
