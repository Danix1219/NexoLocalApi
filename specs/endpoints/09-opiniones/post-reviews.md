# Calificar un servicio · `POST /v1/engagement/reviews`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C** (cliente de un pedido **resuelto**) |
| Pantalla de la maqueta | 7.10 Publicar → "Mi experiencia" · `ACT.exsave`, `EXPS` |
| Spec SDD | nv-09 |
| Idempotente | Sí — `Idempotency-Key` obligatorio |

## 1. Función

El cliente publica su experiencia: **1 a 5 estrellas**, texto opcional y si recomienda. Es el tercer
nivel de confianza del producto: **opinión de usuarios**, siempre separada de lo declarado por el
prestador y de lo verificado por Nexo Local.

**Una opinión por pedido.** Exigir un pedido resuelto es lo que evita las reseñas de nadie: no se
puede calificar a un prestador con el que nunca se tuvo un servicio.

## 2. Petición

```http
POST /v1/engagement/reviews
Authorization: Bearer <access_token>
Idempotency-Key: 2e7b4c9a-5d16-4f08-b3e2-9c1d0a8f7b65
Content-Type: application/json
```

```json
{
  "orderId": "01940208-cd34-7e56-9f78-0a1b2c3d4e5f",
  "stars": 5,
  "text": "Llegó puntual, dejó todo limpio y explicó qué había pasado.",
  "recommend": true
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `orderId` | uuid v7 | — | sí | Pedido **`resolved`** del que el `sub` es cliente |
| `stars` | int32 | **1–5** | sí | Enteros; no hay medias estrellas |
| `text` | string | **500** | no | Mínimo 10 caracteres si se envía. **Moderado** |
| `recommend` | bool | — | no | Equivale al pulgar de la maqueta |

## 3. Respuesta

`201 Created` · `Location: /v1/engagement/reviews/{id}`

```json
{
  "reviewId": "0194030a-de45-7f67-8091-a2b3c4d5e6f7",
  "orderId": "01940208-…",
  "providerId": "019400d4-…",
  "stars": 5,
  "text": "Llegó puntual, dejó todo limpio y explicó qué había pasado.",
  "recommend": true,
  "author": { "displayName": "Ana L.", "photoUrl": null },
  "createdAt": "2026-11-05T22:00:00Z",
  "moderation": { "status": "approved", "checkedAt": "2026-11-05T21:59:58Z" },
  "providerRating": { "avg": 4.7, "count": 13 }
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `author.displayName` | string | **40** | **Nombre de pila e inicial**, no el nombre completo: la opinión es pública y perpetua |
| `providerRating` | object | — | Promedio ya recalculado, para repintar sin otra llamada |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `review.already_exists` | Ya calificó ese pedido (**índice único disperso** en `orderId`) |
| 403 | `review.order_not_resolved` | El pedido no está `resolved` |
| 404 | `resource.not_found` | El pedido no existe o **no eres su cliente** |
| 400 | `review.stars_invalid` | Fuera de 1–5 o no entero |
| 400 | `validation_failed` | `text` de menos de 10 o más de 500 caracteres |
| 403 | `content.inappropriate` | Content Safety rechazó el texto |
| 429 | `rate.limit_exceeded` | Política `publish`: **20 opiniones al día** |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `orders` | `findOne({ _id: orderId, clientId: sub, status: "resolved" }, { providerId: 1 })` — **id, propiedad y estado en el mismo filtro** | `{clientId, status, createdAt: -1}` |
| `reviews` | `insertOne` | **único disperso `orderId`**; `{providerId, createdAt: -1}` |

Documento:

```js
{
  _id: UUID_v7, orderId: UUID, providerId: UUID, clientId: UUID,
  stars: 5, text: "…", recommend: true,
  moderation: { status: "approved", checkedAt: ISODate },
  createdAt: ISODate
}
```

Escrituras derivadas:

- `outbox`: **`review.created`** `{ reviewId, providerId, stars, recommend }` → el worker:
  - recalcula `provider_profiles.rating` (`avg`, `count`) en Discovery con un `$group` sobre las
    opiniones aprobadas de ese prestador, **no con un promedio incremental** (un incremental acumula
    error y no soporta borrados por moderación);
  - invalida el tag `provider:{id}`;
  - notifica al prestador: "Recibiste una opinión nueva".

El `rating` en el perfil es un **modelo de lectura**: se recalcula por evento y nunca se consulta
`reviews` desde la búsqueda.

## 6. Caché y rate limit

- Invalida `provider:{id}`, `prov:{id}` y `rev:{providerId}:*`.
- Rate limit **`publish`**: 20 al día por `sub`, exacto en Redis.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure AI Content Safety** | Moderación del texto **antes** de guardar |
| **MongoDB** | Índice único disperso para una opinión por pedido |
| **FirebaseAdmin** (worker) | Push al prestador |
| Outbox → Storage Queue | Recálculo del promedio en Discovery |
| **Redis** | Idempotencia, rate limit, invalidación |

## 8. Seguridad y privacidad

1. **Sin pedido resuelto no hay opinión.** Es la defensa contra reseñas falsas, y se comprueba con
   id, propiedad y estado en una sola consulta.
2. El **índice único disperso** sobre `orderId` garantiza una opinión por servicio, incluso con dos
   envíos simultáneos.
3. El autor aparece con **nombre de pila e inicial**: una opinión queda publicada indefinidamente y
   no hace falta el nombre completo para que sea creíble.
4. La opinión es **opinión**, y la API la etiqueta como tal (`trust.userOpinion` en el perfil): no se
   presenta como verificación de competencia profesional (constitución §2).
5. Moderación síncrona: el texto no llega a la base sin revisar.
6. El promedio se recalcula completo por evento, de modo que si la moderación retira una opinión, la
   calificación del prestador se corrige.
