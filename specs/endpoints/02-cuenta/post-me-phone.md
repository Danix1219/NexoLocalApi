# Registrar celular (opcional) · `POST /v1/identity/me/phone`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.3 Registro paso `terms` (canales) y 7.13 Cuenta → Ver perfil |
| Spec SDD | nv-02 |
| Idempotente | No. Cada llamada genera un código nuevo |

> **Endpoint nuevo**, consecuencia del cambio a identidad por correo. El celular dejó de ser la
> identidad de la cuenta y pasó a ser un **dato opcional del perfil**, pero sigue existiendo porque
> la maqueta tiene "Copiar número", el canal de llamada y la insignia *Teléfono confirmado*, y la
> constitución §I.1 prohíbe eliminar funciones de la maqueta.

## 1. Función

Primer paso para añadir o cambiar el celular. Valida el formato, comprueba que no esté en otra cuenta
y envía un código de 6 dígitos **por SMS**. Lo confirma
[`POST /v1/identity/me/phone/confirm`](post-me-phone-confirm.md).

El celular habilita dos cosas y nada más:

1. El canal **"mostrar número para llamadas"** (`channels.call` del perfil).
2. La insignia **Teléfono confirmado**, que es una de las tres señales de confianza y debe
   permanecer separada de las otras dos (constitución §IV.3).

## 2. Petición

```http
POST /v1/identity/me/phone
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "phone": "+527715550100" }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `phone` | string | **13** exactos | sí | E.164 México: `+52` + 10 dígitos. Se acepta también `7715550100` y se normaliza añadiendo `+52` |

## 3. Respuesta

`202 Accepted`

```json
{ "expiresInSeconds": 900, "resendAfterSeconds": 60, "codeLength": 6, "maskedPhone": "+52 771 *** 0100" }
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `expiresInSeconds` | int32 | — | 900 |
| `resendAfterSeconds` | int32 | — | 60 |
| `maskedPhone` | string | **20** | Enmascarado, para que la app confirme a qué número se envió sin exponerlo entero en pantalla compartida |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `phone.invalid` | No son 10 dígitos mexicanos válidos |
| 409 | `phone.already_in_use` | Ese celular ya está confirmado en **otra** cuenta |
| 429 | `phone.too_many_codes` | 3 cada 15 min, 10 al día (política `otp`, partición `sub` + celular) |
| 403 | `account.pending_deletion` | |
| 503 | `server.dependency_unavailable` | Redis o la pasarela de SMS no responden |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `findOne({ phone, phoneVerifiedAt: { $exists: true }, _id: { $ne: sub } })` para detectar duplicado | `phone` único **disperso** |
| `accounts` | `updateOne` de `phonePending` (el número aún **no** verificado) | `_id` |

| Clave Redis | Estructura | TTL | Contenido |
|---|---|---|---|
| `nexo:otp:phone:{accountId}` | hash | **900 s** | `codeHash`, `attempts: 0`, `phone` |
| `nexo:otp:resend:phone:{accountId}` | string | **60 s** | |

El número pendiente se guarda en `phonePending`, **no** en `phone`: hasta que se confirme, no cuenta
para el índice único ni para la insignia.

`audit_events`: `phone.verification_requested` `{ accountId, phoneHash }`.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`otp`**, partición `sub` + celular, exacto en Redis. Es cuota aparte de la del correo:
  son recursos distintos y comparten solo la intención de frenar el abuso.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Redis** | Código hasheado, intentos, ventana de reenvío, contadores |
| Pasarela de SMS | Envío del código. **Pendiente de ADR**: Firebase Phone Auth queda disponible solo para esto, o un proveedor de SMS. Es el único resto de Firebase Auth tras el ADR 0010 |
| Outbox → Storage Queue | El envío se encola; el handler responde `202` |
| **Azure Key Vault** | Llave HMAC y credenciales de la pasarela |

> **Decisión abierta**: si el costo del SMS no se justifica, el celular puede quedarse como
> *declarado* (sin confirmación), y entonces la insignia *Teléfono confirmado* no se enciende. Eso
> cambia una señal de confianza de la maqueta y por tanto **requiere ADR y aprobación**.

## 8. Seguridad y privacidad

1. El número solo pasa a `phone` **confirmado**: antes vive en `phonePending`, así que no bloquea el
   índice único ni enciende la insignia.
2. El duplicado se comprueba solo contra celulares **ya verificados**: un número sin confirmar en otra
   cuenta no puede usarse para bloquear a nadie.
3. `maskedPhone` evita mostrar el número completo en una pantalla que alguien más pueda ver.
4. En los logs el celular va como `sha256(phone)[0..16]`, igual que el correo.
5. El celular **nunca** sale en una respuesta ajena: solo en la propia cuenta y en la acción explícita
   "Copiar número" (constitución §III.6), que queda auditada en `contact_log`.
