# Rechazar el pedido · `POST /v1/engagement/orders/{id}/reject`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **Pr** (el prestador **de ese pedido**) |
| Pantalla de la maqueta | 7.15 Pedido → "Rechazar" · `ACT.mno` |
| Spec SDD | nv-07 |
| Idempotente | No — transición de estado |

## 1. Función

El prestador declina el pedido. La solicitud asociada **vuelve a quedar abierta**, para que el cliente
pueda elegir a otro de los interesados sin volver a publicarla.

Solo se puede rechazar desde `pending`: un pedido ya aceptado se deja con `cancel`, que es distinto
porque el cliente ya contaba con el servicio.

## 2. Petición

```http
POST /v1/engagement/orders/01940208-…/reject
Authorization: Bearer <access_token>
If-Match: "1"
Content-Type: application/json
```

```json
{ "reason": "noAvailability", "note": "Esta semana ya no tengo espacio" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Pedido `pending` del que es prestador |
| `reason` | string | enum | no | `noAvailability` \| `outOfArea` \| `notMyService` \| `priceDisagreement` \| `other` |
| `note` | string | **200** | no | Mensaje al cliente. **Moderado** |

## 3. Respuesta

`200 OK`

```json
{
  "orderId": "01940208-…",
  "status": "rejected",
  "statusText": "Rechazado",
  "rejectedAt": "2026-11-05T18:50:00Z",
  "version": 2,
  "requestReopened": true,
  "allowedActions": []
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `requestReopened` | bool | `true` si la solicitud volvió a `open` |
| `allowedActions` | array | **Vacío**: un pedido rechazado es terminal |

**No se devuelve nunca**: la dirección. Rechazar **no** la desbloquea, y si el pedido estaba
`pending` el prestador nunca la tuvo.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe o no eres el prestador |
| 409 | `order.invalid_transition` | No está `pending` (ya aceptado, cancelado o resuelto) |
| 412 | `order.version_conflict` | `If-Match` no coincide |
| 403 | `content.inappropriate` | Content Safety rechazó la nota |

## 5. Datos

```js
db.orders.findOneAndUpdate(
  { _id: id, providerId: miPerfil, status: "pending" },
  { $set: { status: "rejected", rejectedAt: now, rejectReason, rejectNote, version: 2 } },
  { returnDocument: "after" }
)
```

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `findOneAndUpdate` con estado en el filtro. Al pasar a `rejected`, **el par queda libre** en el índice único parcial | `{providerId, status, createdAt: -1}` |
| `order_events` | Evento `rejected`, versión siguiente | único `{streamId, version}` |

Escrituras derivadas:

- `outbox`: **`order.rejected`** `{ orderId, clientId, providerId, requestId?, reason }` → el worker:
  - en Discovery, devuelve la solicitud a `status: "open"` y limpia `pickedProviderId`
    (`findOneAndUpdate({ _id, status: "assigned", pickedProviderId: providerId })`, condicionado para
    no reabrir una solicitud que el cliente ya cerró);
  - notifica al cliente: "Tu pedido fue rechazado. Puedes elegir a otro prestador";
  - inserta mensaje de sistema en la conversación.
- `audit_events`: `order.rejected`, con cadena de hash.

**La dirección cifrada se conserva** (es del cliente y el pedido queda en su historial), pero el
prestador rechazado nunca podrá descifrarla: la regla de visibilidad exige `accepted`/`resolved`.

## 6. Caché y rate limit

- Sin caché. Invalida las listas de pedidos y las oportunidades de la solicitud reabierta.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Transición condicionada; liberación del índice único parcial |
| **Azure AI Content Safety** | Moderación de la nota |
| **Polly v8** | Reapertura de la solicitud en Discovery, por contrato |
| **FirebaseAdmin** (worker) | Push al cliente |
| **SignalR** | Mensaje de sistema |

## 8. Seguridad y privacidad

1. Estado en el filtro: no se puede rechazar un pedido ya aceptado por esta vía, que es lo que
   permitiría a un prestador deshacerse de un compromiso sin que quede como cancelación.
2. **Rechazar no revela nada**: la dirección sigue cifrada e inaccesible para él.
3. La reapertura de la solicitud va **condicionada** a que siga `assigned` con ese prestador: si el
   cliente ya la cerró, no se reabre contra su voluntad.
4. El cliente **sí** se notifica: a diferencia de retirar un interés, aquí había un compromiso
   pendiente y la espera termina.
5. `reason` es un enum para poder medir por qué se rechazan pedidos sin texto libre obligatorio.
