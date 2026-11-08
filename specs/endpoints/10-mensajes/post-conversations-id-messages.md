# Enviar un mensaje · `POST /v1/engagement/conversations/{id}/messages`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **parte** de la conversación |
| Pantalla de la maqueta | 7.16 Chat → enviar texto, respuesta rápida o foto · `ACT.send` |
| Spec SDD | nv-08 |
| Idempotente | Sí — `Idempotency-Key` obligatorio (evita duplicados al reintentar con mala señal) |

## 1. Función

Envía un mensaje de texto o una imagen. Lo persiste, actualiza la bandeja de las dos partes y lo
difunde en vivo por SignalR.

La **respuesta automática simulada** del prototipo (el chat contestaba solo a los pocos segundos)
**no se implementa** (constitución §9).

## 2. Petición

```http
POST /v1/engagement/conversations/01940309-…/messages
Authorization: Bearer <access_token>
Idempotency-Key: 4a9c1e7b-6d35-4f02-91a8-2b4d6e8f0a13
Content-Type: application/json
```

```json
{ "kind": "text", "text": "De acuerdo, mañana a las 9 está bien" }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | El `sub` debe ser parte |
| `kind` | string | enum | sí | `text` \| `image`. **`system` lo genera solo el servidor** |
| `text` | string | **1–1000** | si `text` | **Moderado antes de guardar** |
| `imageUploadId` | uuid | — | si `image` | Subida propia, **aprobada** por moderación |

## 3. Respuesta

`201 Created`

```json
{
  "messageId": "0194040c-0178-72a3-b4c5-d6e7f8091a2b",
  "kind": "text",
  "text": "De acuerdo, mañana a las 9 está bien",
  "mine": true,
  "createdAt": "2026-11-05T19:35:00Z",
  "readAt": null,
  "deliveredToSocket": true
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `deliveredToSocket` | bool | `true` si la otra parte estaba conectada. `false` no es error: se envió push |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `conversation.not_found` | No existe o no eres parte |
| 403 | `conversation.blocked` | Alguna parte bloqueó a la otra |
| 403 | `content.inappropriate` | Content Safety rechazó el texto o la imagen |
| 400 | `validation_failed` | Texto vacío o de más de 1000 caracteres; `kind: system` |
| 404 | `upload.not_found` · 409 `upload.not_pending` | Imagen inválida o ajena |
| 403 | `account.pending_deletion` | |
| 429 | `rate.limit_exceeded` | Política **`chat`**: 60 mensajes/min |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `conversations` | `findOne({ _id: id, $or: [{ clientId: sub }, { providerId: miPerfil }] })` — participación y bloqueo en la misma consulta | único `{clientId, providerId}` |
| `messages` | `insertOne` | **`{conversationId, createdAt: -1}`** |
| `conversations` | `updateOne` de `lastMessageAt`, `lastPreview` y `$inc` del `unread` **de la otra parte** | `_id` |
| `uploads` | `findOne({ _id, ownerId: sub, status: "approved" })` si es imagen | `{ownerId, createdAt}` |

`lastPreview` se guarda ya recortado a 120 caracteres (o "Envió una foto"): la bandeja no vuelve a
leer `messages`.

El `$inc` del contador de no leídos es **solo del receptor**: `unread.provider` si escribe el
cliente, y al revés.

Escrituras derivadas:

- **SignalR**: se emite al grupo `conv:{conversationId}` el mensaje completo. Con dos réplicas, el
  backplane de **Redis** lo reparte.
- `outbox`: **`message.sent`** `{ conversationId, messageId, senderId, recipientId }` → si el receptor
  no estaba conectado, el worker envía push: "Mensaje nuevo de {nombre}", **sin el contenido**.

## 6. Caché y rate limit

- Sin caché. Invalida nada (la bandeja no se cachea).
- Rate limit **`chat`**: 60 mensajes/min por `sub`, en memoria por réplica.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure AI Content Safety** | Moderación del texto **síncrona, antes de persistir** |
| **Azure SignalR** | Difusión en vivo al grupo de la conversación |
| **Redis** | Backplane de SignalR entre réplicas; idempotencia |
| **MongoDB** | `messages` + actualización desnormalizada de la bandeja |
| **Front Door CDN** | Imagen ya aprobada, con URL por hash |
| **FirebaseAdmin** (worker) | Push cuando el receptor no está conectado |

## 8. Seguridad y privacidad

1. **Moderación antes de persistir**, no después: un mensaje ofensivo no llega al destinatario y
   luego se retira (constitución §III.5).
2. Participación y bloqueo se comprueban **en la misma consulta** que recupera la conversación.
3. `kind: system` está **reservado al servidor**: si se aceptara del cliente, cualquiera podría
   falsificar un aviso de "pedido aceptado".
4. **El push no lleva el contenido del mensaje**: se lee en la pantalla bloqueada
   (constitución §7).
5. `Idempotency-Key` evita el duplicado clásico de reintentar con mala cobertura.
6. El límite de 60/min frena el acoso por volumen sin molestar a una conversación normal.
