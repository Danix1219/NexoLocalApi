# Marcar como resuelto · `POST /v1/engagement/orders/{id}/resolve`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **Pr** (el prestador del pedido) |
| Pantalla de la maqueta | 7.15 Pedido → "Marcar como resuelto" · `ACT.mdone` |
| Spec SDD | nv-07 |
| Idempotente | No — transición de estado |

## 1. Función

El prestador declara terminado el servicio. Es el estado terminal "bueno": cierra el pedido y
**habilita al cliente a calificar**.

El control `msimdone` de la maqueta era de **demostración** y no se publica como capacidad de usuario
final (constitución §9).

## 2. Petición

```http
POST /v1/engagement/orders/01940208-…/resolve
Authorization: Bearer <access_token>
If-Match: "2"
Content-Type: application/json
```

```json
{ "finalPrice": { "amountCents": 45000, "includes": "Material y mano de obra" }, "note": "Se cambió el sifón completo" }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Pedido **`accepted`** del que es prestador |
| `finalPrice.amountCents` | int32 | **0 – 99 999 900** | no | Si no se había fijado precio al aceptar, se puede registrar aquí. **Informativo** |
| `finalPrice.includes` | string | **200** | no | **Moderado** |
| `note` | string | **200** | no | Qué se hizo. **Moderada** |

## 3. Respuesta

`200 OK`

```json
{
  "orderId": "01940208-…",
  "status": "resolved",
  "statusText": "Resuelto",
  "resolvedAt": "2026-11-05T21:00:00Z",
  "version": 3,
  "agreedPrice": { "amountCents": 45000, "text": "$450.00", "includes": "Material y mano de obra" },
  "clientCanReview": true,
  "allowedActions": ["message", "copyPhone"]
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `clientCanReview` | bool | `true`: el cliente ya puede calificar este pedido |
| `allowedActions` | array | Queda el chat y copiar número: la relación sigue existiendo tras el servicio |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe o no eres el prestador |
| 409 | `order.invalid_transition` | No está `accepted`. **Un pedido `pending` no se resuelve**: primero hay que aceptarlo |
| 412 | `order.version_conflict` | `If-Match` no coincide |
| 400 | `order.price_out_of_range` | Mayor que $999 999.00 |
| 403 | `content.inappropriate` | Content Safety rechazó la nota o `includes` |

## 5. Datos

```js
db.orders.findOneAndUpdate(
  { _id: id, providerId: miPerfil, status: "accepted" },
  { $set: { status: "resolved", resolvedAt: now, agreedPrice, resolveNote, version: v + 1 } },
  { returnDocument: "after" }
)
```

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `findOneAndUpdate` con estado en el filtro; el par queda **libre** en el índice único parcial | `{providerId, status, createdAt: -1}` |
| `order_events` | Evento `resolved` | único `{streamId, version}` |

Escrituras derivadas:

- `outbox`: **`order.resolved`** `{ orderId, clientId, providerId, requestId? }` → el worker:
  - notifica al cliente: "Tu servicio se marcó como resuelto. ¿Cómo te fue?" con enlace a calificar;
  - si hay solicitud asociada, la cierra en Discovery (`status: "closed"`, `outcome: "solvedHere"`);
  - inserta mensaje de sistema en la conversación;
  - incrementa el contador de servicios del prestador.
- `audit_events`: `order.resolved`, con cadena de hash.

La dirección **sigue accesible** para el prestador: el pedido resuelto queda en su historial y puede
necesitar volver al mismo lugar por una garantía.

## 6. Caché y rate limit

- Sin caché. Invalida las listas de pedidos y el tag `provider:{id}`.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Transición condicionada al estado `accepted` |
| **Azure AI Content Safety** | Moderación de nota y `includes` |
| **FirebaseAdmin** (worker) | Push con la invitación a calificar |
| **Polly v8** | Cierre de la solicitud en Discovery |
| `audit_events` | Cadena de hash |

## 8. Seguridad y privacidad

1. **Solo el prestador marca resuelto**, y solo desde `accepted`: la secuencia
   `pending → accepted → resolved` no se puede saltar, lo que evita que alguien genere pedidos
   "resueltos" ficticios para acumular calificaciones.
2. El precio sigue siendo **informativo**: no hay cobro, comisión ni liquidación
   (constitución §2).
3. La invitación a calificar sale del servidor por evento: la opinión es del cliente y nace de un
   servicio que de verdad ocurrió, no de cualquier contacto.
4. La actividad del prestador **no se contabiliza como ventas verificadas**: es su propia actividad
   declarada en la plataforma.
5. El chat sigue abierto tras resolver: hace falta para garantías y dudas posteriores.
