using Nexo.Engagement.Application.Safety;

namespace Nexo.Engagement.Unit;

public sealed class ContactReportRulesTests
{
    [Theory]
    [InlineData("+527715550100", "771 555 0100")]
    [InlineData("+15551234567", "+15551234567")]
    public void Celular_legible(string e164, string display) => Assert.Equal(display, ContactReportService.DisplayPhone(e164));
}
