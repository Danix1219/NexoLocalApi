namespace Nexo.Common.Errors;

/// <summary>
/// Catalogo de `code` de 00-convenciones/errores.md. El `code` es el contrato real con la app:
/// el `detail` puede cambiar, el `code` no. Van como constantes para que el compilador atrape
/// las erratas en vez de descubrirlas en produccion.
/// </summary>
public static class NexoCodes
{
    // Validacion y protocolo
    public const string ValidationFailed = "validation_failed";
    public const string RequestLimitExceeded = "request.limit_exceeded";
    public const string RequestCursorInvalid = "request.cursor_invalid";
    public const string RequestIdempotencyKeyMissing = "request.idempotency_key_missing";
    public const string RequestIdempotencyKeyReused = "request.idempotency_key_reused";
    public const string ResourceNotFound = "resource.not_found";
    public const string RequestMethodNotAllowed = "request.method_not_allowed";
    public const string RequestPayloadTooLarge = "request.payload_too_large";
    public const string RequestUnsupportedMediaType = "request.unsupported_media_type";
    public const string RateLimitExceeded = "rate.limit_exceeded";
    public const string ServerError = "server.error";
    public const string ServerDependencyUnavailable = "server.dependency_unavailable";

    // Identidad y sesion
    public const string EmailInvalid = "email.invalid";
    public const string EmailAlreadyRegistered = "email.already_registered";
    public const string EmailCodeInvalid = "email.code_invalid";
    public const string EmailCodeExpired = "email.code_expired";
    public const string EmailTooManyCodes = "email.too_many_codes";
    public const string EmailTooManyAttempts = "email.too_many_attempts";
    public const string EmailTicketInvalid = "email.ticket_invalid";
    public const string PasswordTooWeak = "password.too_weak";
    public const string PasswordMismatch = "password.mismatch";
    public const string PasswordSameAsCurrent = "password.same_as_current";
    public const string AuthInvalidCredentials = "auth.invalid_credentials";
    public const string AuthAccountLocked = "auth.account_locked";
    public const string AuthTokenExpired = "auth.token_expired";
    public const string AuthTokenRevoked = "auth.token_revoked";
    public const string AuthRefreshReused = "auth.refresh_reused";
    public const string AuthEmailNotVerified = "auth.email_not_verified";
    public const string AccountPendingDeletion = "account.pending_deletion";
    public const string PhoneAlreadyInUse = "phone.already_in_use";
    public const string PhoneNotVerified = "phone.not_verified";

    // Perfil y contenido
    public const string ProviderAlreadyExists = "provider.already_exists";
    public const string ProviderNotFound = "provider.not_found";
    public const string ProviderCategoriesRequired = "provider.categories_required";
    public const string ProviderBioTooShort = "provider.bio_too_short";
    public const string ProviderScheduleInvalid = "provider.schedule_invalid";
    public const string ProviderRadiusInvalid = "provider.radius_invalid";
    public const string CompareNeedTwoIds = "compare.need_two_ids";
    public const string ContentInappropriate = "content.inappropriate";
    public const string ContentImagesLimit = "content.images_limit";
    public const string WorkNotFound = "work.not_found";
    public const string WorkAlreadyRecommended = "work.already_recommended";
    public const string RequestNotFound = "request.not_found";
    public const string RequestAlreadyClosed = "request.already_closed";
    public const string RequestNotOwner = "request.not_owner";

    // Interes y pedidos
    public const string InterestAlreadyExpressed = "interest.already_expressed";
    public const string InterestOwnRequest = "interest.own_request";
    public const string InterestRequestAssigned = "interest.request_assigned";
    public const string OrderActiveExists = "order.active_exists";
    public const string OrderRequestAlreadyAssigned = "order.request_already_assigned";
    public const string OrderAlreadyAccepted = "order.already_accepted";
    public const string OrderInvalidTransition = "order.invalid_transition";
    public const string OrderNotParticipant = "order.not_participant";
    public const string OrderAddressNotAvailable = "order.address_not_available";
    public const string OrderAddressRequired = "order.address_required";
    public const string OrderPriceAlreadySet = "order.price_already_set";
    public const string OrderPriceOutOfRange = "order.price_out_of_range";

    // Opiniones, chat y moderacion
    public const string ReviewAlreadyExists = "review.already_exists";
    public const string ReviewOrderNotResolved = "review.order_not_resolved";
    public const string ReviewStarsInvalid = "review.stars_invalid";
    public const string ConversationNotAllowed = "conversation.not_allowed";
    public const string ConversationBlocked = "conversation.blocked";
    public const string ConversationNotParticipant = "conversation.not_participant";
    public const string ConversationNotFound = "conversation.not_found";
    public const string ContactPhoneNotAvailable = "contact.phone_not_available";
    public const string ContactDailyLimit = "contact.daily_limit";
    public const string ReportAlreadyReported = "report.already_reported";

    // Subidas e imagenes
    public const string UploadContentTypeInvalid = "upload.content_type_invalid";
    public const string UploadTooLarge = "upload.too_large";
    public const string UploadNotFound = "upload.not_found";
    public const string UploadNotPending = "upload.not_pending";
    public const string UploadExpired = "upload.expired";
    public const string UploadRejectedByModeration = "upload.rejected_by_moderation";
}
