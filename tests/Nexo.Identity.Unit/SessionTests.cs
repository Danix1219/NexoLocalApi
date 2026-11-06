using Nexo.Identity.Domain.Sessions;

namespace Nexo.Identity.Unit;

[Trait("Category", "Unit")]
public sealed class SessionTests
{
    private static readonly DateTime Ahora = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static Session Sesion(DateTime expira, DateTime? revocada = null) => new()
    {
        Id = "s1",
        AccountId = "a1",
        DeviceId = "d1",
        FamilyId = "f1",
        AccessTokenHash = "ah",
        RefreshTokenHash = "rh",
        AccessExpiresAt = Ahora.AddMinutes(15),
        RefreshExpiresAt = expira,
        CreatedAt = Ahora,
        RevokedAt = revocada
    };

    [Fact]
    public void IsActive_true_mientras_no_este_revocada_ni_vencida() =>
        Assert.True(Sesion(Ahora.AddDays(30)).IsActive(Ahora));

    [Fact]
    public void IsActive_false_si_fue_revocada() =>
        Assert.False(Sesion(Ahora.AddDays(30), revocada: Ahora.AddMinutes(-1)).IsActive(Ahora));

    [Fact]
    public void IsActive_false_si_el_refresh_vencio() =>
        Assert.False(Sesion(Ahora.AddMinutes(-1)).IsActive(Ahora));

    [Fact]
    public void IsActive_false_justo_al_instante_de_vencer() =>
        // El limite es estricto: en el instante exacto ya no sirve.
        Assert.False(Sesion(Ahora).IsActive(Ahora));
}
