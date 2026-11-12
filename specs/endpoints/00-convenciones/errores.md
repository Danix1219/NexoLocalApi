# Errores — RFC 9457 y catálogo de `code`

> Todo error de la API es un **Problem Details** (RFC 9457) con un `code` **estable** que la app usa
> para decidir qué mensaje mostrar. El `detail` es para humanos y puede cambiar; **el `code` no**.

---

## 1. Forma de la respuesta

`Content-Type: application/problem+json`

```json
{
  "type": "https://nexolocal.mx/problems/order-already-accepted",
  "title": "El pedido ya fue aceptado",
  "status": 409,
  "detail": "Este pedido fue aceptado el 2026-11-05 a las 14:02.",
  "instance": "/v1/engagement/orders/019400a1-.../accept",
  "code": "order.already_accepted",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

| Campo | Tipo | Máximo | Regla |
|---|---|---|---|
| `type` | string (URI) | 200 | URI estable por tipo de problema |
| `title` | string | 120 | Texto corto en español, sin datos del usuario |
| `status` | int32 | — | Igual al código HTTP |
| `detail` | string | 300 | **Nunca** incluye correo, teléfono, dirección ni si una cuenta ajena existe |
| `instance` | string | 200 | Ruta de la petición |
| `code` | string | 60 | `<dominio>.<motivo>` en snake_case. Es el contrato real |
| `traceId` | string | 64 | W3C Trace Context; se correlaciona con los logs |
| `errors` | object | — | Solo en `400 validation_failed`: `{ "campo": ["regla"] }` |

---

## 2. Reglas de redacción (privacidad)

1. **Nunca revelar si una cuenta existe.** Login, registro y recuperación devuelven el mismo mensaje
   genérico ante correo inexistente o contraseña incorrecta (`auth.invalid_credentials`).
2. **Nunca eco del dato sensible.** El `detail` no repite el correo, el teléfono ni la dirección.
3. **Nunca filtrar existencia de recursos ajenos.** Si el recurso existe pero no es tuyo, se responde
   **`404 resource.not_found`**, no `403` — para no confirmar que existe.
   Única excepción: cuando ya eres parte del recurso y lo que falta es el estado correcto; ahí sí
   `409` o `403` con su `code` específico.
4. Los `5xx` no llevan `detail` técnico: el stack trace va al log con el mismo `traceId`.

---

## 3. Catálogo de `code`

### Validación y protocolo

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Falla un `Validator`. Trae el objeto `errors` |
| 400 | `request.limit_exceeded` | `limit` mayor que 50 |
| 400 | `request.cursor_invalid` | Cursor corrupto o de otra consulta |
| 400 | `request.idempotency_key_missing` | POST que crea recurso sin `Idempotency-Key` |
| 409 | `request.idempotency_key_reused` | La misma llave con cuerpo distinto |
| 404 | `resource.not_found` | No existe, o existe y no es tuyo |
| 405 | `request.method_not_allowed` | |
| 413 | `request.payload_too_large` | Cuerpo mayor que 1 MB o imagen mayor que 5 MB |
| 415 | `request.unsupported_media_type` | |
| 429 | `rate.limit_exceeded` | Trae cabecera `Retry-After`. Ver [`rate-limiting.md`](rate-limiting.md) |
| 500 | `server.error` | |
| 503 | `server.dependency_unavailable` | Circuit breaker abierto (Mongo, Redis, Content Safety) |

### Identidad y sesión

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `email.invalid` | Formato de correo inválido |
| 409 | `email.already_registered` | **Solo en registro con ticket válido**; en el paso de envío de código nunca se delata |
| 400 | `email.code_invalid` | Código de 6 dígitos incorrecto |
| 410 | `email.code_expired` | Pasaron los 15 min |
| 429 | `email.too_many_codes` | Política `otp`: 3 cada 15 min, 10 al día |
| 400 | `email.ticket_invalid` | Ticket inexistente, ya usado o caducado |
| 400 | `password.too_weak` | Menos de 8 caracteres, o sin letra o sin número |
| 401 | `auth.invalid_credentials` | Correo o contraseña incorrectos (mensaje único) |
| 423 | `auth.account_locked` | 5 intentos fallidos; bloqueo temporal |
| 401 | `auth.token_expired` | Access token caducado → la app renueva |
| 401 | `auth.token_revoked` | `jti` en la lista negra tras un logout |
| 401 | `auth.refresh_reused` | Refresh ya rotado: se revoca toda la familia |
| 403 | `auth.email_not_verified` | |
| 403 | `account.pending_deletion` | La cuenta está en los 30 días de baja |
| 409 | `account.concurrent_update` | La revisión cambió durante la escritura; consultar y reintentar |
| 409 | `account.already_pending_deletion` | Solicitud repetida; se conserva la fecha original |
| 409 | `account.not_pending_deletion` | No hay una baja que cancelar |
| 410 | `account.deletion_already_executed` | Venció la ventana para cancelar; un token ya revocado recibe 401 |
| 409 | `provider.profile_required` | El modo proveedor requiere perfil profesional |
| 409 | `device.token_owned_by_other_account` | Otra cuenta activa usó recientemente el token, o hubo conflicto de reasignación |
| 400 | `phone.invalid` | Móvil declarado fuera del formato permitido; no acredita posesión de la línea |

### Perfil y contenido

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `provider.already_exists` | La cuenta ya tiene perfil profesional |
| 404 | `provider.not_found` | |
| 400 | `provider.categories_required` | Ninguna categoría inferida ni declarada |
| 400 | `provider.bio_too_short` | Menos de 20 caracteres |
| 400 | `provider.schedule_invalid` | `close <= open`, o ningún día abierto |
| 400 | `provider.radius_invalid` | Fuera de {0, 2, 5, 10, 20} |
| 400 | `compare.need_two_ids` | `ids` no trae exactamente 2 |
| 403 | `content.inappropriate` | Content Safety rechazó el texto o la imagen |
| 400 | `content.images_limit` | Más de 6 imágenes |
| 404 | `work.not_found` | |
| 409 | `work.already_recommended` | |
| 404 | `request.not_found` | |
| 409 | `request.already_closed` | |
| 403 | `request.not_owner` | No eres quien publicó la solicitud |

### Interés y pedidos

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `interest.already_expressed` | |
| 403 | `interest.own_request` | Un prestador no se interesa en su propia solicitud |
| 409 | `interest.request_assigned` | La solicitud ya tiene prestador asignado |
| 409 | `order.active_exists` | Ya hay un pedido `pending` o `accepted` con ese prestador (índice único parcial) |
| 409 | `order.request_already_assigned` | **Dos asignaciones activas sobre la misma solicitud: nunca** |
| 409 | `order.already_accepted` | |
| 409 | `order.invalid_transition` | El estado actual no permite esa acción |
| 403 | `order.not_participant` | No eres el cliente ni el prestador del pedido |
| 403 | `order.address_not_available` | El pedido no está `accepted`/`resolved`: la dirección no se entrega |
| 400 | `order.address_required` | `attention = home` sin dirección |
| 409 | `order.price_already_set` | |
| 400 | `order.price_out_of_range` | Mayor que $999 999.00 |

### Opiniones, chat y moderación

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `review.already_exists` | Una opinión por pedido (índice único disperso) |
| 403 | `review.order_not_resolved` | Solo se califica un pedido resuelto |
| 400 | `review.stars_invalid` | Fuera de 1–5 |
| 403 | `conversation.not_allowed` | El prestador intenta iniciar chat sin haber sido elegido |
| 403 | `conversation.blocked` | Alguna de las partes bloqueó a la otra |
| 403 | `conversation.not_participant` | |
| 404 | `conversation.not_found` | |
| 403 | `contact.phone_not_available` | El prestador no activó el canal de llamada |
| 429 | `contact.daily_limit` | Política `contact`: 40 al día |
| 409 | `report.already_reported` | Mismo objetivo reportado por el mismo usuario |

### Subidas e imágenes

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `upload.content_type_invalid` | No es jpeg, png ni webp |
| 413 | `upload.too_large` | Mayor que 5 MB |
| 404 | `upload.not_found` | |
| 409 | `upload.not_pending` | Ya confirmada o rechazada |
| 410 | `upload.expired` | Caducó la URL SAS |
| 403 | `upload.rejected_by_moderation` | |

---

## 4. Implementación

- Un `IExceptionHandler` global traduce las excepciones de dominio a Problem Details; los handlers
  devuelven `Result<T>` con el `code`, no lanzan excepciones para el flujo normal.
- El `code` se define como `const string` en `Nexo.Common/Errors/` para que el compilador atrape
  los errores de escritura.
- La app Flutter mapea `code` → mensaje localizado; **si recibe un `code` desconocido** muestra un
  texto genérico y lo registra en Crashlytics.
