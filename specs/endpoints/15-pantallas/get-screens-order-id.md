# Pantalla de pedido · `GET /v1/screens/order/{id}`

| Campo | Valor |
|---|---|
| Módulo | Screens |
| Quién | **parte** del pedido |
| Pantalla de la maqueta | 7.15 Pedido de servicio · `V.match` |
| Spec SDD | nv-14 |
| Idempotente | Sí (lectura) |

## 1. Función

Compone la pantalla del pedido: el pedido con su historial, el mapa (zona o dirección, según la
regla), los datos de la contraparte, el precio, el chat asociado y **qué acciones permite el servidor
ahora mismo**.

## 2. Petición

```http
GET /v1/screens/order/01940208-cd34-7e56-9f78-0a1b2c3d4e5f
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Requerido | Regla |
|---|---|---|---|
| `id` (ruta) | uuid v7 | sí | El `sub` debe ser cliente o prestador del pedido |

## 3. Respuesta

`200 OK`

```json
{
  "partial": false,
  "order": {
    "orderId": "01940208-…",
    "status": "accepted",
    "statusText": "Aceptado",
    "role": "provider",
    "category": "plomeria",
    "description": "Fuga debajo del lavabo del baño principal.",
    "attention": "home",
    "placeZone": "Centro",
    "address": "Calle Hidalgo 123, interior 4, Col. Centro",
    "addressNote": "Portón verde, tocar dos veces",
    "agreedPrice": { "amountCents": 45000, "text": "$450.00", "includes": "Material y mano de obra" },
    "version": 2
  },
  "map": {
    "kind": "exact",
    "point": { "lat": 20.0539, "lng": -99.3403 },
    "label": "Calle Hidalgo 123, Col. Centro",
    "directionsUrl": "https://www.google.com/maps/dir/?api=1&destination=20.0539,-99.3403"
  },
  "counterpart": {
    "id": "019400a1-…",
    "displayName": "Ana López",
    "photoUrl": null,
    "kind": "client",
    "canCopyPhone": true
  },
  "timeline": [
    { "version": 1, "type": "created", "actor": "client", "at": "2026-11-05T18:30:00Z", "text": "Pedido creado" },
    { "version": 2, "type": "accepted", "actor": "provider", "at": "2026-11-05T18:41:00Z", "text": "Aceptado con precio acordado" }
  ],
  "conversationId": "01940309-…",
  "unreadMessages": 0,
  "request": { "requestId": "01940107-…", "title": "Fuga en el baño" },
  "allowedActions": ["resolve", "cancel", "message", "copyPhone"],
  "review": { "canReview": false, "myReviewId": null }
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `map.kind` | string | enum | **`zone`** (círculo de la zona) \| **`exact`** (alfiler y ruta). Lo decide la regla de visibilidad |
| `map.label` | string | **200** | La dirección si `exact`; el nombre de la zona si `zone` |
| `map.directionsUrl` | string | **300** | **Solo si `kind: exact`** |
| `timeline[].text` | string | **120** | Texto ya compuesto, **sin datos sensibles** |
| `allowedActions` | array | — | Transiciones válidas para este papel y estado |

### La regla del mapa

| Estado | Cliente | Prestador |
|---|---|---|
| `pending` | `exact` | **`zone`** |
| `accepted` · `resolved` | `exact` | `exact` |
| `rejected` | `exact` | **`zone`** |
| `cancelled` | `exact` | `exact` si alcanzó a aceptar; `zone` si no |

Es la misma tabla de [`GET /orders/{id}`](../08-pedidos/get-orders-id.md), aplicada también al mapa:
no tendría sentido ocultar la dirección en texto y dibujarla como alfiler.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe **o no eres parte** |
| 401 | `auth.token_expired` | |

Si falla `order`, se responde `404`. Si fallan `counterpart`, `request` o `unreadMessages`, llegan
como `null` con `partial: true`.

## 5. Datos

**Sin colecciones propias.** Composición en paralelo:

| Sección | Contrato | Módulo |
|---|---|---|
| `order`, `timeline`, `allowedActions`, `map` | `IOrderDetailContract.GetAsync(orderId, sub)` | Engagement |
| `counterpart` | `IAccountSummaryContract` / `IProviderSummaryContract` | Identity / Discovery |
| `conversationId`, `unreadMessages` | `IConversationContract.GetByPairAsync(clientId, providerId)` | Engagement |
| `request` | `IRequestSummaryContract.GetAsync(requestId)` | Discovery |
| `review` | `IReviewsContract.GetByOrderAsync(orderId, sub)` | Engagement |

`order` es **obligatoria** y es la que resuelve la participación y el descifrado de la dirección:
Screens **no descifra nada**, recibe de Engagement lo que corresponda según el papel y el estado.

El nombre completo de la contraparte solo se pide si el pedido está `accepted` o `resolved`; si está
`pending`, se piden solo las iniciales.

## 6. Caché y rate limit

- **Sin caché, a ningún nivel.** `Cache-Control: private, no-store`. Es la pantalla que puede
  contener una dirección.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| `Task.WhenAll` | Composición en paralelo |
| **Polly v8** | Timeout, circuit breaker y bulkhead por sección |
| **Azure Key Vault** | El descifrado ocurre **en Engagement**, no aquí |
| Patrón **Facade** | Screens compone, no decide |

## 8. Seguridad y privacidad

1. **Screens nunca descifra la dirección.** La pide a Engagement, que aplica la regla de estado y
   papel y devuelve `null` si no corresponde. Concentrar la decisión en un solo lugar es lo que hace
   la regla auditable.
2. El `map.kind` se deriva de lo que llegó: si no hay dirección, el mapa es de zona. La app no puede
   dibujar un alfiler que el servidor no autorizó.
3. **Sin caché**: cachear esta pantalla sería guardar una dirección en Redis, y además su contenido
   depende de quién pregunta.
4. El `timeline` lleva **textos compuestos sin datos sensibles**: el evento de cambio de dirección
   dice "El cliente cambió la dirección", no la dirección.
5. `404` en vez de `403` para no confirmar pedidos ajenos.
6. `allowedActions` viene del servidor, así que la app no ofrece transiciones inválidas.
