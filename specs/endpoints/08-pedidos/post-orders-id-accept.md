# Aceptar el pedido · `POST /v1/engagement/orders/{id}/accept`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **Pr** (el prestador **de ese pedido**) |
| Pantalla de la maqueta | 7.15 Pedido → "Aceptar" con precio acordado opcional · `ACT.mok` |
| Spec SDD | nv-07 |
| Idempotente | No — es una transición de estado. El segundo intento da `409` |

## 1. Función

El prestador acepta y queda asignado. **Es la transición que desbloquea la dirección exacta**
(constitución §III.7) y el nombre completo del cliente.

Puede indicar un **precio acordado opcional**, que es información del pedido: Nexo Local no lo cobra
ni lo procesa (constitución §2).

## 2. Petición

```http
POST /v1/engagement/orders/01940208-cd34-7e56-9f78-0a1b2c3d4e5f/accept
Authorization: Bearer <access_token>
If-Match: "2"
Content-Type: application/json
```

```json
{ "agreedPrice": { "amountCents": 45000, "includes": "Material y mano de obra" } }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Pedido `pending` del que es prestador |
| `agreedPrice.amountCents` | int32 | **0 – 99 999 900** | no | Centavos MXN. Máximo $999 999.00 |
| `agreedPrice.includes` | string | **200** | no | Qué incluye. **Moderado** |
| `If-Match` (header) | string | **8** | no | `version` esperada. Si no coincide → `412` |

## 3. Respuesta

`200 OK`

```json
{
  "orderId": "01940208-…",
  "status": "accepted",
  "statusText": "Aceptado",
  "acceptedAt": "2026-11-05T18:41:00Z",
  "version": 2,
  "agreedPrice": { "amountCents": 45000, "text": "$450.00", "includes": "Material y mano de obra" },
  "address": "Calle Hidalgo 123, interior 4, Col. Centro",
  "addressNote": "Portón verde, tocar dos veces",
  "location": { "lat": 20.0539, "lng": -99.3403 },
  "client": { "displayName": "Ana López", "photoUrl": null, "canCopyPhone": true },
  "allowedActions": ["resolve", "cancel", "message", "copyPhone"]
}
```

La respuesta **ya trae la dirección**: aceptar es justo el momento en que se habilita, y así la app
no necesita otra llamada para mostrar el mapa y "Cómo llegar".

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe o **no eres el prestador de ese pedido** |
| 409 | `order.already_accepted` | Ya estaba aceptado. El cuerpo trae la fecha |
| 409 | `order.invalid_transition` | Está `rejected`, `cancelled` o `resolved` |
| 412 | `order.version_conflict` | `If-Match` no coincide: alguien cambió el pedido mientras tanto |
| 400 | `order.price_out_of_range` | Mayor que $999 999.00 o negativo |
| 403 | `content.inappropriate` | Content Safety rechazó `includes` |
| 403 | `account.pending_deletion` | |

## 5. Datos

```js
db.orders.findOneAndUpdate(
  { _id: id, providerId: miPerfil, status: "pending" },     // id + papel + estado, en un solo filtro
  { $set: { status: "accepted", acceptedAt: now, agreedPrice, version: 2 } },
  { returnDocument: "after" }
)
```

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `findOneAndUpdate` con estado en el filtro | `{providerId, status, createdAt: -1}` |
| `order_events` | `insertOne { streamId: id, version: 2, type: "accepted", actorId, data: { agreedPrice }, prevHash, hash }` | **único `{streamId, version}`** |

- Si `findOneAndUpdate` devuelve `null`, se hace **una** consulta de diagnóstico para distinguir
  `404` de `409`.
- El índice único `{streamId, version}` es la **concurrencia optimista real**: dos aceptaciones
  simultáneas pueden pasar el `findOneAndUpdate`, pero solo una inserta la versión 2.
- `prevHash` = `hash` del evento anterior; `hash` = SHA-256 de `(prevHash, version, type, data, actorId, occurredAt)`.

Escrituras derivadas:

- `outbox`: **`order.accepted`** `{ orderId, clientId, providerId, agreedPrice? }` → el worker:
  - notifica al cliente ("Tu pedido fue aceptado"), **sin precio en el cuerpo del push**;
  - inserta un mensaje de sistema en la conversación (`kind: "system"`, `system.type: "orderAccepted"`);
  - si el pedido venía de una solicitud, confirma el `assigned` en Discovery.
- `audit_events`: `order.accepted` y, al entregar la dirección, `order.address_revealed`.

## 6. Caché y rate limit

- Sin caché. Invalida las listas de pedidos de ambas partes.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Transición con estado en el filtro; `order_events` con índice único de versión |
| **Azure Key Vault** + AES-256-GCM | Descifrado de la dirección para devolverla aquí |
| **Azure AI Content Safety** | Moderación de `includes` |
| **SignalR** | Mensaje de sistema en la conversación, en vivo |
| **FirebaseAdmin** (worker) | Push al cliente |
| `audit_events` | Cadena de hash de la transición y de la revelación |

## 8. Seguridad y privacidad

1. **Id, papel y estado en un único filtro**: no se lee el pedido, se comprueba y luego se escribe.
   Eso cierra la carrera entre aceptar y cancelar.
2. El **índice único de `{streamId, version}`** garantiza que el historial no tenga dos versiones 2,
   aunque lleguen dos peticiones a la vez.
3. La dirección se entrega **en la respuesta de la transición que la habilita**, y queda auditada.
4. El precio acordado es **informativo**: el modelo no tiene estado de pago, ni cobro, ni liquidación.
   Un `int32` en centavos, y nada más.
5. El push al cliente no lleva el precio ni la dirección: se lee en la pantalla bloqueada.
6. `If-Match` permite a la app detectar que el pedido cambió desde que pintó la pantalla, en vez de
   sobrescribir a ciegas.
