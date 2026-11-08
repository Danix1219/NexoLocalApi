# Feed de trabajos · `GET /v1/discovery/feed`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **P/C/Pr** (público) |
| Pantalla de la maqueta | 7.4 Inicio "Para ti" / 7.5 Comunidad · `V.home`, `postNear` |
| Spec SDD | nv-04 |
| Idempotente | Sí (lectura) |

## 1. Función

El feed "Trabajos en {zona}": publicaciones de prestadores cercanos, con **los intereses del usuario
primero**. Es la pantalla de entrada del cliente y la pestaña Comunidad del prestador.

Regla de la maqueta (`postNear`): un trabajo es relevante si **su autor atiende tu punto** o si el
trabajo está **a ≤ 3 km** de ti. Las dos condiciones, no solo la distancia: un prestador que va a
domicilio en tu zona es relevante aunque su trabajo se hiciera lejos.

El formato de la tarjeta es el **feed estilo Instagram/Facebook** (`.post.ig`): foto cuadrada, franja
"Contactar", fila de iconos (recomendar, compartir, comparar, guardar), conteo de recomendaciones y
texto recortado a 3 líneas.

## 2. Petición

```http
GET /v1/discovery/feed?lat=20.0539&lng=-99.3403&cursor=&limit=10
Authorization: Bearer <access_token>      # opcional
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `lat` / `lng` | double | −90..90 / −180..180 | sí | Punto del usuario. Sin ellos no hay feed: todo se calcula desde ahí |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto **10**: son tarjetas con foto |

Sin token, el visitante recibe **feed limitado**: las mismas tarjetas pero sin los campos de estado
personal (`saved`, `recommended`), y la app muestra el gate de cuenta al interactuar.

## 3. Respuesta

`200 OK`

```json
{
  "zoneLabel": "Centro",
  "items": [
    {
      "workId": "019400f6-ab12-7c34-9d56-7e8f90a1b2c3",
      "provider": {
        "providerId": "019400d4-…",
        "name": "Plomería del Centro",
        "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
        "kind": "business",
        "badges": ["phoneVerified"]
      },
      "category": "plomeria",
      "title": "Cambio de tubería en cocina",
      "textExcerpt": "Cambiamos la bajada completa y dejamos probada la instalación…",
      "images": [{ "url": "…", "thumbUrl": "…", "w": 1200, "h": 1200 }],
      "zoneLabel": "Centro",
      "distanceText": "a ~2 km",
      "recs": 12,
      "recsText": "12 recomendaciones",
      "tag": "Urgencia resuelta",
      "ago": "hace 3 días",
      "saved": false,
      "recommended": false,
      "canContact": true
    }
  ],
  "nextCursor": "MDE5NDAwZjYt…",
  "hasMore": true,
  "guest": false
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `zoneLabel` | string | **60** | Para el título "Trabajos en {zona}" |
| `textExcerpt` | string | **300** | Recorte; la UI lo muestra en 3 líneas |
| `images[]` | array | **6** | URLs de CDN, con miniatura |
| `recsText` | string | **40** | "N recomendaciones" |
| `ago` | string | **30** | Tiempo relativo en español |
| `saved` / `recommended` | bool | — | **Solo con token.** Ausentes para el visitante |
| `canContact` | bool | — | `false` para el visitante: la app muestra el gate |
| `guest` | bool | — | `true` si no hubo token |

**No se devuelve nunca**: teléfono del prestador, punto exacto, ni datos del cliente para quien se
hizo el trabajo.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Faltan `lat`/`lng` o están fuera de rango |
| 400 | `request.limit_exceeded` | `limit` mayor que 50 |
| 400 | `request.cursor_invalid` | |
| 429 | `rate.limit_exceeded` | Política `public` |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `works` | `$geoNear` sobre `publicPoint` con `maxDistance` del radio efectivo | `{publicPoint: "2dsphere", createdAt: -1}` |
| `provider_profiles` | `$lookup` por `providerId` para el bloque `provider` | `_id` |
| `saved`, `recommendations` | `find({ accountId: sub, targetId: { $in: workIds } })` — **una sola consulta** para marcar las tarjetas | únicos `{accountId, kind, targetId}` y `{accountId, workId}` |

Composición del feed:

1. `$geoNear` sobre `works.publicPoint` dentro de 3 km, **unido** a los trabajos de prestadores que
   atienden el punto (`serves`), resuelto con dos consultas y mezcla en memoria: `$geoNear` no admite
   el `$expr` de `serves()` en la misma etapa.
2. Prioridad: los de **categorías de interés** del usuario primero (`user_profiles.interests[]`),
   luego por `createdAt` descendente.
3. El desempate final es `_id`, para que el cursor sea estable.

`readPreference: secondaryPreferred`. Un feed 30 s desactualizado no molesta a nadie.

El `$lookup` del prestador se evita cuando el bloque ya está en la caché L2 por `provider:{id}`.

## 6. Caché y rate limit

- **L2 (Redis) 60 s** para la **parte pública** del feed, clave `feed:{geohash6}:{cursor}:{limit}`.
- Los campos `saved` y `recommended` **no se cachean**: se resuelven por petición con la consulta de
  marcado. Así una sola entrada de caché sirve a todos los usuarios de la celda.
- Invalida con los tags `feed:{geohash6}` (trabajo nuevo o borrado) y `provider:{id}`.
- Rate limit **`public`** (60/min por IP).
- `Cache-Control: private, max-age=30` cuando hay token (por los campos personales),
  `public, max-age=30` para el visitante.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** `2dsphere` + `$geoNear` | Trabajos cercanos |
| **Redis** | Caché L2 de la parte pública por celda |
| **HybridCache** | Protección contra *stampede* |
| **Front Door CDN** | Fotos y miniaturas de los trabajos |
| Geohash | Clave de caché por celda |

## 8. Seguridad y privacidad

1. El feed se arma desde `publicPoint` (difuminado), nunca desde el punto exacto del trabajo.
2. **Separar lo cacheado de lo personal** es también una medida de privacidad: la entrada compartida
   de Redis no contiene qué guardó o recomendó nadie.
3. El visitante recibe un feed **sin campos personales** y con `canContact: false`; las acciones que
   exigen cuenta las bloquea el servidor, no solo la UI (gate de `rec`, `savew`, `savep`).
4. No se expone quién fue el cliente de un trabajo publicado: el trabajo es del prestador.
5. Las categorías de interés se usan para ordenar, nunca se devuelven en la respuesta pública.
