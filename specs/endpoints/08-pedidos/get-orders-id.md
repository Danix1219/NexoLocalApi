# Detalle del pedido · `GET /v1/engagement/orders/{id}`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (autenticado, **parte** del pedido) |
| Pantalla de la maqueta | 7.15 Pedido de servicio · `V.match` |
| Spec SDD | nv-07 |
| Idempotente | Sí (lectura) |

## 1. Función

El detalle completo del pedido. **Es el único endpoint que entrega la dirección del servicio**, y por
eso concentra la regla más delicada del producto:

> La dirección exacta solo la recibe el prestador **después** de `accepted`, y lo hace cumplir el
> servidor (constitución §III.7).

## 2. Petición

```http
GET /v1/engagement/orders/01940208-cd34-7e56-9f78-0a1b2c3d4e5f
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Requerido | Regla |
|---|---|---|---|
| `id` (ruta) | uuid v7 | sí | El `sub` debe ser el cliente o el prestador del pedido |

## 3. Respuesta

`200 OK`

```json
{
  "orderId": "01940208-…",
  "status": "accepted",
  "statusText": "Aceptado",
  "role": "provider",
  "counterpart": {
    "accountId": "019400a1-…",
    "displayName": "Ana López",
    "photoUrl": null,
    "kind": "client",
    "canCopyPhone": true
  },
  "category": "plomeria",
  "description": "Fuga debajo del lavabo del baño principal.",
  "attention": "home",
  "placeZone": "Centro",
  "address": "Calle Hidalgo 123, interior 4, Col. Centro",
  "addressNote": "Portón verde, tocar dos veces",
  "location": { "lat": 20.0539, "lng": -99.3403 },
  "agreedPrice": { "amountCents": 45000, "text": "$450.00", "includes": "Material y mano de obra" },
  "requestId": "01940107-…",
  "conversationId": "01940309-…",
  "timeline": [
    { "version": 1, "type": "created",  "actor": "client",   "at": "2026-11-05T18:30:00Z" },
    { "version": 2, "type": "accepted", "actor": "provider", "at": "2026-11-05T18:41:00Z" }
  ],
  "version": 2,
  "allowedActions": ["resolve", "cancel", "message", "copyPhone"],
  "canReview": false
}
```

### Tabla de visibilidad de la dirección

| Estado | Cliente | Prestador |
|---|---|---|
| `pending` | dirección completa | **`null`** · solo `placeZone` |
| `accepted` | dirección completa | **dirección completa** |
| `resolved` | dirección completa | dirección completa |
| `rejected` | dirección completa | **`null`** |
| `cancelled` | dirección completa | **`null`** si se canceló antes de aceptar; completa si ya la tenía |

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `address` | string \| null | **300** | Descifrado solo según la tabla |
| `location` | Point \| null | — | Misma regla que `address`: habilita el mapa y "Cómo llegar" |
| `displayName` | string | **80** | Completo desde `accepted`; iniciales antes |
| `canCopyPhone` | bool | — | `true` solo si hay pedido aceptado (o si el usuario es cliente y el prestador publicó el canal) |
| `timeline[]` | array | — | Historial de `order_events`, sin datos sensibles |
| `canReview` | bool | — | `true` para el cliente cuando el pedido está `resolved` y no ha calificado |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe **o no eres parte**. No se usa `403`: eso confirmaría que el pedido existe |
| 401 | `auth.token_expired` | |

`order.address_not_available` **no se devuelve aquí**: la dirección simplemente llega como `null`.
Ese `code` es para un endpoint que la pidiera explícitamente.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `findOne({ _id: id, $or: [{ clientId: sub }, { providerId: miPerfil }] })` — **id y participación en el mismo filtro** | `_id` |
| `order_events` | `find({ streamId: id })` ordenado por `version` | **único `{streamId, version}`** |
| `reviews` | `countDocuments({ orderId: id, clientId: sub })` para `canReview` | único disperso `orderId` |

El descifrado de `addressEnc` ocurre **después** de evaluar el estado y el papel:

```csharp
var canSeeAddress = order.ClientId == sub
                 || (order.ProviderId == myProviderId
                     && order.Status is Accepted or Resolved
                     || (order.Status is Cancelled && order.AcceptedAt is not null));

var address = canSeeAddress ? await _envelope.DecryptAsync(order.AddressEnc) : null;
```

Si `canSeeAddress` es `false`, **no se llama a Key Vault**: no hay descifrado que pueda filtrarse por
un log o una excepción.

Cuando el prestador ve la dirección por primera vez se escribe
`audit_events: order.address_revealed` `{ orderId, providerId, at }`, con cadena de hash.

## 6. Caché y rate limit

- **Sin caché, nunca.** `Cache-Control: private, no-store`. Cachear una respuesta cuyo contenido
  depende del papel y del estado es la forma más fácil de entregar una dirección a quien no debe.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure Key Vault** + **AES-256-GCM** | Descifrado de sobre de la dirección, solo si corresponde |
| **MongoDB** | Lectura con participación en el filtro; `order_events` para el historial |
| **Polly v8** | Timeout + circuit breaker al pedir el nombre de la contraparte |
| `audit_events` | Registro encadenado de la revelación de dirección |

## 8. Seguridad y privacidad

1. **La regla de la dirección se evalúa en el servidor y antes de descifrar.** No se descifra y luego
   se omite del DTO: no se descifra en absoluto.
2. **Participación en el filtro de la consulta** (`$or` de cliente y prestador): una sola consulta
   resuelve existencia y permiso, sin ventana de carrera (constitución §III.2).
3. `404` en vez de `403` para no confirmar que un pedido ajeno existe.
4. **Revelar la dirección queda auditado** con cadena de hash, junto a copiar número y borrar cuenta.
   Si alguien alega que recibió datos que no debía, hay registro verificable.
5. El nombre completo sigue la misma gradación que la dirección: se revela cuando hay compromiso.
6. Sin caché a ningún nivel, incluida la caché del cliente.
