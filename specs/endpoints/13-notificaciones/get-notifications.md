# Centro de notificaciones · `GET /v1/engagement/notifications`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Hoja de notificaciones (campana) · `notifSheet` |
| Spec SDD | nv-11 |
| Idempotente | Sí (lectura) |

## 1. Función

El historial de avisos del usuario. Replica los estados reales que la maqueta muestra en las
notificaciones de una solicitud:

> *Aún sin interesados* → *N prestadores interesados* (con botón "Ver y elegir") → *Elegiste a X*

Es el respaldo del push: lo que llegó al teléfono queda aquí, y lo que no llegó (teléfono apagado,
permiso denegado) se recupera al abrir la campana.

## 2. Petición

```http
GET /v1/engagement/notifications?filter=all&cursor=&limit=20
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `filter` | string | enum | no | `all` \| `unread`. Por defecto `all` |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "items": [
    {
      "notificationId": "0194060e-239a-74c5-d6e7-f8091a2b3c4d",
      "type": "interest.received",
      "title": "3 prestadores interesados",
      "body": "Ya puedes comparar y elegir a quién pedirle el servicio.",
      "deepLink": "nexolocal://requests/01940107-…/interested",
      "actionLabel": "Ver y elegir",
      "icon": "bell",
      "readAt": null,
      "ago": "hace 1 hora",
      "createdAt": "2026-11-05T19:00:00Z"
    }
  ],
  "nextCursor": null,
  "hasMore": false,
  "unreadCount": 2
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `type` | string | **40** | `interest.received` \| `order.created` \| `order.accepted` \| `order.rejected` \| `order.cancelled` \| `order.resolved` \| `order.address_changed` \| `message.received` \| `review.received` \| `work.recommended` \| `request.closed` \| `account.security` |
| `title` | string | **80** | |
| `body` | string | **200** | **Sin domicilio ni contenido sensible** |
| `deepLink` | string | **200** | Esquema `nexolocal://`; lo resuelve go_router |
| `actionLabel` | string | **40** | Texto del botón, si hay acción |

**No se devuelve nunca**: direcciones, teléfonos, precios acordados ni el texto de los mensajes de
chat. Una notificación lleva **referencias**, y el dato se consulta en su endpoint con la regla de
visibilidad aplicada.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `filter` fuera del enum |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `notifications` | `find({ accountId: sub, readAt? })` orden `_id: -1` | **`{accountId, createdAt: -1}`**; **TTL 90 días** |

Las escribe el **worker** al consumir los eventos del outbox, nunca un endpoint: así una notificación
no puede existir sin el hecho que la provocó.

`unreadCount` se resuelve con un `$facet` en el mismo *pipeline*.

El TTL de **90 días** es minimización de datos: un aviso de hace tres meses no sirve y ocupa espacio
del cluster Free.

### Agrupación

Las notificaciones del mismo `type` sobre el mismo objeto se **actualizan en vez de acumularse**:
el tercer prestador interesado no genera una tercera fila, actualiza la existente a "3 prestadores
interesados" y la devuelve a no leída. Eso es lo que hace coincidir el comportamiento con la
progresión de la maqueta.

```js
db.notifications.updateOne(
  { accountId, type: "interest.received", "ref.requestId": requestId },
  { $set: { title, body, readAt: null, createdAt: now } },
  { upsert: true }
)
```

`readPreference` primario.

## 6. Caché y rate limit

- **Sin caché.** `Cache-Control: private, no-store`.
- Rate limit **`user`** (120/min). Se actualiza por **SignalR** (grupo `user:{accountId}`), no por
  sondeo.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `notifications` con índice por cuenta y TTL de 90 días |
| **Azure SignalR** | Aviso en vivo cuando llega una notificación nueva |
| **FirebaseAdmin** (worker) | El push correspondiente al teléfono |
| Outbox → Storage Queue | Las notificaciones nacen de eventos, no de peticiones |

## 8. Seguridad y privacidad

1. El filtro es `accountId: sub`: **solo las propias**.
2. **El `body` nunca lleva datos sensibles.** Es la misma cadena que va al push y se lee en la
   pantalla bloqueada: sin dirección, sin teléfono, sin el texto del mensaje
   (constitución §7).
3. Las notificaciones llevan **`deepLink` y referencias**: el dato real se obtiene del endpoint
   correspondiente, donde se vuelve a comprobar el permiso. Una notificación vieja no entrega
   información a quien ya perdió el acceso.
4. El TTL de 90 días limita cuánto historial de actividad se conserva.
5. Las escribe solo el worker, a partir de eventos: no hay forma de inyectar una notificación falsa
   desde la API.
