# Abrir una conversación · `POST /v1/engagement/conversations`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C** siempre · **Pr** solo si fue elegido |
| Pantalla de la maqueta | "Contactar" → "Iniciar chat" · `ACT.chat` |
| Spec SDD | nv-08 |
| Idempotente | Sí — si ya existe, devuelve la misma conversación |

## 1. Función

Crea o recupera la conversación entre un cliente y un prestador. Es el canal de contacto principal
del producto, el que sustituyó a WhatsApp.

**Regla central**: *"el cliente inicia; el prestador solo si fue elegido"*. Un prestador no puede
escribirle a alguien porque vio su solicitud: eso convertiría publicar una necesidad en recibir
mensajes no solicitados.

## 2. Petición

```http
POST /v1/engagement/conversations
Authorization: Bearer <access_token>
Idempotency-Key: 6b3d8f1a-4c27-4e09-82b5-1d3e5f7a9c04
Content-Type: application/json
```

```json
{ "providerId": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01", "requestId": "01940107-…" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `providerId` | uuid v7 | — | sí si eres cliente | Perfil publicado |
| `clientId` | uuid v7 | — | sí si eres prestador | Solo válido si hay pedido activo o fue elegido |
| `requestId` | uuid v7 | — | no | Da contexto: el chat nace de esa solicitud |

## 3. Respuesta

`201 Created` (nueva) o `200 OK` (ya existía)

```json
{
  "conversationId": "01940309-ef56-7081-92a3-b4c5d6e7f809",
  "counterpart": {
    "id": "019400d4-…",
    "displayName": "Plomería del Centro",
    "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
    "kind": "provider"
  },
  "requestId": "01940107-…",
  "activeOrder": null,
  "created": true,
  "canSend": true,
  "quickReplies": ["¿Cuándo podrías venir?", "¿Cuánto costaría?", "¿Atiendes en mi zona?"]
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `created` | bool | — | `true` si se creó ahora |
| `canSend` | bool | — | Si puede enviar mensajes ya |
| `quickReplies` | array\<string 80> | **3** | Las respuestas rápidas de la maqueta, según el contexto |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 403 | `conversation.not_allowed` | **Un prestador intenta iniciar sin haber sido elegido** |
| 404 | `provider.not_found` | No existe o está despublicado |
| 403 | `conversation.blocked` | Alguna parte bloqueó a la otra |
| 400 | `conversation.self` | Intento de chatear consigo mismo |
| 403 | `account.pending_deletion` | |
| 429 | `rate.limit_exceeded` | Política `user` |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `conversations` | `updateOne({ clientId, providerId }, { $setOnInsert: { requestId, createdAt, unread: { client: 0, provider: 0 } } }, { upsert: true })` | **único `{clientId, providerId}`** |
| `interests` / `orders` | Si quien abre es el prestador: comprobación de que fue elegido | `{requestId, providerId}` único · `{providerId, status, createdAt: -1}` |

El **índice único `{clientId, providerId}`** con `upsert` es lo que hace este endpoint idempotente:
una conversación por par, sin importar cuántas veces se toque "Iniciar chat".

Comprobación de permiso para el prestador, en **una** consulta:

```js
db.orders.countDocuments({
  clientId, providerId: miPerfil,
  status: { $in: ["pending", "accepted", "resolved"] }
})
```

Si es 0 → `403 conversation.not_allowed`.

Escrituras derivadas:

- `outbox`: **`conversation.created`** `{ conversationId, clientId, providerId }`. **Sin push**: una
  conversación vacía no merece notificación; el aviso llega con el primer mensaje.
- Si el prestador tiene activado el canal de llamada, se registra `stats.contactClicks` en su perfil.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `upsert` con índice único por par |
| **Azure SignalR** | La contraparte recibe la conversación nueva en su bandeja, en vivo |
| **Redis** | Idempotencia; backplane de SignalR entre réplicas |

## 8. Seguridad y privacidad

1. **El prestador no puede iniciar el chat sin relación previa.** Es la regla que hace seguro publicar
   una solicitud, y se comprueba en el servidor con una consulta sobre `orders`.
2. Una conversación por par, garantizada por índice único: no se puede saturar a alguien abriendo
   hilos nuevos.
3. El bloqueo se respeta **en la apertura**, no solo al enviar: ni se crea el canal.
4. Sin notificación al crear: notificar una conversación vacía sería una vía para molestar sin
   escribir nada.
5. `quickReplies` las decide el servidor según el contexto, así que no se usan para colar texto
   arbitrario sin moderar.
