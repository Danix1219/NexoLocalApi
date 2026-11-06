using Nexo.Identity.Domain.Verification;

namespace Nexo.Identity.Unit;

[Trait("Category", "Unit")]
public sealed class EmailChallengeTests
{
    private static readonly DateTime Ahora = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static EmailChallenge Desafio(int intentos = 0, DateTime? consumido = null, int minutos = 15) => new()
    {
        Id = "c1",
        Email = "ana@ejemplo.mx",
        CodeHash = "hash",
        Purpose = ChallengePurpose.Registration,
        Attempts = intentos,
        CreatedAt = Ahora,
        ExpiresAt = Ahora.AddMinutes(minutos),
        ConsumedAt = consumido
    };

    [Fact]
    public void CanAttempt_true_con_desafio_fresco_y_sin_intentos() =>
        Assert.True(Desafio().CanAttempt(Ahora));

    [Fact]
    public void CanAttempt_false_si_ya_se_consumio() =>
        // Un solo uso: tras consumirlo no se puede reintentar aunque queden intentos.
        Assert.False(Desafio(consumido: Ahora).CanAttempt(Ahora));

    [Fact]
    public void CanAttempt_false_si_expiro() =>
        Assert.False(Desafio(minutos: -1).CanAttempt(Ahora));

    [Fact]
    public void CanAttempt_permite_hasta_MaxAttempts_y_bloquea_despues()
    {
        // Frontera del limite de fuerza bruta: el intento numero 5 es el ultimo permitido.
        Assert.True(Desafio(intentos: EmailChallenge.MaxAttempts - 1).CanAttempt(Ahora));
        Assert.False(Desafio(intentos: EmailChallenge.MaxAttempts).CanAttempt(Ahora));
        Assert.False(Desafio(intentos: EmailChallenge.MaxAttempts + 1).CanAttempt(Ahora));
    }

    private static EmailProof Prueba(ChallengePurpose proposito, DateTime? consumido = null, int minutos = 10) => new()
    {
        Id = "p1",
        Email = "ana@ejemplo.mx",
        TokenHash = "hash",
        Purpose = proposito,
        ExpiresAt = Ahora.AddMinutes(minutos),
        ConsumedAt = consumido
    };

    [Fact]
    public void EmailProof_usable_con_el_proposito_correcto() =>
        Assert.True(Prueba(ChallengePurpose.Registration).IsUsable(Ahora, ChallengePurpose.Registration));

    [Fact]
    public void EmailProof_no_sirve_para_otro_proposito() =>
        // Una prueba de recuperacion no puede usarse para registrar: son flujos distintos.
        Assert.False(Prueba(ChallengePurpose.Recovery).IsUsable(Ahora, ChallengePurpose.Registration));

    [Fact]
    public void EmailProof_no_se_reutiliza() =>
        Assert.False(Prueba(ChallengePurpose.Registration, consumido: Ahora).IsUsable(Ahora, ChallengePurpose.Registration));

    [Fact]
    public void EmailProof_no_sirve_vencida() =>
        Assert.False(Prueba(ChallengePurpose.Registration, minutos: -1).IsUsable(Ahora, ChallengePurpose.Registration));
}
