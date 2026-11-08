# Pedir servicio · `POST /v1/engagement/orders`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C** (autenticado) |
| Pantalla de la maqueta | 7.15 Pedido de servicio — hoja "Pedir servicio" · `ACT.reqpick` |
| Spec SDD | nv-07 |
| Idempotente | Sí — `Idempotency-Key` obligatorio |

## 1. Función

Crea el pedido en estado **`pending`**. Es el momento en que una conversación se convierte en un
compromiso: el prestador tendrá que aceptarlo o rechazarlo.

Se puede pedir desde tres sitios, como en la maqueta: el **perfil** del prestador, el **chat**, o la
pantalla de **Interesados**.

Reglas duras:

- **Nunca dos asignaciones activas sobre la misma solicitud** (constitución §6).
- **Nunca dos pedidos activos entre el mismo cliente y el mismo prestador.**
- La **dirección se cifra** al guardarse y el prestador **no la recibe hasta aceptar**.

## 2. Petición

```http
POST /v1/engagement/orders
Authorization: Bearer <access_token>
Idempotency-Key: 9d4f2a6e-1c83-4b05-a2f7-3e5d6c7b8a90
Content-Type: application/json
```

```json
{
  "providerId": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01",
  "requestId": "01940107-bc23-7d45-8e67-9f01a2b3c4d5",
  "origin": "post",
  "description": "Fuga debajo del lavabo del baño principal.",
  "attention": "home",
  "address": "Calle Hidalgo 123, interior 4, Col. Centro",
  "addressNote": "Portón verde, tocar dos veces",
  "location": { "lat": 20.0539, "lng": -99.3403 }
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `providerId` | uuid v7 | — | sí | Perfil publicado |
| `requestId` | uuid v7 | — | no | Si viene, `origin` debe ser `post` y la solicitud ser del `sub` |
| `origin` | string | enum | sí | `post` (desde una solicitud) \| `direct` (perfil o chat) |
| `description` | string | **10–600** | sí | **Moderada** |
| `attention` | string | enum | sí | `home` \| `shop`. Debe ser compatible con el `attention` del prestador |
| `address` | string | **300** | si `home` | **Se cifra**; no se guarda en claro |
| `addressNote` | string | **120** | no | Referencias. **Se cifra junto con la dirección** |
| `location.lat` / `.lng` | double | −90..90 / −180..180 | si `home` | Punto del servicio. **Privado** |

## 3. Respuesta

`201 Created` · `Location: /v1/engagement/orders/{id}`

```json
{
  "orderId": "01940208-cd34-7e56-9f78-0a1b2c3d4e5f",
  "status": "pending",
  "statusText": "Esperando respuesta",
  "providerId": "019400d4-…",
  "providerName": "Plomería del Centro",
  "requestId": "01940107-…",
  "origin": "post",
  "attention": "home",
  "placeZone": "Centro",
  "createdAt": "2026-11-05T18:30:00Z",
  "version": 1,
  "allowedActions": ["cancel", "changeAddress", "message"]
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `placeZone` | string | **60** | **La zona, no la dirección.** Es lo que verá el prestador antes de aceptar |
| `version` | int32 | — | Versión del historial de eventos; se usa para concurrencia optimista |
| `allowedActions` | array | — | Lo que el servidor permite ahora mismo. La app pinta solo esos botones |

**No se devuelve nunca**: la dirección en claro (ni al cliente en este DTO; está en
[`GET /orders/{id}`](get-orders-id.md)), ni el teléfono del prestador.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `order.active_exists` | Ya hay un pedido `pending` o `accepted` con ese prestador (**índice único parcial**) |
| 409 | `order.request_already_assigned` | Esa solicitud ya tiene un prestador asignado |
| 404 | `provider.not_found` | No existe o está despublicado |
| 404 | `request.not_found` | La solicitud no existe o no es del `sub` |
| 400 | `order.address_required` | `attention = home` sin dirección |
| 400 | `order.attention_not_supported` | El prestador no atiende de ese modo |
| 403 | `content.inappropriate` | Content Safety rechazó la descripción |
| 429 | `rate.limit_exceeded` | Política `publish` |
| 403 | `account.pending_deletion` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `insertOne` | **único parcial `{clientId, providerId}` con `status ∈ {pending, accepted}`**; `{providerId, status, createdAt: -1}`; `{clientId, status, createdAt: -1}` |
| `order_events` | `insertOne` del evento `created`, `version: 1` | **único `{streamId, version}`** |
| `service_requests` (Discovery) | Por evento: `status → assigned`, `pickedProviderId` | — |

Documento:

```js
{
  _id: UUID_v7, clientId: UUID, providerId: UUID, requestId: UUID?,
  origin: "post", category: "plomeria", description: "…",
  attention: "home", placeZone: "Centro",
  addressEnc: BinData(0, "…"),          // cifrado de sobre; nunca en claro
  location: { type: "Point", coordinates: [lng, lat] },   // PRIVADO
  status: "pending", version: 1,
  createdAt: ISODate
}
```

### Las dos reglas de unicidad

1. **Un pedido activo por par cliente-prestador**: índice único **parcial**
   `{clientId: 1, providerId: 1}` con
   `partialFilterExpression: { status: { $in: ["pending", "accepted"] } }`.
   Al cerrarse el pedido (rechazado, cancelado, resuelto) el par queda libre otra vez.
2. **Una sola asignación por solicitud**: la transición de `service_requests` a `assigned` se hace con
   `findOneAndUpdate({ _id: requestId, status: "open" }, { $set: { status: "assigned", pickedProviderId } })`.
   Si devuelve `null`, otro pedido ganó la carrera → **se revierte** la inserción del pedido y se
   responde `409 order.request_already_assigned`.

Como son dos bases distintas (Engagement y Discovery) **no hay transacción**: el orden es insertar el
pedido, intentar la asignación por contrato, y compensar si falla. La compensación es segura porque el
pedido recién creado no tiene efectos visibles aún.

Escrituras derivadas:

- `outbox`: **`order.created`** `{ orderId, clientId, providerId, requestId?, placeZone }` → push al
  prestador ("Tienes un pedido nuevo en Centro"), **sin dirección**.
- `audit_events`: `order.created`, con cadena de hash.

## 6. Caché y rate limit

- Sin caché (dato vivo).
- Invalida la lista de pedidos de ambas partes y, si hubo solicitud, sus oportunidades.
- Rate limit **`publish`** por `sub`.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure Key Vault** | Llave maestra del **cifrado de sobre** de la dirección |
| **AES-256-GCM** | Cifrado de `addressEnc` con clave de datos envuelta por Key Vault |
| **Azure AI Content Safety** | Moderación de la descripción |
| **MongoDB** | Índice único parcial; `order_events` con `{streamId, version}` único |
| **Polly v8** | Timeout + circuit breaker en la asignación de la solicitud en Discovery |
| **FirebaseAdmin** (worker) | Push al prestador |
| **Redis** | Idempotencia y rate limit |

## 8. Seguridad y privacidad

1. **La dirección se cifra antes de llegar al disco** y solo se descifra en
   [`GET /orders/{id}`](get-orders-id.md) cuando el pedido está `accepted`/`resolved` **y** quien
   pregunta es el prestador de ese pedido. El cliente la ve siempre, porque es suya.
2. Antes de aceptar, el prestador recibe **`placeZone`** y nada más: así decide con la información
   suficiente sin que publicar un pedido revele dónde vive alguien.
3. El push de aviso **no lleva dirección ni nombre completo**: se lee en la pantalla bloqueada.
4. Las dos reglas de unicidad las garantizan **índices**, no comprobaciones previas: dos toques
   rápidos del botón no crean dos pedidos.
5. `order_events` con `prevHash`/`hash` encadenados: el historial del pedido es verificable y nadie
   puede reescribir quién hizo qué y cuándo.
6. `allowedActions` viene del servidor: la app no decide qué transiciones son válidas.
