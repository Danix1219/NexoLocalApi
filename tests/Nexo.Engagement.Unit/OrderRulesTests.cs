using Nexo.Engagement.Domain.Orders;

namespace Nexo.Engagement.Unit;

public sealed class OrderRulesTests
{
    [Theory]
    [InlineData("pending", "accepted", true)]
    [InlineData("pending", "rejected", true)]
    [InlineData("pending", "cancelled", true)]
    [InlineData("accepted", "resolved", true)]
    [InlineData("accepted", "cancelled", true)]
    [InlineData("pending", "resolved", false)]
    [InlineData("rejected", "accepted", false)]
    [InlineData("cancelled", "resolved", false)]
    [InlineData("resolved", "cancelled", false)]
    public void Transiciones_siguen_la_maquina_contractual(string from, string to, bool allowed) =>
        Assert.Equal(allowed, ServiceOrder.CanTransition(from, to));

    [Theory]
    [InlineData(0, true)]
    [InlineData(99_999_900, true)]
    [InlineData(-1, false)]
    [InlineData(99_999_901, false)]
    public void Precio_solo_admite_centavos_en_rango(int cents, bool valid) => Assert.Equal(valid, OrderRules.ValidPrice(cents));

    [Fact]
    public void Domicilio_requiere_punto_y_coordenadas_dentro_de_Mexico()
    {
        Assert.NotNull(OrderRules.Validate("direct", "Necesito reparar una fuga", "home", "Calle 1", null, null, null));
        Assert.NotNull(OrderRules.Validate("direct", "Necesito reparar una fuga", "home", "Calle 1", null, 40, -99));
        Assert.Null(OrderRules.Validate("direct", "Necesito reparar una fuga", "home", "Calle 1", "Reja azul", 20.05, -99.34));
    }
}
