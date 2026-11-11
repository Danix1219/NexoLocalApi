using System.Net;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Infrastructure.Messaging;

public sealed record EmailContent(string Subject, string Text, string Html);

/// <summary>Email-compatible inline tables; no remote images, scripts or tracking.</summary>
public static class CodeEmailTemplate
{
    public static EmailContent Render(string purpose, string code, DateTimeOffset expiresAt)
    {
        var (subject, title, action) = purpose switch
        {
            VerificationPurpose.Signup => ("Confirma tu correo | Nexo Local", "Tu comunidad empieza aquí", "crear tu cuenta"),
            VerificationPurpose.ResetPassword => ("Recupera tu acceso | Nexo Local", "Volvamos a conectarte", "restablecer tu contraseña"),
            VerificationPurpose.ChangeEmail => ("Confirma tu nuevo correo | Nexo Local", "Un nuevo correo, el mismo Nexo", "confirmar tu nuevo correo"),
            _ => ("Tu código de verificación | Nexo Local", "Confirma que eres tú", "verificar tu correo")
        };
        var safeCode = WebUtility.HtmlEncode(code);
        var localExpiry = TimeZoneInfo.ConvertTime(expiresAt, TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"));
        var expires = localExpiry.ToString("dd/MM/yyyy HH:mm '(hora del centro de México)'", System.Globalization.CultureInfo.InvariantCulture);
        var text = $"Nexo Local\n\n{title}\n\nUsa este código para {action}:\n\n{code}\n\nVálido hasta {expires}. Solo se puede usar una vez.\nNo lo compartas: nuestro equipo nunca te pedirá este código.\nSi no lo solicitaste, ignora este mensaje.\n\nConectamos lo que necesitas con quien sabe hacerlo.";
        var html = $$"""
            <!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{{subject}}</title></head>
            <body style="margin:0;padding:0;background:#F6F5F0;color:#1C2926;font-family:Arial,Helvetica,sans-serif">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0">Tu código para {{action}}. No lo compartas con nadie.</div>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#F6F5F0"><tr><td align="center" style="padding:32px 16px">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#FFFEFB;border:1px solid #D9DFDA;border-radius:24px;overflow:hidden">
            <tr><td style="padding:28px 32px;background:#17695D;color:#FFFFFF"><span style="color:#FFC043;font-size:32px;font-weight:bold">∞</span><span style="font-size:27px;font-weight:bold;letter-spacing:-1px"> nexo<span style="font-weight:normal">local</span></span><p style="margin:10px 0 0;font-size:13px;color:#E0F1E8">Personas cerca. Soluciones reales.</p></td></tr>
            <tr><td style="padding:32px">
            <p style="margin:0 0 12px;color:#17695D;font-size:12px;font-weight:bold;letter-spacing:1.5px">CONFIRMACIÓN DE CORREO</p>
            <h1 style="margin:0 0 14px;font-size:27px;line-height:1.25;letter-spacing:-.6px">{{title}}</h1>
            <p style="margin:0 0 24px;color:#61736C;font-size:16px;line-height:1.6">Estás a un paso de {{action}}. Regresa a la app e introduce el siguiente código:</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td align="center" style="padding:22px 8px;background:#E0F1E8;border:1px solid #BDDCD0;border-radius:16px;font-size:34px;font-weight:bold;letter-spacing:7px;color:#17695D;font-family:Consolas,monospace">{{safeCode}}</td></tr></table>
            <p style="margin:14px 0 26px;text-align:center;font-size:12px;line-height:1.6;color:#61736C">Un solo uso · Válido hasta {{expires}}</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td style="padding:16px;background:#FFF5DA;border-radius:14px;color:#66501C;font-size:14px;line-height:1.6"><strong>Tu seguridad va primero.</strong><br>No compartas este código. El equipo de Nexo Local nunca te lo pedirá por llamada, chat o correo.</td></tr></table>
            <p style="margin:24px 0 0;color:#61736C;font-size:13px;line-height:1.6">Si no solicitaste este código, puedes ignorar el mensaje.</p>
            </td></tr><tr><td style="padding:22px 32px;border-top:1px solid #D9DFDA;text-align:center;font-size:12px;line-height:1.7;color:#61736C"><strong style="color:#17695D">Nexo Local</strong><br>Conectamos lo que necesitas con quien sabe hacerlo.<br>Este es un mensaje automático.</td></tr>
            </table></td></tr></table></body></html>
            """;
        return new(subject, text, html);
    }
}
