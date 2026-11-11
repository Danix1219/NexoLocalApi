using MimeKit;
using Nexo.Identity.Infrastructure.Messaging;

namespace Nexo.Identity.Unit;

public sealed class CodeEmailTemplateTests
{
    [Theory]
    [InlineData("signup", "crear tu cuenta")]
    [InlineData("resetPassword", "restablecer tu contraseña")]
    [InlineData("changeEmail", "confirmar tu nuevo correo")]
    public void Plantilla_tiene_identidad_y_alternativa_accesible(string purpose, string expected)
    {
        var content = CodeEmailTemplate.Render(purpose, "012345", DateTimeOffset.Parse("2026-11-10T06:00:00Z"));
        Assert.Contains(expected, content.Text);
        Assert.Contains("012345", content.Html);
        Assert.Contains("#17695D", content.Html);
        Assert.Contains("#FFC043", content.Html);
        Assert.Contains("10/11/2026 00:00 (hora del centro de México)", content.Text);
        Assert.DoesNotContain("<script", content.Html);
        Assert.DoesNotContain("<img", content.Html);
        if (purpose == "signup" && Environment.GetEnvironmentVariable("NEXO_EMAIL_PREVIEW_PATH") is { Length: > 0 } preview)
            File.WriteAllText(preview, content.Html);
        var message = SmtpEmailSender.CreateMessage(new MailOptions { From = "nexo@example.test" },
            "persona@example.test", content.Subject, content.Text, content.Html);
        Assert.IsType<MultipartAlternative>(message.Body);
        Assert.Equal(content.Text.ReplaceLineEndings("\n"), Assert.IsType<string>(message.TextBody).ReplaceLineEndings("\n"));
        Assert.Equal(content.Html.ReplaceLineEndings("\n"), Assert.IsType<string>(message.HtmlBody).ReplaceLineEndings("\n"));
    }
    [Fact]
    public void Codigo_no_puede_inyectar_HTML()
    {
        var content = CodeEmailTemplate.Render("signup", "<img src=x>", DateTimeOffset.UtcNow);
        Assert.Contains("&lt;img src=x&gt;", content.Html);
        Assert.DoesNotContain("<img", content.Html);
    }
}
