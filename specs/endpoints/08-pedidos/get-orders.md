# Mis pedidos · `GET /v1/engagement/orders`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.4 "Mis servicios" (cliente) y 7.5 "Pedidos por responder / aceptados" (prestador) |
| Spec SDD | nv-07 |
| Idempotente | Sí (lectura) |

## 1. Función

Lista los pedidos en los que el usuario participa, filtrables por su papel y por estado. Alimenta
"Mis servicios" del cliente y los dos bloques de pedidos del inicio del prestador.

## 2. Petición

```http
GET /v1/engagement/orders?role=provider&status=pending&cursor=&limit=20
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `role` | string | enum | no | `client` \| `provider`. Por defecto, el modo del token |
| `status` | string | enum | no | `pending` \| `accepted` \| `rejected` \| `cancelled` \| `resolved` \| `active` (= pending + accepted) \| `all`. Por defecto `active` |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "items": [
    {
      "orderId": "01940208-…",
      "status": "pending",
      "statusText": "Esperando tu respuesta",
      "role": "provider",
      "counterpart": {
        "accountId": "019400a1-…",
        "displayName": "Ana L.",
        "photoUrl": null,
        "kind": "client"
      },
      "category": "plomeria",
      "descriptionExcerpt": "Fuga debajo del lavabo del baño principal.",
      "attention": "home",
      "placeZone": "Centro",
      "address": null,
      "agreedPrice": null,
      "requestId": "01940107-…",
      "conversationId": "01940309-…",
      "ago": "hace 10 minutos",
      "createdAt": "2026-11-05T18:30:00Z",
      "allowedActions": ["accept", "reject", "message"]
    }
  ],
  "nextCursor": null,
  "hasMore": false,
  "counts": { "pending": 1, "accepted": 2, "resolved": 14 }
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `counterpart.displayName` | string | **40** | **Iniciales** mientras el pedido esté `pending`; nombre completo desde `accepted` |
| `address` | string \| null | **300** | **`null` en la lista, siempre y para todos.** La dirección solo se entrega en el detalle |
| `agreedPrice` | object \| null | — | Presente desde que se aceptó con precio |
| `allowedActions` | array | — | Transiciones válidas ahora, calculadas por el servidor |
| `counts` | object | — | Totales por estado para los contadores de las pestañas |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `role` o `status` fuera del enum |
| 403 | `provider.profile_required` | `role=provider` sin perfil de prestador |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `find({ clientId: sub })` o `find({ providerId: <perfil del sub> })` + `status`, orden `_id: -1`, **proyección sin `addressEnc` ni `location`** | **`{clientId, status, createdAt: -1}`** y **`{providerId, status, createdAt: -1}`** |
| `conversations` | `find({ _id: { $in: … } })` para `conversationId` | único `{clientId, providerId}` |
| `accounts` / `provider_profiles` | Nombre y foto de la contraparte por contrato, en **una** llamada con `$in` | — |

Los dos índices compuestos cubren exactamente las dos consultas: no hay *scan* ni ordenamiento en
memoria.

`addressEnc` **no se proyecta**: ni se lee de la base en esta consulta, así que no hay descifrado ni
riesgo de que se escape en un log.

`counts` se resuelve con un `$facet` en el mismo *pipeline*, para no hacer cinco `countDocuments`.

## 6. Caché y rate limit

- **Sin caché**: pedidos y estados cambian en el momento. `Cache-Control: private, no-store`.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Índices compuestos por papel y estado; `$facet` para los conteos |
| **Polly v8** | Timeout + circuit breaker al pedir nombres a Identity y Discovery |
| **Front Door CDN** | Avatares |

## 8. Seguridad y privacidad

1. El filtro es siempre `clientId: sub` o `providerId` del perfil del `sub`: **no se listan pedidos
   ajenos**, y no hay parámetro para pedir los de otra persona.
2. **La dirección nunca aparece en una lista.** Se entrega solo en el detalle, con la comprobación de
   estado y de participante. Una lista es el lugar más fácil de filtrar por accidente.
3. La contraparte aparece con **iniciales mientras el pedido está pendiente**: el nombre completo se
   revela cuando ya hay compromiso mutuo, igual que la dirección.
4. `allowedActions` lo calcula el servidor a partir del estado y del papel: la app no infiere
   permisos.
5. `readPreference` primario: un estado de pedido desactualizado llevaría a intentar una transición
   inválida.
