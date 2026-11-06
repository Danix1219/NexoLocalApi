# Cambiar la dirección del servicio · `PUT /v1/engagement/orders/{id}/address`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C** (el cliente del pedido) |
| Pantalla de la maqueta | 7.15 Pedido → cambiar domicilio · `ACT.maddr` |
| Spec SDD | nv-07 |
| Idempotente | Sí (`PUT`: el mismo cuerpo deja el mismo estado) |

## 1. Función

El cliente corrige o cambia la dirección del servicio. El documento oficial lo exige explícitamente:
*"se conserva cancelación, cambio de domicilio durante estados permitidos y acceso posterior a la
experiencia"*.

**Estados permitidos: `pending` y `accepted`.** Un pedido resuelto, rechazado o cancelado no admite
cambio de dirección: ya no hay servicio por prestar.

## 2. Petición

```http
PUT /v1/engagement/orders/01940208-…/address
Authorization: Bearer <access_token>
If-Match: "2"
Content-Type: application/json
```

```json
{
  "address": "Av. Juárez 45, Col. Jardines",
  "addressNote": "Casa blanca con reja negra",
  "location": { "lat": 20.0601, "lng": -99.3355 }
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Pedido `pending` o `accepted` del que es cliente |
| `address` | string | **300** | sí | **Se cifra** antes de guardarse |
| `addressNote` | string | **120** | no | **Se cifra** junto con la dirección |
| `location.lat` / `.lng` | double | −90..90 / −180..180 | sí | Punto nuevo. **Privado** |

## 3. Respuesta

`200 OK`

```json
{
  "orderId": "01940208-…",
  "status": "accepted",
  "version": 3,
  "placeZone": "Jardines",
  "address": "Av. Juárez 45, Col. Jardines",
  "addressNote": "Casa blanca con reja negra",
  "location": { "lat": 20.0601, "lng": -99.3355 },
  "zoneChanged": true,
  "providerNotified": true
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `placeZone` | string | **60** | Recalculada con `areaLabel` |
| `zoneChanged` | bool | — | `true` si la zona cambió, no solo la calle |
| `providerNotified` | bool | — | `true` si el pedido estaba `accepted` y se avisó al prestador |

Al cliente se le devuelve la dirección en claro porque es suya.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe o **no eres el cliente** del pedido |
| 409 | `order.invalid_transition` | El pedido está `resolved`, `rejected` o `cancelled` |
| 400 | `order.address_required` | `address` vacía |
| 400 | `order.attention_is_shop` | El pedido es `attention = shop`: se atiende en el local, no hay domicilio que cambiar |
| 400 | `location.out_of_bounds` | Fuera del *viewbox* de México |
| 412 | `order.version_conflict` | `If-Match` no coincide |
| 429 | `rate.limit_exceeded` | Política `user` |

## 5. Datos

```js
db.orders.findOneAndUpdate(
  { _id: id, clientId: sub, status: { $in: ["pending", "accepted"] }, attention: "home" },
  { $set: { addressEnc, location, placeZone, version: v + 1 } },
  { returnDocument: "after" }
)
```

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `findOneAndUpdate` con cliente, estado y modalidad en el filtro | `{clientId, status, createdAt: -1}` |
| `order_events` | Evento **`addressChanged`**, con `data: { placeZone, zoneChanged }` — **nunca la dirección** | único `{streamId, version}` |

**El evento del historial no guarda la dirección**, ni la vieja ni la nueva: solo que cambió y a qué
zona. El historial de eventos es inmutable y de consulta más amplia que el pedido, así que meter ahí
un dato personal sería crear una segunda copia que nadie podría borrar.

Escrituras derivadas:

- `outbox`: **`order.address_changed`** `{ orderId, clientId, providerId, zoneChanged }` → si el
  pedido está `accepted`, el worker notifica al prestador ("El cliente cambió la dirección del
  servicio") **sin la dirección en el push**, e inserta un mensaje de sistema en la conversación.
- `audit_events`: `order.address_changed` con cadena de hash, sin la dirección.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure Key Vault** + **AES-256-GCM** | Cifrado de sobre de la dirección nueva |
| **MongoDB** | Transición con cliente, estado y modalidad en el filtro |
| **FirebaseAdmin** (worker) | Aviso al prestador, sin dirección |
| **SignalR** | Mensaje de sistema en la conversación |
| `audit_events` | Cadena de hash del cambio |

## 8. Seguridad y privacidad

1. **Solo el cliente cambia la dirección**: `clientId: sub` en el filtro. El prestador no puede
   modificar dónde debe ir.
2. La dirección nueva **se cifra igual que la original** y sigue sujeta a la misma regla de
   visibilidad: si el pedido está `pending`, el prestador verá solo la zona nueva.
3. **El historial de eventos no guarda direcciones.** Es inmutable; guardar ahí un dato personal
   impediría cumplir un borrado ARCO.
4. El push de aviso no lleva la dirección: el prestador la consulta en el detalle, con la regla
   aplicada.
5. `attention: "home"` en el filtro: no se "cambia el domicilio" de un servicio que se presta en el
   local del negocio.
6. Avisar al prestador cuando el pedido ya estaba aceptado es necesario: puede estar en camino.
