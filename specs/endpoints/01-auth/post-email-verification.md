# Enviar código de verificación al correo · `POST /v1/identity/email/verification`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **P** (público, sin token) |
| Pantalla de la maqueta | 7.3 Registro, paso `phone` → ahora paso **`email`** · `ACT.obnext` |
| Spec SDD | nv-01 |
| Idempotente | No. Reenviar genera un código nuevo e invalida el anterior |

> **Cambio de tecnología**: reemplaza a `POST /v1/identity/phone/verify` (Firebase Phone Auth).
> Ahora el código se envía por correo con **MailKit**. Ver ADR 0010.

## 1. Función

Primer paso del registro y del cambio de correo. Genera un código de **6 dígitos**, lo guarda
*hasheado* en Redis y lo envía por correo. La app pasa luego a la pantalla del código.

Reglas que vienen de la maqueta:

- El registro de cliente son 3 pantallas: **correo → código → datos** (antes `phone → code → who`).
- Si ya hay sesión y la cuenta ya tiene correo verificado, este paso **se omite** (equivalente a
  `S.phoneOK` en el prototipo).
- La maqueta avisaba "ya existe una cuenta con este número" (`phNote`). **Eso ya no se hace aquí**:
  delatar que un correo está registrado permite enumerar cuentas. El aviso se da al final, al crear
  la cuenta, cuando ya hay un ticket válido.

## 2. Petición

```http
POST /v1/identity/email/verification
Content-Type: application/json
```

```json
{ "email": "ana.lopez@example.com", "purpose": "signup" }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `email` | string | **254** | sí | RFC 5321. Se normaliza: `trim` + minúsculas. Debe tener un `@` y un TLD válido |
| `purpose` | string | enum | sí | `signup` \| `changeEmail`. `changeEmail` exige JWT |

## 3. Respuesta

`202 Accepted` — **siempre la misma respuesta**, exista o no la cuenta.

```json
{ "expiresInSeconds": 900, "resendAfterSeconds": 60, "codeLength": 6 }
```

| Campo | Tipo | Regla |
|---|---|---|
| `expiresInSeconds` | int32 | 900 (15 min) |
| `resendAfterSeconds` | int32 | 60. La app deshabilita "Reenviar" ese tiempo |
| `codeLength` | int32 | 6 |

**No se devuelve nunca**: el código, si el correo existe, ni el nombre del titular.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `email.invalid` | Formato inválido o más de 254 caracteres |
| 400 | `validation_failed` | Falta `email` o `purpose` fuera del enum |
| 401 | `auth.token_expired` | Solo con `purpose = changeEmail` |
| 429 | `email.too_many_codes` | Política `otp`: 3 cada 15 min, 10 al día |
| 503 | `server.dependency_unavailable` | Redis o la persistencia de outbox no responden. Un fallo SMTP posterior no cambia el `202`; el despachador reintenta hasta que vence el código |

## 5. Datos

Redis guarda el estado de comprobación. MongoDB recibe el mensaje de outbox para envío asíncrono:

| Clave | Estructura | TTL | Contenido |
|---|---|---|---|
| `nexo:otp:email:{sha256(email)[0..16]}` | hash | **900 s** | `codeHash` (HMAC-SHA256 del código con llave de Key Vault), `attempts` (int, 0), `purpose`, `sentAt` |
| `nexo:otp:resend:{emailHash}` | string | **60 s** | Marca de reenvío; si existe → `202` uniforme sin generar otro código; la cuota sí se consume |
| `nexo:rl:otp:{emailHash}:{ip}:{ventana}` | counter | 15 min / 24 h | Contadores de la política `otp` |

- Redis guarda solo el HMAC. El outbox contiene correo y código en claro para su envío; no se devuelve por HTTP ni se escribe en logs. La depuración automática de payloads procesados sigue pendiente y exige acceso restringido a la colección.
- Un envío nuevo **sobrescribe** la clave, con lo que el código anterior deja de servir.
- Escribe un `audit_events` solo si `purpose = changeEmail` (acción sensible sobre una cuenta viva).

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`otp`**: partición **correo + IP**, 3 cada 15 min y 10 al día, contadores exactos en
  Redis. Ver [`../00-convenciones/rate-limiting.md`](../00-convenciones/rate-limiting.md).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MailKit** (`MailKit` + `MimeKit`) | Envío SMTP del correo con el código. Plantilla actual de texto plano |
| **Redis** | Código hasheado, ventana de reenvío y contadores exactos |
| **Azure Key Vault** | Llave HMAC del código y credenciales SMTP |
| Outbox MongoDB → despachador del mismo host (sin Storage Queue en el flujo actual) | El envío se **encola**: el handler responde `202` y el worker manda el correo. Así un SMTP lento no bloquea la petición |
| OpenTelemetry | Traza `email.verification.sent` con `emailHash`, nunca el correo en claro |

Plantilla actual: asunto por propósito, sin código en el asunto; cuerpo con el código, el tiempo de
vigencia y la advertencia de no compartirlo. Remitente verificado del dominio; `List-Unsubscribe` no
aplica (es transaccional, no mercadotecnia).

## 8. Seguridad y privacidad

1. **Respuesta uniforme**: el mismo `202` y el mismo tiempo de respuesta exista o no la cuenta. El
   trabajo de envío va al worker precisamente para que el tiempo no delate nada.
2. Redis guarda el HMAC; el outbox necesita el código para enviarlo. No confundir estas garantías: la retención del payload requiere tratamiento propio.
3. `attempts` arranca en 0 y lo incrementa el endpoint de confirmación: 5 intentos fallidos queman el
   código.
4. El correo en los logs va siempre como `sha256(email)[0..16]`.
5. Rate limit por correo **y** por IP, para que una NAT compartida no bloquee a todos y una sola IP
   no pueda rociar muchos correos.
