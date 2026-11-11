using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;
using Nexo.Common.Errors;

namespace Nexo.Discovery.Unit;

public sealed class ProviderRulesTests
{
    private static ProviderProfile Valid => new()
    {
        Name = "Oficio Local",
        Bio = "Reparamos fugas y tuberias del hogar.",
        Trades = ["Plomero"],
        Categories = ["plomeria"],
        Location = new(20.05, -99.34),
        RadiusKm = 5
    };
    [Fact]
    public void Difuminado_se_mantiene_entre_500_y_800_m_incluso_en_fecha_y_polos()
    {
        foreach (var point in new[] { new ProviderPoint(20.05, -99.34), new ProviderPoint(0, 179.999), new ProviderPoint(89.99, 40) })
            for (var i = 0; i < 100; i++)
            {
                var blurred = point.Blur();
                Assert.True(blurred.IsValid);
                Assert.InRange(point.DistanceKm(blurred), .499999, .800001);
            }
    }
    [Theory]
    [InlineData("22:00", "08:00", false)]
    [InlineData("08:00", "08:00", false)]
    [InlineData("8:00", "18:00", false)]
    [InlineData("08:00", "24:00", false)]
    [InlineData("08:00", "18:00", true)]
    public void Horarios_validan_formato_y_orden(string open, string close, bool valid) =>
        Assert.Equal(valid, new ProviderSchedule([new(open, close), null, null, null, null, null, null]).IsValid);
    [Fact]
    public void Horario_utiliza_lunes_y_reloj_real_de_Mexico()
    {
        var schedule = new ProviderSchedule([new("08:00", "18:00"), null, null, null, null, null, null]);
        Assert.True(schedule.IsOpen(new DateTime(2026, 11, 9, 16, 0, 0, DateTimeKind.Utc)));
        Assert.False(schedule.IsOpen(new DateTime(2026, 11, 10, 16, 0, 0, DateTimeKind.Utc)));
        Assert.False(schedule.IsOpen(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc)));
    }
    [Fact]
    public void Alta_basica_valida_y_limites_se_comprueban()
    {
        var account = new ProviderAccountState(true, true, false);
        Assert.Null(ProviderRules.Validate(Valid, "all", account));
        Assert.Equal(NexoCodes.ProviderBioTooShort, ProviderRules.Validate(Valid with { Bio = "corto" }, "all", account));
        Assert.Equal(NexoCodes.ProviderRadiusInvalid, ProviderRules.Validate(Valid with { RadiusKm = 0 }, "all", account));
        Assert.Equal(NexoCodes.ProviderCategoriesRequired, ProviderRules.Validate(Valid with { Categories = ["inventada"] }, "all", account));
        Assert.Equal(NexoCodes.ValidationFailed, ProviderRules.Validate(Valid with { Specialties = ["llaves"] }, "all", account));
    }
    [Fact]
    public void Llamada_requiere_correo_y_movil_sin_afirmar_SMS()
    {
        var p = Valid with { Channels = new(true, true) };
        Assert.Equal(NexoCodes.AuthEmailNotVerified, ProviderRules.Validate(p, "terms", new(true, false, true)));
        Assert.Equal(NexoCodes.PhoneInvalid, ProviderRules.Validate(p, "terms", new(true, true, false)));
        Assert.Null(ProviderRules.Validate(p, "terms", new(true, true, true)));
    }
    [Theory]
    [InlineData("Instalaciones eléctricas y tuberias", "electricidad")]
    [InlineData("Pintura e impermeabilizado", "pintura")]
    [InlineData("Consultoria de servicios", "otros")]
    public void Negocio_infiere_categorias_del_texto(string text, string expected) => Assert.Contains(expected, ProviderRules.Infer(text));
    [Fact]
    public void Completitud_usa_una_formula_sin_superar_cien()
    {
        Assert.Equal(35, Valid.Completeness);
        var p = Valid with
        {
            PhotoUrl = "https://example.test/a",
            CoverUrl = "https://example.test/c",
            Schedule = new([new("08:00", "18:00"), null, null, null, null, null, null]),
            Prices = [new("Visita", 100)],
            Jobs = 1,
            Payment = ["cash"]
        };
        Assert.Equal(100, p.Completeness);
    }
    [Fact]
    public void Comparacion_conserva_centavos_y_no_inventa_ganador_en_empates()
    {
        var p = Valid with { Prices = [new("Reparacion", 12050)] };
        var view = ProviderService.View(new(Guid.NewGuid(), p, 0, "key", "hash", true), new(true, true, false), false);
        var comparison = System.Text.Json.JsonSerializer.SerializeToElement(ProviderComparison.Build([view, view]), ProviderService.Json);
        var row = comparison.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("key").GetString() == "minPrice");
        Assert.Contains("120.50", row.GetProperty("values")[0].GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, row.GetProperty("best").ValueKind);
    }
}
