# Cerrar una solicitud · `POST /v1/discovery/requests/{id}/close`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C** (autenticado, dueño de la solicitud) |
| Pantalla de la maqueta | 7.4 "Mis solicitudes" → "Ya lo resolví" · `ACT.closereq` |
| Spec SDD | nv-06 |
| Idempotente | Sí |

## 1. Función

Marca la solicitud como resuelta y **la oculta a los prestadores**. Es la acción `ACT.closereq` de la
maqueta: deja de aparecer en oportunidades, en el mapa y en las búsquedas de los prestadores.

Se puede cerrar tanto si se eligió a alguien por Nexo Local como si el problema se resolvió por fuera;
el servidor no exige justificación.

## 2. Petición

```http
POST /v1/discovery/requests/01940107-bc23-7d45-8e67-9f01a2b3c4d5/close
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "outcome": "solvedHere", "pickedProviderId": "019400d4-…" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Debe ser del `sub` |
| `outcome` | string | enum | no | `solvedHere` (con un prestador de Nexo) \| `solvedElsewhere` \| `noLongerNeeded`. Por defecto `noLongerNeeded`. Solo para métrica |
| `pickedProviderId` | uuid | — | no | Solo con `solvedHere`. Si hay un pedido aceptado, se toma de ahí y este campo se ignora |

## 3. Respuesta

`200 OK`

```json
{
  "requestId": "01940107-…",
  "status": "closed",
  "closedAt": "2026-11-05T19:00:00Z",
  "interestCount": 3,
  "canReview": true,
  "reviewProviderId": "019400d4-…"
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `canReview` | bool | `true` si hay un prestador asignado al que se puede calificar |
| `reviewProviderId` | uuid \| null | A quién invitar a calificar |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `request.not_found` | No existe **o no es tuya** |
| 409 | `request.already_closed` | Ya estaba cerrada. El cuerpo trae la fecha de cierre |
| 400 | `validation_failed` | `outcome` fuera del enum |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `service_requests` | `findOneAndUpdate({ _id: id, clientId: sub, status: { $ne: "closed" } }, { $set: { status: "closed", closedAt, outcome } })` | `{clientId: 1, createdAt: -1}` |

**Id, propiedad y estado van en el mismo filtro**: si devuelve `null`, se distingue entre `404` y
`409` con una sola consulta adicional solo en ese caso.

Escrituras derivadas:

- `outbox`: **`request.closed`** `{ requestId, clientId, pickedProviderId?, interestProviderIds[] }` →
  el worker:
  - invalida `requests:{geohash6}` y `map:requests:*`;
  - en Engagement, marca los `interests` de esa solicitud como cerrados y **notifica a los
    prestadores que habían mostrado interés**: "La solicitud que te interesaba ya se resolvió";
  - si quedaba un pedido `pending` de esa solicitud, lo **cancela**.
- `audit_events`: no aplica.

## 6. Caché y rate limit

- Invalida `requests:{geohash6}`, `map:requests:*` y las oportunidades de los prestadores del rubro.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `findOneAndUpdate` con propiedad y estado en el filtro |
| **FirebaseAdmin** (worker) | Push a los prestadores interesados |
| Outbox → Storage Queue | Cierre de intereses y cancelación de pedidos pendientes en Engagement |
| **Redis** | Invalidación por tags |

## 8. Seguridad y privacidad

1. `clientId: sub` en el filtro: **solo el dueño cierra su solicitud**. Un prestador no puede
   cerrarla para quitarla de la competencia.
2. El estado en el filtro hace la operación segura ante dos peticiones simultáneas.
3. **Avisar a los prestadores interesados es parte del trato**: dedicaron atención a esa solicitud y
   merecen saber que ya no sigue abierta. El aviso no revela con quién se resolvió.
4. `outcome` es métrica interna y no se publica en ninguna respuesta ajena.
5. Cerrar no borra: la solicitud queda en el historial del cliente y sostiene la calificación
   posterior.
