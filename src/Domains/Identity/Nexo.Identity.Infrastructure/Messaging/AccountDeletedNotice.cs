namespace Nexo.Identity.Infrastructure.Messaging;

/// <summary>Último aviso de una cuenta. La dirección solo vive en el mensaje hasta que se envía o vence.</summary>
public sealed record AccountDeletedNotice(string Email)
{
    public const string Type = "identity.account_deleted";
    /// <summary>Marca de mensaje ya atendido, sin datos personales.</summary>
    public const string Erased = "{}";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public const string Subject = "Tu cuenta de Nexo Local se eliminó";
    public const string Body =
        "Tu cuenta de Nexo Local se eliminó y ya no puedes iniciar sesión con ella.\n\n" +
        "Para no alterar el historial de otras personas se conservan los pedidos terminados, sin su descripción " +
        "ni domicilio, y las opiniones, sin tu nombre.\n\n" +
        "Si quieres volver, puedes registrarte de nuevo con este correo.";
}
