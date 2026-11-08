# Copiar número · `POST /v1/engagement/contacts/phone`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C** · o **Pr** con pedido aceptado |
| Pantalla de la maqueta | Hoja "Contactar" → "Copiar número" · `ACT.copy`, `ACT.chanclick` |
| Spec SDD | nv-18 |
| Idempotente | No — cada llamada es un acceso que se audita |

## 1. Función

**La única vía por la que un número de celular sale de la API.** La constitución §III.6 lo dice así:
*"el número de celular nunca aparece en ninguna respuesta de API, salvo la acción explícita
'Copiar número', limitada y registrada en `ContactLog`"*.

Por eso este endpoint existe en lugar de incluir el teléfono en el perfil: convierte el acceso en un
**acto deliberado, limitado y auditado**, en vez de un campo que viaja en cada respuesta.

## 2. Petición

```http
POST /v1/engagement/contacts/phone
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "targetId": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01", "targetType": "provider", "context": "profile" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `targetId` | uuid v7 | — | sí | Perfil del prestador, o cuenta del cliente si quien pide es el prestador |
| `targetType` | string | enum | sí | `provider` \| `client` |
| `context` | string | enum | no | `profile` \| `chat` \| `order` \| `interested`. Solo para la auditoría |

## 3. Respuesta

`200 OK`

```json
{
  "phone": "+527715550100",
  "displayPhone": "771 555 0100",
  "targetName": "Plomería del Centro",
  "remainingToday": 39,
  "loggedAt": "2026-11-05T20:00:00Z"
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `phone` | string | **13** | E.164, para `tel:` |
| `displayPhone` | string | **20** | Formato legible |
| `remainingToday` | int32 | — | Cuántos accesos quedan hoy (de 40) |
| `loggedAt` | date | — | **Se dice al usuario que el acceso quedó registrado** |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 403 | `contact.phone_not_available` | El prestador **no activó** el canal de llamada (`channels.call: false`) o no tiene celular confirmado |
| 403 | `contact.not_allowed` | Un prestador pide el número de un cliente **sin pedido aceptado** |
| 404 | `resource.not_found` | El objetivo no existe o está despublicado |
| 429 | `contact.daily_limit` | Política `contact`: **40 al día** |
| 403 | `account.pending_deletion` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `contact_log` | `insertOne { actorId, targetId, channel: "copyPhone", context, createdAt }` | `{actorId, createdAt: -1}`; **TTL 1 año** |
| `audit_events` | Evento `contact.phone_copied`, **con cadena de hash** | `{actorId, occurredAt}` |
| `accounts` (Identity) | El teléfono se obtiene **por contrato**: `IPhoneDisclosureContract.GetPhoneAsync(accountId, reason)` | `_id` |
| `orders` | Si quien pide es el prestador: `countDocuments({ clientId: targetId, providerId: miPerfil, status: { $in: ["accepted", "resolved"] } })` | `{providerId, status, createdAt: -1}` |
| `provider_profiles` | `channels.call` y `phoneVerified` del objetivo | `_id` |

El contrato de Identity **registra el motivo**: el número no se puede pedir desde otro módulo sin
decir para qué, y ese motivo queda en el log de Identity además del de Engagement.

Escrituras derivadas:

- `outbox`: **`contact.phone_copied`** `{ actorId, targetId }` → el worker incrementa
  `provider_profiles.stats.contactClicks` (la estadística "clics de contacto" del perfil propio).
- **No se notifica al dueño del número** en el momento. Se refleja en su estadística agregada de
  Cuenta, que es lo que la maqueta muestra.

## 6. Caché y rate limit

- **Sin caché, nunca.** Cachear esta respuesta sería guardar un teléfono en Redis.
- Rate limit **`contact`**: **40 al día** por `sub`, contador exacto en Redis.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Polly v8** | Timeout + circuit breaker en el contrato con Identity |
| **MongoDB** | `contact_log` con TTL de 1 año; `audit_events` encadenado |
| **Redis** | Contador exacto del límite diario |
| Outbox → Storage Queue | Estadística del prestador |

## 8. Seguridad y privacidad

1. **Es el único punto de salida de un teléfono.** Concentrarlo en un endpoint hace que la regla se
   pueda auditar en un solo sitio, en lugar de revisar cada DTO del sistema.
2. **Doble condición para el prestador**: solo puede pedir el número de un cliente si hay un pedido
   `accepted` o `resolved` entre ambos. Un prestador no obtiene teléfonos de quienes publican
   solicitudes.
3. **Para el cliente, la condición es el consentimiento del prestador**: `channels.call` lo activó él
   y requiere celular confirmado.
4. **Cada acceso queda registrado dos veces**: en `contact_log` (consultable, con TTL de 1 año) y en
   `audit_events` con cadena de hash (inalterable). Si alguien recibe una llamada no deseada, hay
   rastro de quién obtuvo el número y cuándo.
5. El **límite de 40 al día** impide usar la plataforma como directorio telefónico: es suficiente
   para un uso normal y bajo para una extracción masiva.
6. `loggedAt` en la respuesta le dice al usuario que el acceso quedó registrado. Saberlo es en sí
   mismo un desincentivo del mal uso.
7. Sin caché a ningún nivel, incluida la del cliente (`no-store`).
