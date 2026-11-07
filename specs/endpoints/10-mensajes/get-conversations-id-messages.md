# Historial de mensajes · `GET /v1/engagement/conversations/{id}/messages`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **parte** de la conversación |
| Pantalla de la maqueta | 7.16 Chat a pantalla completa · `V.chat` |
| Spec SDD | nv-08 |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve los mensajes de una conversación, paginados **hacia atrás** (del más reciente al más
antiguo), que es como se lee un chat. Cubre también la recuperación del historial tras una
desconexión, que el documento oficial exige.

## 2. Petición

```http
GET /v1/engagement/conversations/01940309-…/messages?before=&limit=30
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | El `sub` debe ser parte |
| `before` | string | **64** | no | Cursor: devuelve los **anteriores** a ese punto. Sin él, los más recientes |
| `after` | string | **64** | no | Para **recuperar tras desconexión**: los posteriores al último recibido |
| `limit` | int32 | **1–50** | no | Por defecto **30** |

`before` y `after` son excluyentes entre sí; enviar los dos → `400`.

## 3. Respuesta

`200 OK`

```json
{
  "conversationId": "01940309-…",
  "counterpart": { "id": "019400d4-…", "displayName": "Plomería del Centro", "photoUrl": "…", "kind": "provider" },
  "items": [
    {
      "messageId": "0194040b-f067-7192-a3b4-c5d6e7f8091a",
      "kind": "text",
      "text": "Puedo pasar mañana a las 9",
      "mine": false,
      "senderId": "019400d4-…",
      "createdAt": "2026-11-05T19:30:00Z",
      "readAt": null
    },
    {
      "messageId": "0194040a-…",
      "kind": "system",
      "system": { "type": "orderAccepted", "orderId": "01940208-…" },
      "text": "El prestador aceptó tu pedido",
      "mine": false,
      "createdAt": "2026-11-05T18:41:00Z"
    },
    {
      "messageId": "01940409-…",
      "kind": "image",
      "imageUrl": "https://cdn.nexolocal.mx/ch/7a1b2c3d.webp",
      "thumbUrl": "https://cdn.nexolocal.mx/ch/7a1b2c3d_t.webp",
      "w": 1200, "h": 900,
      "mine": true,
      "createdAt": "2026-11-05T18:20:00Z",
      "readAt": "2026-11-05T18:22:00Z"
    }
  ],
  "nextCursor": "MDE5NDA0MDkt…",
  "hasMore": true,
  "activeOrder": { "orderId": "01940208-…", "status": "accepted", "statusText": "Aceptado" },
  "blocked": false,
  "canSend": true,
  "quickReplies": ["De acuerdo", "¿A qué hora?", "Gracias"]
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `kind` | string | enum | `text` \| `image` \| `system` |
| `text` | string | **1000** | En `system`, el texto ya compuesto del aviso |
| `mine` | bool | — | Para alinear la burbuja sin comparar ids en la app |
| `readAt` | date \| null | — | Solo en los mensajes propios: cuándo los leyó la otra parte |
| `activeOrder` | object \| null | — | Avisos del pedido dentro del chat |
| `canSend` | bool | — | `false` si hay bloqueo o la cuenta está en baja |

**No se devuelve nunca**: teléfono ni dirección en el cuerpo de los mensajes de sistema. Un aviso de
pedido lleva el `orderId` y la app consulta el detalle, donde se aplica la regla de visibilidad.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `conversation.not_found` | No existe **o no eres parte** |
| 400 | `validation_failed` | `before` y `after` juntos, o `limit` fuera de rango |
| 400 | `request.cursor_invalid` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `conversations` | `findOne({ _id: id, $or: [{ clientId: sub }, { providerId: miPerfil }] })` — **participación en el filtro** | único `{clientId, providerId}` |
| `messages` | `find({ conversationId: id, _id: { $lt: before } })` orden `_id: -1` | **`{conversationId, createdAt: -1}`** |

Al ser `_id` un UUID v7, el orden por `_id` es cronológico y el cursor no necesita campo de
desempate.

La comprobación de participación se hace **primero y en una sola consulta**; si falla, no se toca
`messages`.

`readPreference` primario: un chat es lo último que debe leerse de una réplica atrasada.

## 6. Caché y rate limit

- **Sin caché, a ningún nivel.** `Cache-Control: private, no-store`. Los mensajes privados no se
  cachean ni en Redis ni en el CDN.
- Rate limit **`user`** (120/min). El chat en vivo llega por **SignalR**; este endpoint es para la
  carga inicial, el desplazamiento hacia atrás y la recuperación tras desconexión.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `messages` por conversación, orden por `_id` |
| **Azure SignalR** | Mensajes nuevos en vivo; este endpoint no se sondea |
| **Front Door CDN** | Imágenes del chat, **ya moderadas**, con URL por hash |
| **Polly v8** | Timeout al pedir el nombre de la contraparte |

## 8. Seguridad y privacidad

1. **Participación en el filtro de la consulta**: una sola ida a la base resuelve existencia y
   permiso. `404` en vez de `403` para no confirmar conversaciones ajenas.
2. **Nunca se cachean mensajes privados.** Es la razón por la que este endpoint no tiene L2 ni ETag.
3. Las imágenes del chat están en el CDN con nombre por hash, no enumerable; solo se sirven las
   aprobadas por moderación.
4. Los mensajes de sistema llevan **referencias** (`orderId`), no datos sensibles: así la regla de
   visibilidad de la dirección se aplica siempre en un único lugar.
5. `readAt` solo se expone en los mensajes propios: saber cuándo leyó la otra parte es legítimo; que
   un tercero lo sepa, no.
