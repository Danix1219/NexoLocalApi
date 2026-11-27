using System.Net;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Infrastructure.Messaging;

public sealed record EmailContent(string Subject, string Text, string Html);

/// <summary>
/// Email-compatible inline tables; no remote images, scripts or tracking. The only image is the app
/// logo, attached to the message itself and referenced by Content-ID.
/// </summary>
public static class CodeEmailTemplate
{
    public const string LogoContentId = "nexo-logo";
    private const string LogoResource = "Nexo.Identity.Messaging.nexo-logo.png";

    private sealed record Copy(string Subject, string Eyebrow, string Title, string Action, string Intro, string[] Steps, string Ignore);

    private static Copy For(string purpose) => purpose switch
    {
        VerificationPurpose.Signup => new(
            "Confirma tu correo | Nexo Local", "CREA TU CUENTA", "Tu comunidad empieza aquí", "crear tu cuenta",
            "¡Qué gusto tenerte por aquí! Confirma tu correo para crear tu cuenta y encontrar a quien sabe hacerlo, cerca de ti.",
            ["Regresa a la app de Nexo Local.", "Escribe el código de 6 dígitos.", "Completa tu nombre y tu contraseña."],
            "Si no intentaste crear una cuenta, ignora este mensaje: no se creará nada sin el código."),
        VerificationPurpose.ResetPassword => new(
            "Recupera tu acceso | Nexo Local", "RECUPERA TU ACCESO", "Volvamos a conectarte", "restablecer tu contraseña",
            "Recibimos una solicitud para restablecer tu contraseña. Usa este código para continuar de forma segura.",
            ["Regresa a la app de Nexo Local.", "Escribe el código de 6 dígitos.", "Crea una contraseña nueva e inicia sesión."],
            "Si no pediste cambiar tu contraseña, ignora este mensaje: tu contraseña actual sigue funcionando."),
        VerificationPurpose.ChangeEmail => new(
            "Confirma tu nuevo correo | Nexo Local", "CAMBIO DE CORREO", "Un nuevo correo, el mismo Nexo", "confirmar tu nuevo correo",
            "Confirma que este correo es tuyo para usarlo en tu cuenta.",
            ["Regresa a la app de Nexo Local.", "Escribe el código de 6 dígitos.", "Tu cuenta usará el correo nuevo."],
            "Si no solicitaste este cambio, ignora este mensaje."),
        _ => new(
            "Tu código de verificación | Nexo Local", "VERIFICACIÓN", "Confirma que eres tú", "verificar tu correo",
            "Usa este código para continuar.",
            ["Regresa a la app de Nexo Local.", "Escribe el código de 6 dígitos."],
            "Si no solicitaste este código, ignora este mensaje.")
    };

    /// <summary>PNG bytes of the app icon, embedded in this assembly.</summary>
    public static byte[] Logo()
    {
        using var stream = typeof(CodeEmailTemplate).Assembly.GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException($"Falta el recurso incrustado {LogoResource}.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    public static EmailContent Render(string purpose, string code, DateTimeOffset expiresAt)
    {
        var copy = For(purpose);
        var safeCode = WebUtility.HtmlEncode(code);
        var localExpiry = TimeZoneInfo.ConvertTime(expiresAt, TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"));
        var expires = localExpiry.ToString("dd/MM/yyyy HH:mm '(hora del centro de México)'", System.Globalization.CultureInfo.InvariantCulture);

        var textSteps = string.Join("\n", copy.Steps.Select((step, i) => $"{i + 1}. {step}"));
        var text = $"Nexo Local\n\n{copy.Title}\n\n{copy.Intro}\n\nUsa este código para {copy.Action}:\n\n{code}\n\n" +
            $"Válido hasta {expires}. Solo se puede usar una vez.\n\n{textSteps}\n\n" +
            $"No lo compartas: nuestro equipo nunca te pedirá este código.\n{copy.Ignore}\n\n" +
            "Conectamos lo que necesitas con quien sabe hacerlo.";

        var htmlSteps = string.Concat(copy.Steps.Select((step, i) => $$"""
            <tr><td width="34" valign="top" style="padding:6px 0"><div style="width:24px;height:24px;line-height:24px;border-radius:12px;background:#17695D;color:#FFFFFF;text-align:center;font-size:12px;font-weight:bold">{{i + 1}}</div></td>
            <td valign="top" style="padding:8px 0 6px;color:#1C2926;font-size:14px;line-height:1.5">{{step}}</td></tr>
            """));

        var html = $$"""
            <!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="color-scheme" content="light"><title>{{copy.Subject}}</title></head>
            <body style="margin:0;padding:0;background:#F6F4EE;color:#1C2926;font-family:Arial,Helvetica,sans-serif">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0">Tu código para {{copy.Action}}. No lo compartas con nadie.</div>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#F6F4EE"><tr><td align="center" style="padding:28px 12px">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#FFFEFB;border:1px solid #D9DFDA;border-radius:24px;overflow:hidden">
            <tr><td align="center" style="padding:30px 24px 26px;background:#104D45;border-bottom:4px solid #FFC043">
            <img src="cid:{{LogoContentId}}" width="76" height="76" alt="Logo de Nexo Local" style="display:block;width:76px;height:76px;border:0;border-radius:20px">
            <div style="margin-top:14px;font-size:28px;letter-spacing:-1px;color:#FFFFFF"><strong>nexo</strong><span style="color:#BFE3D8">local</span></div>
            <div style="margin-top:6px;font-size:13px;color:#E0F1E8">Personas cerca. Soluciones reales.</div></td></tr>
            <tr><td style="padding:30px 28px 8px">
            <p style="margin:0 0 10px;color:#17695D;font-size:12px;font-weight:bold;letter-spacing:1.5px">{{copy.Eyebrow}}</p>
            <h1 style="margin:0 0 12px;font-size:26px;line-height:1.25;letter-spacing:-.5px;color:#1C2926">{{copy.Title}}</h1>
            <p style="margin:0 0 22px;color:#55645F;font-size:15px;line-height:1.65">{{copy.Intro}}</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td align="center" style="padding:18px 8px 20px;background:#E1F0EB;border:1px solid #BDDCD0;border-radius:18px">
            <div style="margin-bottom:8px;font-size:12px;color:#104D45;font-weight:bold">TU CÓDIGO</div>
            <div style="font-size:36px;font-weight:bold;letter-spacing:8px;color:#104D45;font-family:Consolas,Menlo,monospace">{{safeCode}}</div></td></tr></table>
            <p style="margin:12px 0 24px;text-align:center;font-size:12px;line-height:1.6;color:#62716C">Un solo uso · Válido hasta {{expires}}</p>
            <p style="margin:0 0 6px;font-size:15px;font-weight:bold;color:#1C2926">Qué sigue</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin-bottom:20px">{{htmlSteps}}</table>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td style="padding:15px 16px;background:#FCEEDB;border-left:4px solid #C87825;border-radius:12px;color:#7C440B;font-size:13.5px;line-height:1.6"><strong>Tu seguridad va primero.</strong><br>No compartas este código. El equipo de Nexo Local nunca te lo pedirá por llamada, chat o correo.</td></tr></table>
            <p style="margin:20px 0 22px;color:#62716C;font-size:13px;line-height:1.6">{{copy.Ignore}}</p>
            </td></tr><tr><td style="padding:20px 28px;background:#F6F4EE;border-top:1px solid #D9DFDA;text-align:center;font-size:12px;line-height:1.7;color:#62716C"><strong style="color:#17695D">Nexo Local</strong><br>Conectamos lo que necesitas con quien sabe hacerlo.<br>Este es un mensaje automático; no respondas a este correo.</td></tr>
            </table></td></tr></table></body></html>
            """;
        return new(copy.Subject, text, html);
    }
}
