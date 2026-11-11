using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Domain.Accounts;
using Nexo.Outbox;
using Nexo.Identity.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Nexo.Identity.Infrastructure.Messaging;

/// <summary>
/// Consumidor del outbox de Identity: convierte `identity.email_code_requested` en un correo.
/// Los demas tipos solo se registran todavia.
///
/// Si el envio lanza, el despachador marca el mensaje como fallido y el codigo sigue vivo en
/// Redis hasta su vencimiento, asi que el usuario puede pedir otro.
/// </summary>
public sealed class IdentityOutboxHandler(IEmailSender sender, ILogger<IdentityOutboxHandler> logger, IdentityCollections collections)
    : IOutboxHandler
{
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Type == OutboxIdentityEvents.AuditType)
        {
            var audit = BsonDocument.Parse(message.Payload);
            audit["_id"] = new BsonBinaryData(message.MessageId, GuidRepresentation.Standard);
            try { await collections.AuditEvents.InsertOneAsync(audit, cancellationToken: cancellationToken); }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // La entrega repetida no altera un registro de auditoria ya persistido.
            }
            return;
        }

        if (message.Type == "identity.account_notice")
        {
            var notice = JsonSerializer.Deserialize<AccountNotice>(message.Payload)
                ?? throw new InvalidOperationException("Aviso de cuenta invalido.");
            var pending = notice.Status == AccountStatus.PendingDeletion;
            await sender.SendAsync(notice.Email,
                pending ? "Solicitud de baja de Nexo Local" : "Tu cuenta de Nexo Local sigue activa",
                pending
                    ? $"Solicitaste la baja de tu cuenta. Fecha efectiva prevista: {notice.EffectiveAt:yyyy-MM-dd}. Puedes iniciar sesion para cancelarla durante la ventana de gracia."
                    : "Se cancelo la solicitud de baja. Tu cuenta vuelve a estar activa. Los dispositivos se registran de nuevo y los efectos en otros modulos se procesan por separado.",
                cancellationToken);
            return;
        }

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

        await sender.SendRichAsync(payload.Email,
            CodeEmailTemplate.Render(payload.Purpose, payload.Code,
                message.OccurredAt + EmailVerificationService.CodeLifetime),
            cancellationToken);

        // Nunca el codigo ni el correo completo: el registro no es un lugar donde guardar secretos.
        logger.LogInformation("Codigo de {Purpose} enviado para el mensaje {MessageId}.",
            payload.Purpose, message.MessageId);
    }

}
