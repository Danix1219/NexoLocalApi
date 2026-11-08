using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Domain.Accounts;
using Nexo.Outbox;

namespace Nexo.Identity.Infrastructure.Messaging;

/// <summary>
/// Consumidor del outbox de Identity: convierte `identity.email_code_requested` en un correo.
/// Los demas tipos solo se registran todavia.
///
/// Si el envio lanza, el despachador marca el mensaje como fallido y el codigo sigue vivo en
/// Redis hasta su vencimiento, asi que el usuario puede pedir otro.
/// </summary>
public sealed class IdentityOutboxHandler(IEmailSender sender, ILogger<IdentityOutboxHandler> logger)
    : IOutboxHandler
{
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Type != OutboxIdentityEvents.EmailCodeType)
        {
            logger.LogInformation(
                "Outbox {MessageId} de tipo {Type} sin consumidor todavia.", message.MessageId, message.Type);
            return;
        }

        var payload = JsonSerializer.Deserialize<EmailCodeRequested>(message.Payload)
            ?? throw new InvalidOperationException($"El mensaje {message.MessageId} no trae un codigo de correo.");

        // Un fallo de envio se reintenta cada ciclo sin tope. Pasada la vida del codigo ya no sirve
        // de nada mandarlo: se descarta para no reintentar para siempre ni gastar cuota del SMTP.
        if (DateTimeOffset.UtcNow - message.OccurredAt > EmailVerificationService.CodeLifetime)
        {
            logger.LogWarning(
                "Codigo de {Purpose} descartado: el mensaje {MessageId} vencio antes de poder enviarse.",
                payload.Purpose, message.MessageId);
            return;
        }

        await sender.SendAsync(payload.Email, Subject(payload.Purpose), Body(payload), cancellationToken);

        // Nunca el codigo ni el correo completo: el registro no es un lugar donde guardar secretos.
        logger.LogInformation("Codigo de {Purpose} enviado para el mensaje {MessageId}.",
            payload.Purpose, message.MessageId);
    }

    private static string Subject(string purpose) => purpose switch
    {
        VerificationPurpose.Signup => "Tu codigo para crear la cuenta",
        VerificationPurpose.ChangeEmail => "Tu codigo para cambiar el correo",
        VerificationPurpose.ResetPassword => "Tu codigo para recuperar la contrasena",
        _ => "Tu codigo de verificacion"
    };

    private static string Body(EmailCodeRequested payload)
    {
        var action = payload.Purpose switch
        {
            VerificationPurpose.Signup => "crear tu cuenta en Nexo Local",
            VerificationPurpose.ChangeEmail => "cambiar el correo de tu cuenta",
            VerificationPurpose.ResetPassword => "recuperar tu contrasena",
            _ => "verificar tu correo"
        };

        return $"""
            Tu codigo para {action} es:

                {payload.Code}

            Vence en {EmailVerificationService.CodeLifetime.TotalMinutes:0} minutos y solo sirve una vez.

            Si no lo pediste, ignora este mensaje: nadie puede entrar a tu cuenta sin el.

            Nexo Local
            """;
    }
}
