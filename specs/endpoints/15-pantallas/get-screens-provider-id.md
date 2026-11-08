# Pantalla de perfil · `GET /v1/screens/provider/{id}`

| Campo | Valor |
|---|---|
| Módulo | Screens |
| Quién | **P/C/Pr** |
| Pantalla de la maqueta | 7.8 Perfil de prestador, con sus pestañas · `V.profile`, `S.ptab` |
| Spec SDD | nv-14 |
| Idempotente | Sí (lectura) |

## 1. Función

Compone en una petición el perfil, la primera página de trabajos, la primera página de opiniones y el
estado personal del visitante respecto a ese prestador (si lo guardó, si tiene pedido activo, si hay
chat abierto).

Sin esto, abrir un perfil serían cuatro peticiones, y la pantalla aparecería por partes.

## 2. Petición

```http
GET /v1/screens/provider/019400d4-9e5a-7b23-8c91-4f6a7b8c9d01?lat=20.0539&lng=-99.3403
Authorization: Bearer <access_token>      # opcional
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | `providerId` |
| `lat` / `lng` | double | −90..90 / −180..180 | no | Para distancia y `attentionText` |

## 3. Respuesta

`200 OK`

```json
{
  "partial": false,
  "provider": { "providerId": "019400d4-…", "name": "Plomería del Centro", "…": "igual que GET /providers/{id}" },
  "works": { "items": [ … ], "nextCursor": "…", "hasMore": true, "total": 18 },
  "reviews": {
    "summary": { "avg": 4.7, "count": 13, "recommendCount": 11, "breakdown": { "5": 9, "4": 3, "3": 1, "2": 0, "1": 0 } },
    "items": [ … ],
    "hasMore": false
  },
  "me": {
    "saved": true,
    "activeOrder": { "orderId": "01940208-…", "status": "accepted", "statusText": "Aceptado" },
    "conversationId": "01940309-…",
    "canOrder": false,
    "canMessage": true,
    "canCopyPhone": true,
    "canCompare": true,
    "myReviewId": null
  },
  "guest": false
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `provider` | object | El DTO público completo, con `trust` y los tres niveles separados |
| `me` | object \| null | **Solo con token.** `null` para el visitante |
| `me.canOrder` | bool | `false` si ya hay pedido activo con él |
| `me.myReviewId` | uuid \| null | Si ya lo calificó, para poder ver su propia opinión |
| `guest` | bool | `true` sin token: la app muestra el gate al interactuar |

**No se devuelve nunca**: teléfono, punto exacto si es a domicilio (solo `shopLocation` con local),
ni el correo. Las mismas exclusiones que
[`GET /providers/{id}`](../03-perfil-prestador/get-providers-id.md).

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `provider.not_found` | No existe, está despublicado, o su cuenta está en baja pendiente |
| 400 | `validation_failed` | `lat`/`lng` fuera de rango |
| 429 | `rate.limit_exceeded` | `public` sin token, `user` con token |

Si el perfil falla, se responde `404`: sin `provider` la pantalla no existe. Si fallan `works`,
`reviews` o `me`, llegan como `null` con `partial: true`.

## 5. Datos

**Sin colecciones propias.** Composición en paralelo:

| Sección | Contrato | Módulo |
|---|---|---|
| `provider` | `IProviderPublicContract.GetAsync(id, point)` | Discovery |
| `works` | `IWorksContract.GetByProviderAsync(id, limit: 12)` | Discovery |
| `reviews` | `IReviewsContract.GetByProviderAsync(id, limit: 10)` | Engagement |
| `me.saved` | `ISavedContract.IsSavedAsync(sub, "provider", id)` | Discovery |
| `me.activeOrder`, `me.conversationId`, `me.myReviewId` | `IRelationContract.GetWithProviderAsync(sub, id)` | Engagement |

`provider` es **obligatoria**; las demás son opcionales y degradan a `null`.

Cada llamada con timeout 2 s, circuit breaker y bulkhead. El bloque `me` se omite por completo si no
hay token: ni se llama.

También dispara el incremento de visitas (`INCR nexo:views:{providerId}` en Redis), una sola vez por
composición, no una por sección.

## 6. Caché y rate limit

- Las partes **públicas** (`provider`, `works`, `reviews`) vienen de la caché L2 de sus endpoints:
  `prov:{id}`, `works:{id}:*`, `rev:{id}:*`.
- El bloque `me` **nunca se cachea**: se resuelve por petición.
- Para el **visitante** se cachea la composición completa: `L2 60 s`,
  clave `screen:provider:{id}:{geohash6}:guest`.
- `ETag` sobre la parte pública. `Cache-Control: private, max-age=60` con token;
  `public, max-age=60` sin él.
- Invalida con el tag `provider:{id}`.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| `Task.WhenAll` | Composición en paralelo |
| **Polly v8** | Timeout, circuit breaker y bulkhead por sección |
| **Redis** | Caché de las partes públicas y contador de visitas |
| **Front Door CDN** | Foto, portada, portafolio e imágenes de trabajos |
| Patrón **Facade** | Screens no tiene lógica de negocio |

## 8. Seguridad y privacidad

1. **Separar `me` de lo público es también lo que permite cachear.** El bloque personal queda fuera de
   la caché compartida, así que una entrada de Redis nunca contiene lo que alguien guardó o pidió.
2. Screens no consulta bases de datos: hereda las reglas de privacidad de cada contrato, incluidas
   las exclusiones de teléfono y ubicación exacta.
3. `me` se resuelve con el `sub`, nunca con un id de parámetro.
4. Las capacidades (`canOrder`, `canMessage`, `canCopyPhone`) **las decide el servidor**: la app no
   infiere permisos, y así no ofrece botones que acabarían en `403`.
5. El contador de visitas es agregado y no registra quién visitó.
6. `404` también para perfiles de cuentas en baja pendiente: se respeta la retirada del dueño.
