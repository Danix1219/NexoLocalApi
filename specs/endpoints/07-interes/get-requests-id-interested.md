# Pantalla Interesados · `GET /v1/engagement/requests/{id}/interested`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C** (autenticado, **dueño** de la solicitud) |
| Pantalla de la maqueta | Pantalla "Interesados" (`S.stack = ints`) · desde `myReqFoot` → "Ver" |
| Spec SDD | nv-06 |
| Idempotente | Sí (lectura) |

## 1. Función

La lista completa de prestadores interesados en una solicitud, **ordenable**, con las dos acciones
que ofrece la maqueta por cada uno: **Mensaje** y **Pedir servicio**.

Es la pantalla donde el cliente compara y elige. El orden importa, por eso se permite reordenar por
calificación, recomendaciones o quién está abierto ahora.

## 2. Petición

```http
GET /v1/engagement/requests/01940107-…/interested?sort=rate&cursor=&limit=20
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | **Debe ser una solicitud del `sub`** |
| `sort` | string | enum | no | `rate` (calificación) \| `recs` (recomendaciones) \| `open` (abiertos primero) \| `recent`. Por defecto `recent` |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "requestId": "01940107-…",
  "requestTitle": "Fuga en el baño",
  "requestStatus": "open",
  "items": [
    {
      "providerId": "019400d4-…",
      "name": "Plomería del Centro",
      "kind": "business",
      "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
      "categories": ["plomeria"],
      "zoneLabel": "Centro",
      "attentionText": "Va a domicilio",
      "distanceText": "a ~2 km",
      "status": { "open": false, "text": "Cerrado · abre mañana a las 8:00" },
      "rating": { "avg": 4.6, "count": 12 },
      "recs": 34,
      "minPrice": { "fromCents": 35000, "text": "Desde $350" },
      "visit": { "mode": "free", "text": "Visita sin costo" },
      "badges": ["emailVerified"],
      "note": "Puedo pasar hoy por la tarde",
      "interestedAt": "2026-11-05T18:05:00Z",
      "conversationId": null,
      "orderStatus": null,
      "actions": { "canMessage": true, "canOrder": true, "canCompare": true }
    }
  ],
  "nextCursor": null,
  "hasMore": false,
  "total": 4
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `note` | string | **200** | La nota que dejó el prestador, ya moderada |
| `conversationId` | uuid \| null | — | Si ya hay chat abierto, para ir directo |
| `orderStatus` | string \| null | enum | Si ya hay pedido con él: `pending` \| `accepted` \| … |
| `actions.canOrder` | bool | — | `false` cuando ya hay un pedido activo con ese prestador o la solicitud está asignada |

**No se devuelve nunca**: teléfono de los prestadores, su punto exacto si son a domicilio, ni su
correo. Para el número está la acción auditada "Copiar número".

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `request.not_found` | No existe **o no es tuya**. No se distingue: si no, cualquiera podría sondear solicitudes ajenas |
| 400 | `validation_failed` | `sort` fuera del enum |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `interests` | `find({ requestId: id, clientId: sub, withdrawnAt: { $exists: false } })` | **`{clientId, requestId}`** |
| `orders` | `find({ requestId: id, clientId: sub })` para `orderStatus` | `{clientId, status, createdAt: -1}` |
| `conversations` | `find({ clientId: sub, providerId: { $in: ids } })` | único `{clientId, providerId}` |
| `provider_profiles` (Discovery) | Datos públicos por contrato: `IProviderSummaryContract.GetManyAsync(ids)` | — |

**La propiedad se verifica en el filtro**: `clientId: sub` está en la consulta de `interests`, que ya
guarda el `clientId` desnormalizado precisamente para no tener que ir a Discovery a comprobar de quién
es la solicitud.

Los datos de los prestadores llegan de Discovery por **contrato de solo lectura**, en **una sola
llamada** con `$in` de los ids (nunca una por prestador), con timeout 2 s y circuit breaker.
Si Discovery no responde → `503`: sin nombre ni calificación la pantalla no sirve para elegir.

El orden se aplica **después** de recibir los datos de Discovery, porque `rating`, `recs` y `open`
viven allá. Para que el cursor sea estable, el desempate final es `interests._id`.

## 6. Caché y rate limit

- **Sin caché**: es la pantalla de decisión y tiene que estar al día. `Cache-Control: private, no-store`.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `interests` por `{clientId, requestId}` |
| **Resiliencia en Host (.NET)** | Timeout de 2 s, circuit breaker de 30 s tras tres fallos y bulkhead de 64 operaciones |
| **Front Door CDN** | Avatares |
| Patrón **Strategy** | Un ordenador por criterio, igual que en la búsqueda |

## 8. Seguridad y privacidad

1. **Solo el dueño de la solicitud ve a los interesados.** `clientId: sub` va en el filtro, y el
   `404` no distingue entre inexistente y ajena.
2. De cada prestador se devuelve **lo que ya es público** en su perfil; esta pantalla no es una vía
   para obtener su teléfono.
3. Una sola llamada a Discovery con `$in`: evita el patrón N+1 y limita la superficie entre módulos.
4. `actions.canOrder` lo decide el servidor: así la app no puede ofrecer "Pedir servicio" cuando ya
   hay un pedido activo, lo que acabaría en `409 order.active_exists`.
5. El prestador que retiró su interés **no aparece**, aunque el documento siga existiendo.
