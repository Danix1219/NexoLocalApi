# Cancelar el pedido · `POST /v1/engagement/orders/{id}/cancel`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (cualquiera de las dos partes) |
| Pantalla de la maqueta | 7.15 Pedido → "Cancelar" · `ACT.mcancel` |
| Spec SDD | nv-07 |
| Idempotente | No — transición de estado |

## 1. Función

Cancela un pedido `pending` o `accepted`. **Las dos partes pueden cancelar**: el cliente porque ya no
necesita el servicio, el prestador porque le surgió un impedimento después de haber aceptado.

Se registra **quién** canceló, porque no es lo mismo que el cliente se eche atrás que el prestador
deje colgado un servicio aceptado.

## 2. Petición

```http
POST /v1/engagement/orders/01940208-…/cancel
Authorization: Bearer <access_token>
If-Match: "2"
Content-Type: application/json
```

```json
{ "reason": "Ya lo resolvió otra persona" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Pedido `pending` o `accepted` donde el `sub` es parte |
| `reason` | string | **200** | no | Texto libre. **Moderado** |

## 3. Respuesta

`200 OK`

```json
{
  "orderId": "01940208-…",
  "status": "cancelled",
  "statusText": "Cancelado por el cliente",
  "cancelledAt": "2026-11-05T19:10:00Z",
  "cancelledBy": "client",
  "version": 3,
  "requestReopened": true,
  "allowedActions": []
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `cancelledBy` | string | enum `client` \| `provider`. Queda en el historial |
| `requestReopened` | bool | `true` si la solicitud volvió a `open`. Solo cuando canceló el **prestador**: si cancela el cliente, su solicitud no se reabre sola |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe o **no eres parte** |
| 409 | `order.invalid_transition` | Ya está `resolved`, `rejected` o `cancelled`. **Un pedido resuelto no se cancela** |
| 412 | `order.version_conflict` | `If-Match` no coincide |
| 403 | `content.inappropriate` | Content Safety rechazó el motivo |

## 5. Datos

```js
db.orders.findOneAndUpdate(
  { _id: id,
    $or: [{ clientId: sub }, { providerId: miPerfil }],
    status: { $in: ["pending", "accepted"] } },
  { $set: { status: "cancelled", cancelledAt: now, cancelledBy, cancelReason, version: v + 1 } },
  { returnDocument: "after" }
)
```

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `findOneAndUpdate`; al pasar a `cancelled` **el par queda libre** en el índice único parcial | `{clientId, status, createdAt: -1}` · `{providerId, status, createdAt: -1}` |
| `order_events` | Evento `cancelled` con `actorId` y `cancelledBy` | único `{streamId, version}` |

`cancelledBy` se deduce del `sub` comparado con `clientId`/`providerId`: **no se acepta del cuerpo**,
porque sería trivial atribuir la cancelación a la otra parte.

Escrituras derivadas:

- `outbox`: **`order.cancelled`** `{ orderId, clientId, providerId, cancelledBy, requestId? }` →
  el worker:
  - notifica **a la otra parte** (nunca a quien canceló);
  - si canceló el prestador y la solicitud sigue `assigned` con él, la devuelve a `open`;
  - inserta mensaje de sistema en la conversación.
- `audit_events`: `order.cancelled`, con cadena de hash.

La dirección cifrada **se conserva**. Si el prestador ya la había visto (pedido `accepted`), su acceso
se mantiene para ese pedido cancelado: ya la conoce, ocultarla no desharía nada y el historial debe
ser consistente. Si se canceló en `pending`, nunca la tuvo y sigue sin tenerla.

## 6. Caché y rate limit

- Sin caché. Invalida las listas de pedidos de ambas partes y, si se reabrió, las oportunidades.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Transición con participación y estado en el filtro |
| **Azure AI Content Safety** | Moderación del motivo |
| **Polly v8** | Reapertura condicionada de la solicitud en Discovery |
| **FirebaseAdmin** (worker) | Push a la otra parte |
| `audit_events` | Cadena de hash con el autor de la cancelación |

## 8. Seguridad y privacidad

1. **`cancelledBy` lo determina el servidor** desde el `sub`. Es el dato que distingue a quien
   cancela de quien recibe la cancelación, así que no puede venir del cliente.
2. Participación y estado en el mismo filtro: un pedido `resolved` no se puede "deshacer"
   cancelándolo, lo que borraría la base de una calificación.
3. Solo se avisa **a la otra parte**: notificar a quien acaba de pulsar el botón es ruido.
4. La reapertura de la solicitud se condiciona al estado y al prestador, para no reabrir algo que el
   cliente cerró.
5. El acceso a la dirección ya concedido no se revoca: fingir que se "desentrega" un dato que la otra
   parte ya leyó sería una garantía falsa. Lo que importa es que quedó auditado quién la vio.
