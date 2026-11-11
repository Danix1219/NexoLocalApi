using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Nexo.Common.Results;

namespace Nexo.Common.Errors;

/// <summary>
/// Traduce un <see cref="NexoError"/> a Problem Details RFC 9457, con el `code` estable,
/// el `traceId` de W3C y sin filtrar datos del usuario (00-convenciones/errores.md).
/// </summary>
public static class NexoProblem
{
    private const string TypeBase = "https://nexolocal.mx/problems/";

    /// <summary>Estado HTTP y titulo por `code`. Un code desconocido es un 500: no se adivina.</summary>
    public static (int Status, string Title) Describe(string code) => code switch
    {
        NexoCodes.ValidationFailed => (StatusCodes.Status400BadRequest, "Revisa los datos enviados"),
        NexoCodes.RequestLimitExceeded => (StatusCodes.Status400BadRequest, "El limite por pagina es 50"),
        NexoCodes.RequestCursorInvalid => (StatusCodes.Status400BadRequest, "El cursor no es valido"),
        NexoCodes.RequestIdempotencyKeyMissing => (StatusCodes.Status400BadRequest, "Falta la llave de idempotencia"),
        NexoCodes.RequestIdempotencyKeyReused => (StatusCodes.Status409Conflict, "Esa llave ya se uso con otro contenido"),
        NexoCodes.ResourceNotFound => (StatusCodes.Status404NotFound, "No encontrado"),
        NexoCodes.RequestPayloadTooLarge => (StatusCodes.Status413PayloadTooLarge, "El contenido es demasiado grande"),
        NexoCodes.RequestUnsupportedMediaType => (StatusCodes.Status415UnsupportedMediaType, "Formato no admitido"),
        NexoCodes.RateLimitExceeded => (StatusCodes.Status429TooManyRequests, "Demasiadas solicitudes"),
        NexoCodes.ServerDependencyUnavailable => (StatusCodes.Status503ServiceUnavailable, "Servicio no disponible por ahora"),

        NexoCodes.EmailInvalid => (StatusCodes.Status400BadRequest, "Correo invalido"),
        NexoCodes.EmailAlreadyRegistered => (StatusCodes.Status409Conflict, "Ese correo ya tiene cuenta"),
        NexoCodes.EmailCodeInvalid => (StatusCodes.Status400BadRequest, "Codigo incorrecto"),
        NexoCodes.EmailCodeExpired => (StatusCodes.Status410Gone, "El codigo caduco"),
        NexoCodes.EmailTooManyCodes => (StatusCodes.Status429TooManyRequests, "Demasiados codigos solicitados"),
        NexoCodes.EmailTooManyAttempts => (StatusCodes.Status429TooManyRequests, "Demasiados intentos"),
        NexoCodes.EmailTicketInvalid => (StatusCodes.Status400BadRequest, "La verificacion del correo no es valida"),
        NexoCodes.PasswordTooWeak => (StatusCodes.Status400BadRequest, "La contrasena no cumple los requisitos"),
        NexoCodes.PasswordMismatch => (StatusCodes.Status400BadRequest, "Las contrasenas no coinciden"),
        NexoCodes.PasswordSameAsCurrent => (StatusCodes.Status400BadRequest, "Esa ya es tu contrasena actual"),
        NexoCodes.AuthInvalidCredentials => (StatusCodes.Status401Unauthorized, "Correo o contrasena incorrectos"),
        NexoCodes.AuthAccountLocked => (StatusCodes.Status423Locked, "Cuenta bloqueada temporalmente"),
        NexoCodes.AuthTokenExpired => (StatusCodes.Status401Unauthorized, "La sesion caduco"),
        NexoCodes.AuthTokenRevoked => (StatusCodes.Status401Unauthorized, "La sesion fue cerrada"),
        NexoCodes.AuthRefreshReused => (StatusCodes.Status401Unauthorized, "La sesion se cerro por seguridad"),
        NexoCodes.AuthEmailNotVerified => (StatusCodes.Status403Forbidden, "Falta verificar el correo"),
        NexoCodes.AccountPendingDeletion => (StatusCodes.Status403Forbidden, "La cuenta esta en proceso de baja"),
        NexoCodes.AccountConcurrentUpdate => (StatusCodes.Status409Conflict, "La cuenta cambio; reintenta"),
        NexoCodes.AccountAlreadyPendingDeletion => (StatusCodes.Status409Conflict, "La baja ya fue solicitada"),
        NexoCodes.AccountNotPendingDeletion => (StatusCodes.Status409Conflict, "No hay baja pendiente"),
        NexoCodes.AccountDeletionAlreadyExecuted => (StatusCodes.Status410Gone, "Ya no se puede cancelar la baja"),
        NexoCodes.ProviderProfileRequired => (StatusCodes.Status409Conflict, "Crea tu perfil profesional"),
        NexoCodes.ProviderAlreadyExists => (409, "La cuenta ya tiene perfil profesional"),
        NexoCodes.ProviderNotFound => (404, "Perfil no disponible"),
        NexoCodes.ProviderBioTooShort => (400, "La descripcion necesita al menos 20 caracteres"),
        NexoCodes.ProviderCategoriesRequired => (400, "Elige las categorias permitidas"),
        NexoCodes.ProviderRadiusInvalid => (400, "Radio de atencion no valido"),
        NexoCodes.ProviderScheduleInvalid => (400, "Horario no valido"),
        NexoCodes.CompareNeedTwoIds => (400, "Elige dos prestadores distintos"),
        NexoCodes.ProviderPricesLimit => (400, "Solo se permiten tres precios"),
        NexoCodes.ContentImagesLimit => (400, "Solo se permiten seis imagenes"),
        NexoCodes.DeviceTokenOwnedByOtherAccount => (StatusCodes.Status409Conflict, "Dispositivo en uso"),
        NexoCodes.PhoneInvalid => (StatusCodes.Status400BadRequest, "Celular invalido"),
        NexoCodes.ContentInappropriate => (StatusCodes.Status403Forbidden, "Contenido no permitido"),
        NexoCodes.UploadNotFound => (StatusCodes.Status404NotFound, "Subida no disponible"),
        NexoCodes.UploadNotPending => (StatusCodes.Status409Conflict, "Subida no aprobada"),
        NexoCodes.WorkNotFound => (StatusCodes.Status404NotFound, "Trabajo no disponible"),
        NexoCodes.RequestNotFound => (StatusCodes.Status404NotFound, "Solicitud no disponible"),
        NexoCodes.RequestAlreadyClosed => (StatusCodes.Status409Conflict, "La solicitud ya está cerrada"),
        NexoCodes.InterestOwnRequest => (StatusCodes.Status403Forbidden, "No puedes interesarte en tu propia solicitud"),
        NexoCodes.InterestRequestAssigned or NexoCodes.InterestCannotWithdrawAssigned => (StatusCodes.Status409Conflict, "La solicitud ya tiene un prestador asignado"),
        NexoCodes.WorkOwnContent => (StatusCodes.Status403Forbidden, "No puedes recomendar tu propio trabajo"),
        NexoCodes.WorkCategoryNotInProfile => (StatusCodes.Status400BadRequest, "La categoría no pertenece a tu perfil"),

        _ => (StatusCodes.Status500InternalServerError, "Error del servidor")
    };

    public static IResult From(NexoError error, HttpContext context) =>
        From(error.Code, error.Message, context);

    public static IResult From(string code, string? detail, HttpContext context)
    {
        var (status, title) = Describe(code);

        // Los 5xx no llevan detalle tecnico: ese va al log con el mismo traceId.
        var safeDetail = status >= StatusCodes.Status500InternalServerError ? null : detail;

        return Microsoft.AspNetCore.Http.Results.Problem(
            type: TypeBase + code.Replace('.', '-').Replace('_', '-'),
            title: title,
            statusCode: status,
            detail: safeDetail,
            instance: context.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
            });
    }

    /// <summary>400 con el objeto `errors` por campo, la unica forma que lo lleva.</summary>
    public static IResult Validation(IDictionary<string, string[]> errors, HttpContext context)
    {
        var (status, title) = Describe(NexoCodes.ValidationFailed);

        return Microsoft.AspNetCore.Http.Results.Problem(
            type: TypeBase + "validation-failed",
            title: title,
            statusCode: status,
            detail: "Uno o mas campos no cumplen las reglas.",
            instance: context.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = NexoCodes.ValidationFailed,
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
                ["errors"] = errors
            });
    }
}
