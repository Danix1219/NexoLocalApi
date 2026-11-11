# Olvidé mi contraseña · `POST /v1/identity/password/forgot`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **P** (público) |
| Pantalla de la maqueta | 7.2 Autenticación, vista `forgot` · `ACT.aunext` |
| Spec SDD | nv-01 |
| Idempotente | No. Cada llamada genera un código nuevo |

> **Cambio de tecnología**: el código llega por **correo con MailKit**, ya no por SMS.

## 1. Función

Primer paso de la recuperación (`forgot → code → newpass → done` en la maqueta). Envía un código de
6 dígitos al correo de la cuenta. El segundo paso reutiliza
[`/email/verification/confirm`](post-email-verification-confirm.md) con `purpose = resetPassword`, y
el ticket resultante lo consume [`/password/reset`](post-password-reset.md).

## 2. Petición

```http
POST /v1/identity/password/forgot
Content-Type: application/json
```

```json
{ "email": "ana.lopez@example.com" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `email` | string | **254** | sí | Normalizado a minúsculas |

## 3. Respuesta

`202 Accepted` — **idéntica exista o no la cuenta**.

```json
{ "expiresInSeconds": 900, "resendAfterSeconds": 60, "codeLength": 6 }
```

**No se devuelve nunca**: si el correo está registrado, ni el nombre del titular.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `email.invalid` | Formato inválido |
| 429 | `email.too_many_codes` | Política `otp`: 3 cada 15 min, 10 al día |
| 503 | `server.dependency_unavailable` | Redis caído |

**No existe** un `404` aquí: responder distinto ante un correo no registrado sería enumeración.

## 5. Datos

Lee cuentas `active` o `pendingDeletion` y escribe el mensaje de correo en outbox. Lectura: `accounts.findOne({ email })`
con el índice único `email`.

| Clave Redis | Estructura | TTL | Contenido |
|---|---|---|---|
| `nexo:otp:email:{emailHash}` | hash | **900 s** | `codeHash`, `attempts: 0`, `purpose: "resetPassword"` |
| `nexo:otp:resend:{emailHash}` | string | **60 s** | Ventana de reenvío |
| `nexo:rl:otp:{emailHash}:{ip}:{ventana}` | counter | 15 min / 24 h | |

**Si la cuenta no existe, no se encola ningún correo pero sí se consume cuota de rate limit y se
simula el trabajo**, para que el tiempo de respuesta sea el mismo.

`audit_events`: `password.reset_requested` `{ accountId?, ip, emailHash }` — solo si la cuenta existe.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`otp`** (correo + IP), exacto en Redis. Comparte contador con el envío de verificación
  de registro: así nadie puede duplicar su cuota alternando los dos endpoints.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MailKit** | Correo con el código, encolado al worker |
| **Redis** | Código hasheado, ventana de reenvío, contadores |
| **Azure Key Vault** | Llave HMAC y credenciales SMTP |
| Outbox → Storage Queue | Envío asíncrono, para que el tiempo de respuesta no delate nada |

Plantilla: asunto "Recupera tu acceso a Nexo Local", cuerpo con el código, vigencia de 15 minutos y
la advertencia de ignorar el correo si no fue quien lo pidió.

## 8. Seguridad y privacidad

1. **Respuesta y tiempo uniformes** exista o no la cuenta; el trabajo real va al worker.
2. Redis guarda el HMAC, con 5 intentos y 15 minutos de vida; el outbox conserva el código necesario para enviarlo y requiere controlar su retención.
3. La cuota de `otp` es **compartida** entre registro y recuperación, por correo y por IP.
4. Una cuenta en `pendingDeletion` **sí** puede recuperar contraseña: es la vía para entrar y
   cancelar la baja.
5. El correo nunca aparece en claro en los logs ni en las trazas.
