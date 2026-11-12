# Registrar celular opcional · `POST /v1/identity/me/phone`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | C/Pr autenticado, cuenta activa y correo verificado |
| Estado | Implementación local; aceptación cloud/global pendiente |
| Idempotente | Sí: repetir el mismo número conserva el contacto |

## 1. Función

Captura el móvil de contacto. La verificación de la cuenta se hace por correo mediante SMTP/MailKit,
no por SMS. No genera OTP ni ticket y no requiere [confirmación de celular](post-me-phone-confirm.md).
Decisión vigente en el [registro](../../../docs/decisions/registro.md).
El número es declarado: no establece `phoneVerifiedAt` ni insignia de teléfono confirmado.

## 2. Petición

```http
POST /v1/identity/me/phone
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "phone": "+527715550100" }
```

Aceptar 10 dígitos ASCII mexicanos o `+52` seguido de 10 dígitos; normalizar a E.164 (13 caracteres).

## 3. Respuesta

`200 OK`, sin OTP ni promesa de SMS:

```json
{ "phone": "+527715550100", "phoneVerificationStatus": "declared" }
```

No activa por sí solo `channels.call`: mostrar el número exige consentimiento explícito.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `phone.invalid` | Número inválido |
| 401 | `auth.token_expired` | Sin sesión válida |
| 403 | `auth.email_not_verified` | Correo sin verificar |
| 403 | `account.pending_deletion` | Cuenta en baja |
| 503 | `server.dependency_unavailable` | Persistencia no disponible |

Sin `phone.already_in_use`: declarar un número no reserva una línea contra otras cuentas.

## 5. Datos

Actualizar `accounts.phone` y `updatedAt` con `sub` y estado activo en el filtro.
Sin `phonePending`, `phoneVerifiedAt` ni índice único de teléfono declarado.
Evento previsto `account.phone_updated`, no `account.phone_verified`; su consumidor queda pendiente.

## 6. Caché y rate limit

Política `user`, sin cuota OTP. Invalidar la vista propia al guardar.

## 7. Tecnologías

MongoDB y outbox para propagación futura. SMTP/MailKit verifica exclusivamente el correo.
Sin pasarela SMS ni Firebase Phone Auth; FCM para push es independiente.

## 8. Seguridad y privacidad

El recurso lo determina `sub`. No registrar teléfonos completos ni devolver números ajenos.
Mostrar el número requiere correo confirmado, móvil declarado y consentimiento explícito.
Pruebas futuras: caso válido, formato inválido, 401, correo no verificado, aislamiento y ausencia de OTP.
