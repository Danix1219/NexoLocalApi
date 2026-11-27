using Nexo.Engagement.Application.Interests;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Engagement.Unit;

public sealed class OrderRequestFlowRulesTests
{
    [Theory]
    [InlineData("chat", true)]
    [InlineData("chatCall", true)]
    [InlineData("", false)]
    public void Published_contact_channels_allow_in_app_chat(string contact, bool expected) =>
        Assert.Equal(expected, InterestService.AllowsChat(contact));

    [Theory]
    [InlineData("client", "pending", "Plomeria Centro", null, "Plomeria Centro")]
    [InlineData("provider", "pending", null, "Ana Maria Lopez", "Ana L.")]
    [InlineData("provider", "pending", null, "Ana", "Ana")]
    [InlineData("provider", "accepted", null, "Ana Lopez", "Ana Lopez")]
    [InlineData("provider", "pending", null, null, "Cliente")]
    public void Counterpart_name_follows_the_order_privacy_rule(string role, string status, string? provider, string? client, string expected) =>
        Assert.Equal(expected, OrderService.CounterpartName(role, status, provider, client));
}
