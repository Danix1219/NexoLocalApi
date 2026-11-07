# Ubicación y radio de búsqueda · `PUT /v1/discovery/me/location`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Selector de ubicación (`pickerOpen`) · `ACT.mapok`, `ACT.mapgps`, `setLoc` |
| Spec SDD | nv-05 |
| Idempotente | Sí (`PUT`: el mismo cuerpo deja el mismo estado) |

## 1. Función

Guarda **desde dónde busca** el usuario y con qué radio. Es la pieza del principio "ubicación
primero": resultados, feed, mapa y distancias se calculan desde este punto.

Reglas de la maqueta:

- El cliente y el visitante eligen radio: **"Buscar a menos de 2 / 5 / 10 / 20 km"** (`S.srad`,
  **10 km por defecto**).
- Al confirmar el selector, **Buscar respeta ese radio** (`results()`).
- La etiqueta de zona la calcula `areaLabel`: zona más cercana, o "Cerca de X" si está a más de 1.2 km.
- El visitante sin cuenta también elige zona, pero **no se guarda en el servidor**: vive en el
  dispositivo y viaja como `lat`/`lng` en cada consulta.

## 2. Petición

```http
PUT /v1/discovery/me/location
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "lat": 20.0539, "lng": -99.3403, "radiusKm": 10, "source": "map" }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `lat` | double | **−90 a 90** | sí | Debe caer dentro del *viewbox* de México; fuera → `400` |
| `lng` | double | **−180 a 180** | sí | |
| `radiusKm` | int32 | **{2,5,10,20}** | no | Por defecto **10**. Aquí **no** se acepta `0`: ese valor es del radio de atención de un prestador, no del de búsqueda |
| `source` | string | enum | no | `map` (marcó en el mapa) \| `gps` (`navigator.geolocation`) \| `search` (buscó una dirección). Solo para métrica |

## 3. Respuesta

`200 OK`

```json
{
  "searchLocation": { "lat": 20.0539, "lng": -99.3403 },
  "zoneLabel": "Centro",
  "radiusKm": 10,
  "nearbyProviders": 23,
  "summary": "23 prestadores a menos de 10 km que atienden este punto"
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `zoneLabel` | string | **60** | Calculado con `areaLabel`, no enviado por el cliente |
| `nearbyProviders` | int32 | — | Cuántos prestadores atienden ese punto dentro del radio |
| `summary` | string | **120** | El texto que la maqueta muestra bajo el mapa |

Se devuelve el conteo para que la app no tenga que hacer una segunda llamada solo para pintar el
resumen del selector.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `location.out_of_bounds` | Fuera del *viewbox* de México |
| 400 | `provider.radius_invalid` | `radiusKm` fuera de {2,5,10,20} |
| 400 | `validation_failed` | Coordenadas ausentes o fuera de rango |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `user_profiles` | `updateOne({ _id: sub }, { $set: { searchLocation, searchZoneLabel, searchRadiusKm, updatedAt } }, { upsert: true })` | `_id` = accountId |
| `provider_profiles` | `$geoNear` + `$match` de `serves()` + `$count` para `nearbyProviders` | `{location: "2dsphere", categories: 1}` |

El `upsert` cubre el caso de una cuenta cuyo `user_profiles` aún no llegó por el evento
`account.created`.

La consulta del conteo es la misma de `serves()`:

```js
db.provider_profiles.aggregate([
  { $geoNear: { near: { type: "Point", coordinates: [lng, lat] }, distanceField: "distM",
                maxDistance: 20000, spherical: true, query: { published: true } } },
  { $match: { $expr: { $or: [
      { $lte: ["$distM", { $multiply: ["$radiusKm", 1000] }] },
      { $and: [ { $in: ["$attention", ["shop", "both"]] }, { $lte: ["$distM", 10000] } ] } ] } } },
  { $match: { distM: { $lte: radiusKm * 1000 } } },
  { $count: "n" }
])
```

**`searchLocation` es un dato privado**: no se difunde en ninguna respuesta pública ni se usa para
localizar al usuario frente a otros. No es su domicilio, es desde dónde busca.

## 6. Caché y rate limit

- Invalida `me:{accountId}` y las claves de feed y búsqueda del usuario.
- El conteo se cachea en **L2 60 s** con clave `map:count:{geohash6}:{radiusKm}`: dos usuarios en la
  misma celda comparten resultado.
- Rate limit **`user`** (120/min). El selector de mapa dispara una llamada por confirmación, no por
  arrastre.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** `2dsphere` + `$geoNear` | Conteo de prestadores que atienden el punto |
| **Redis** | Caché del conteo por celda geohash |
| **Nominatim** (app) | La geocodificación inversa para el nombre de la calle la hace la app; el servidor solo calcula la **zona** con su propio catálogo |
| **geolocator** (app) | Fuente `gps` |

La etiqueta de zona la resuelve el servidor contra sus 4 zonas conocidas (Centro, Jardines,
San Marcos, Las Flores). No se depende de Nominatim para un dato que se guarda.

## 8. Seguridad y privacidad

1. `searchLocation` **no es el domicilio** y no se publica: se usa solo para ordenar y filtrar lo que
   ese usuario ve.
2. El punto se guarda con la precisión que manda el cliente porque las distancias lo necesitan, pero
   **nunca sale** en un DTO público.
3. `zoneLabel` lo calcula el servidor: si lo mandara el cliente, podría falsear su zona para aparecer
   en listados ajenos.
4. El límite del *viewbox* de México evita guardar coordenadas absurdas que degradarían las consultas
   geoespaciales.
5. La dirección exacta del servicio es **otra cosa**: se pide al crear el pedido y se cifra
   (ver [`../08-pedidos/post-orders.md`](../08-pedidos/post-orders.md)).
