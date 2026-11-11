# Publicar una solicitud · `POST /v1/discovery/requests`

> Implementación local: rige el [refinamiento del incremento](implementacion.md) para cuota, búsqueda, contratos y capacidades pendientes. Los ejemplos cloud y de pedidos representan el objetivo posterior.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C** (autenticado) |
| Pantalla de la maqueta | 7.10 Publicar → "Busco un servicio" · `ACT.rqsave`, borrador `S.rq` |
| Spec SDD | nv-09 (solicitudes; refinamiento local) |
| Idempotente | Sí — `Idempotency-Key` obligatorio |

## 1. Función

El cliente publica lo que necesita. La solicitud la ven los prestadores de ese rubro cuyo radio
alcanza la zona, que pueden expresar interés.

Reglas de la maqueta:

- Solo se publica la **zona aproximada**; el teléfono del cliente **no** se muestra nunca.
- Urgencia: **Hoy / Esta semana / Sin prisa**.
- Canal de contacto: chat en Nexo, o chat y llamada.
- La simulación `simInterest` del prototipo (prestadores que se interesan solos unos segundos
  después) **no se implementa**: aquí el interés es real (constitución §9).

## 2. Petición

```http
POST /v1/discovery/requests
Authorization: Bearer <access_token>
Idempotency-Key: 5c2e8a1f-3b74-4d09-91e6-7a8b9c0d1e2f
Content-Type: application/json
```

```json
{
  "category": "plomeria",
  "title": "Fuga en el baño",
  "description": "Hay una fuga debajo del lavabo que no para. Necesito que la revisen pronto.",
  "urgency": "today",
  "contact": "chat",
  "location": { "lat": 20.0539, "lng": -99.3403 },
  "imageUploadId": "019400b2-…"
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `category` | string | **20** | sí | Slug de las 7 categorías |
| `title` | string | **90** | sí | Mínimo 5 caracteres. **Moderado** |
| `description` | string | **10–600** | sí | **Moderada** |
| `urgency` | string | enum | sí | `today` \| `week` \| `noRush` |
| `contact` | string | enum | sí | `chat` \| `chatCall`. `chatCall` **exige correo confirmado y móvil declarado** |
| `location.lat` / `.lng` | double | −90..90 / −180..180 | sí | Punto del servicio. Se guarda **privado** y se publica difuminado |
| `imageUploadId` | uuid | — | no | Una foto del problema, ya aprobada |

## 3. Respuesta

`201 Created` · `Location: /v1/discovery/requests/{id}`

```json
{
  "requestId": "01940107-bc23-7d45-8e67-9f01a2b3c4d5",
  "category": "plomeria",
  "title": "Fuga en el baño",
  "urgency": "today",
  "urgencyText": "Hoy",
  "zoneLabel": "Centro",
  "publicPoint": { "lat": 20.0536, "lng": -99.3398 },
  "status": "open",
  "interestCount": 0,
  "reachableProviders": 14,
  "createdAt": "2026-11-05T17:30:00Z",
  "moderation": { "status": "approved", "checkedAt": "2026-11-05T17:29:58Z" }
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `publicPoint` | Point | **Difuminado**; es lo único que verán los prestadores |
| `reachableProviders` | int32 | A cuántos prestadores les aparecerá. Da expectativa realista al cliente |
| `interestCount` | int32 | 0 al crear |

**No se devuelve nunca**: el `location` exacto (ni al propio dueño en este DTO), ni el teléfono.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 403 | `content.inappropriate` | Content Safety rechazó título, descripción o imagen |
| 403 | `auth.email_not_verified` | Pidió `contact: chatCall` sin correo confirmado |
| 400 | `phone.invalid` | Pidió `contact: chatCall` sin móvil declarado válido |
| 400 | `validation_failed` | Longitudes o enums inválidos |
| 404 | `upload.not_found` · 409 `upload.not_pending` | Imagen inválida o ajena |
| 429 | `rate.limit_exceeded` | Política `publish`: **10 solicitudes al día** |
| 403 | `account.pending_deletion` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `service_requests` | `insertOne` | `{location: "2dsphere", category: 1, status: 1}`; `{clientId: 1, createdAt: -1}`; `{status: 1, createdAt: -1}` |
| `uploads` | `findOne({ _id, ownerId: sub, status: "approved" })` | `{ownerId, createdAt}` |
| `provider_profiles` | `$geoNear` + `serves()` + `$count` para `reachableProviders` | `{location: "2dsphere", categories: 1}` |

Documento:

```js
{
  _id: UUID_v7, clientId: UUID, category: "plomeria",
  title: "…", description: "…",
  location: { type: "Point", coordinates: [lng, lat] },     // PRIVADO
  publicPoint: { type: "Point", coordinates: [lng', lat'] }, // difuminado 500–800 m
  zoneLabel: "Centro", urgency: "today", contact: "chat",
  status: "open", interestCount: 0,
  imageUrl: "https://cdn…", moderation: { status: "approved", checkedAt: ISODate },
  createdAt: ISODate
}
```

A diferencia de `works`, aquí **sí** se guarda `location` exacto: lo necesita el filtro de radio de
las oportunidades del prestador. Pero la proyección de salida lo excluye **siempre**, para todos.

Escrituras derivadas:

- `outbox`: **`request.created`** `{ requestId, clientId, category, publicPoint, urgency }` →
  el worker invalida `requests:{geohash6}`, y notifica por push a los prestadores del rubro cuyo
  radio alcanza el punto ("Hay una solicitud nueva de plomería en Centro").
- Validator: `title` `maxLength 90`, `description` `minLength 10` / `maxLength 600`, `urgency` y
  `status` en sus enums.

## 6. Caché y rate limit

- Invalida `requests:{geohash6}` y `map:requests:*` de la celda.
- Rate limit **`publish`**: 10 solicitudes al día por `sub`, exacto en Redis.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure AI Content Safety** | Moderación de título, descripción e imagen antes de insertar |
| **MongoDB** `2dsphere` | Índice para el filtro de radio de los prestadores |
| **Front Door CDN** | La foto del problema |
| **FirebaseAdmin** (worker) | Push a los prestadores alcanzables |
| **Redis** | Idempotencia, rate limit exacto, invalidación |

## 8. Seguridad y privacidad

1. **El punto exacto no sale nunca en una respuesta**, ni para el dueño: la dirección precisa se
   entrega al prestador solo tras aceptar el pedido, y por otro camino
   (ver [`../08-pedidos/get-orders-id.md`](../08-pedidos/get-orders-id.md)).
2. El **teléfono del cliente no se publica** con la solicitud; `contact: chatCall` solo indica que
   acepta llamadas, y el número se obtiene con la acción auditada "Copiar número".
3. Moderación **síncrona** antes de persistir.
4. El límite de 10 al día frena el ruido en el mapa de oportunidades de los prestadores.
5. `reachableProviders` es un agregado: no revela **quiénes** son.
6. El push a los prestadores lleva categoría y zona, nunca la dirección ni el nombre del cliente
   (constitución §7).

## Refinamiento de implementación local (2026-11-10)
Aplican las [decisiones, límites y evidencia del incremento](implementacion.md), incluida cuota transaccional Mongo, búsqueda normalizada sin Atlas Search, resumen por contratos y bloqueo de imágenes/reseñas sin infraestructura o pedido autorizado. Esta nota concilia los ejemplos históricos; no acredita despliegue.
