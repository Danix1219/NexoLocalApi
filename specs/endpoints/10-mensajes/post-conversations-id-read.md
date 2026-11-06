# Marcar como leídos · `POST /v1/engagement/conversations/{id}/read`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **parte** de la conversación |
| Pantalla de la maqueta | 7.16 Chat → al abrirlo, el globo de no leídos se limpia |
| Spec SDD | nv-08 |
| Idempotente | Sí |

## 1. Función

Pone a cero los no leídos del usuario en esa conversación y sella `readAt` en los mensajes de la otra
parte. Lo llama la app al abrir el chat y al volver a primer plano con el chat abierto.

## 2. Petición

```http
POST /v1/engagement/conversations/01940309-…/read
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "upToMessageId": "0194040c-0178-72a3-b4c5-d6e7f8091a2b" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | El `sub` debe ser parte |
| `upToMessageId` | uuid v7 | — | no | Marca hasta ese mensaje inclusive. Sin él, **todos** |

Aceptar `upToMessageId` evita marcar como leído algo que el usuario no alcanzó a ver si llegaron
mensajes nuevos mientras la petición viajaba.

## 3. Respuesta

`200 OK`

```json
{ "conversationId": "01940309-…", "unread": 0, "markedCount": 2, "totalUnread": 1 }
```

| Campo | Tipo | Regla |
|---|---|---|
| `unread` | int32 | Queda en 0 (o en los posteriores a `upToMessageId`) |
| `markedCount` | int32 | Cuántos se marcaron |
| `totalUnread` | int32 | Total del usuario, para el globo de la barra |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `conversation.not_found` | No existe o no eres parte |
| 401 | `auth.token_expired` | |
| 429 | `rate.limit_exceeded` | Política `user` |

**No hay error** si ya estaba todo leído: se responde `200` con `markedCount: 0`.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `conversations` | `findOneAndUpdate({ _id: id, $or: [{ clientId: sub }, { providerId: miPerfil }] }, { $set: { "unread.client": 0 } })` — **solo el campo del propio papel** | único `{clientId, providerId}` |
| `messages` | `updateMany({ conversationId: id, senderId: { $ne: sub }, readAt: null, _id: { $lte: upToMessageId } }, { $set: { readAt: now } })` | `{conversationId, createdAt: -1}` |

- Se pone a cero **solo `unread.client` o `unread.provider`**, según el papel del `sub`: nunca el de
  la otra parte.
- `senderId: { $ne: sub }` en el `updateMany`: no se marcan como leídos los propios mensajes.
- `readAt: null` evita reescribir los ya marcados, de modo que el sello conserva la hora de la
  primera lectura.

Escrituras derivadas:

- **SignalR**: se avisa al grupo `conv:{id}` del evento `read`, para que la otra parte vea los dobles
  checks sin recargar.
- **No** hay evento de outbox ni push: leer un mensaje no merece una notificación.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `updateMany` acotado por conversación, remitente y rango |
| **Azure SignalR** | Aviso de lectura en vivo |
| **Redis** | Backplane de SignalR |

## 8. Seguridad y privacidad

1. **Solo se tocan los contadores propios**: el campo `unread` que se pone a cero lo determina el
   papel del `sub`, no el cuerpo de la petición. Si no, alguien podría borrar los no leídos del otro.
2. Participación en el filtro, `404` si no es parte.
3. El acuse de lectura es un dato que la contraparte ya tiene derecho a conocer (le escribió a esa
   persona), así que difundirlo por SignalR es correcto; no se expone a terceros.
4. `upToMessageId` evita el falso "leído" en condiciones de red lenta, que es un problema de
   exactitud, no solo de comodidad.
5. Sin notificación: ya se cubre con el canal en vivo.
