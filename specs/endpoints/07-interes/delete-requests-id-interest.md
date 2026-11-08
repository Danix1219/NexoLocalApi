# Retirar interés · `DELETE /v1/engagement/requests/{id}/interest`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **Pr** (autenticado, quien lo expresó) |
| Pantalla de la maqueta | Mismo botón "Me interesa" en estado activo · `ACT.reqint` (alterna) |
| Spec SDD | nv-06 |
| Idempotente | Sí |

## 1. Función

El prestador retira su interés y sale de la lista de Interesados del cliente. La maqueta usa el mismo
botón para poner y quitar; aquí son dos métodos sobre el mismo recurso.

## 2. Petición

```http
DELETE /v1/engagement/requests/01940107-bc23-7d45-8e67-9f01a2b3c4d5/interest
Authorization: Bearer <access_token>
```

Sin cuerpo.

| Parámetro | Tipo | Requerido | Regla |
|---|---|---|---|
| `id` (ruta) | uuid v7 | sí | |

## 3. Respuesta

`200 OK`

```json
{ "requestId": "01940107-…", "interested": false, "interestCount": 3 }
```

Se devuelve el contador actualizado para repintar la tarjeta sin otra llamada.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `request.not_found` | La solicitud no existe |
| 409 | `interest.cannot_withdraw_assigned` | **Ya fue elegido** para esta solicitud y hay un pedido activo: para salir tiene que rechazar o cancelar el pedido, no retirar el interés |
| 401 | `auth.token_expired` | |

**No hay error** si no había interés: se responde `200` con `interested: false`.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `interests` | `updateOne({ requestId, providerId, withdrawnAt: { $exists: false } }, { $set: { withdrawnAt: now } })` | **único `{requestId, providerId}`** |
| `orders` | Comprobación de pedido activo: `countDocuments({ requestId, providerId, status: { $in: ["pending", "accepted"] } })` | `{providerId, status, createdAt: -1}` |

**Se marca `withdrawnAt`, no se borra el documento.** Así se conserva que hubo interés (sirve para la
estadística y para detectar patrones de abuso: interesarse y retirarse en masa) y se puede volver a
expresar reusando la fila.

La lista de Interesados filtra por `withdrawnAt: { $exists: false }`.

Escrituras derivadas:

- `outbox`: **`interest.withdrawn`** `{ requestId, providerId, clientId }` → el worker decrementa
  `service_requests.interestCount` en Discovery y ajusta la estadística del prestador.
- **No se notifica al cliente**: enterarse de que alguien se desinteresó no habilita ninguna acción y
  solo desanima.

## 6. Caché y rate limit

- Invalida la lista de interesados de la solicitud y las oportunidades del prestador.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Marcado con `withdrawnAt`; comprobación de pedido activo |
| **Redis** | Invalidación por tags |
| Outbox → Storage Queue | Decremento del contador en Discovery |

## 8. Seguridad y privacidad

1. El filtro lleva `providerId` del `sub`: **solo se retira el interés propio**.
2. `interest.cannot_withdraw_assigned` protege al cliente: quien ya fue elegido no desaparece de la
   pantalla sin dejar rastro; tiene que rechazar el pedido explícitamente, lo que sí se notifica.
3. El borrado lógico conserva el historial para la estadística y para detectar abuso, sin dejar el
   interés visible.
4. Sin notificación al cliente, por la misma razón que no se avisa de una recomendación retirada.
