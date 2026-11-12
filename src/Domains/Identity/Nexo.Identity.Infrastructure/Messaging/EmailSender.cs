using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Nexo.Identity.Infrastructure.Messaging;

/// <summary>Datos del SMTP. Como todo lo demas, llegan por variables de entorno `Mail__*`.</summary>
public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "Nexo Local";

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            return $"{SectionName}:Host ausente. Definir la variable {SectionName}__Host.";
        }

        if (Port is < 1 or > 65535)
        {
            return $"{SectionName}:Port fuera de rango.";
        }

        return string.IsNullOrWhiteSpace(From)
            ? $"{SectionName}:From ausente. Definir la variable {SectionName}__From."
            : null;
    }
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendRichAsync(string to, EmailContent content, CancellationToken cancellationToken = default) =>
        SendAsync(to, content.Subject, content.Text, cancellationToken);
}

/// <summary>
/// Envio por SMTP con MailKit. Abre y cierra la conexion por mensaje: el worker manda pocos
/// correos por minuto y una conexion viva se cae sin avisar, que es peor de depurar.
/// </summary>
public sealed class SmtpEmailSender(IOptions<MailOptions> options) : IEmailSender
{
    private readonly MailOptions mail = options.Value;

    public async Task SendAsync(
        string to, string subject, string body, CancellationToken cancellationToken = default)
        => await SendMessageAsync(CreateMessage(mail, to, subject, body), cancellationToken);

    public Task SendRichAsync(string to, EmailContent content, CancellationToken cancellationToken = default) =>
        SendMessageAsync(CreateMessage(mail, to, content.Subject, content.Text, content.Html), cancellationToken);

    public static MimeMessage CreateMessage(MailOptions mail, string to, string subject, string body, string? html = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(mail.FromName, mail.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = html is null ? new TextPart(TextFormat.Plain) { Text = body }
            : new BodyBuilder { TextBody = body, HtmlBody = html }.ToMessageBody();
        return message;
    }

    private async Task SendMessageAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();

        // 465 es TLS desde el saludo; 587 y 25 negocian STARTTLS. Elegirlo mal cuelga la conexion.
        var security = mail.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(mail.Host, mail.Port, security, cancellationToken);

        if (!string.IsNullOrEmpty(mail.User))
        {
            await client.AuthenticateAsync(mail.User, mail.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
