# Puntos del mapa · `GET /v1/discovery/map/nearby`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **P/C/Pr** (público) |
| Pantalla de la maqueta | Selector de ubicación con puntos · `pkItems`, `pkPoints`, `ACT.pkpt` |
| Spec SDD | nv-05 |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve los puntos que se dibujan sobre el mapa del selector. **Qué se dibuja depende de quién
mira**, exactamente como en la maqueta:

| `kind` | Quién | Qué puntos | Colores | Resumen |
|---|---|---|---|---|
| `providers` | Cliente y visitante | Prestadores que **atienden ese punto** dentro del radio | verde = abierto, gris = cerrado | "N prestadores a menos de X km que atienden este punto" |
| `requests` | Prestador | Solicitudes **de su rubro** dentro del radio | ámbar = urgencia Hoy, verde = otras | "N solicitudes de tu rubro dentro de X km · Toca un punto para verla" |

El conteo y los puntos **cambian en vivo** al mover el punto o cambiar el radio, así que este
endpoint se llama a menudo: por eso la caché por celda geohash es importante.

## 2. Petición

```http
GET /v1/discovery/map/nearby?lat=20.0539&lng=-99.3403&radiusKm=5&kind=providers
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `lat` / `lng` | double | −90..90 / −180..180 | sí | Centro del mapa |
| `radiusKm` | int32 | **{2,5,10,20}** | no | Por defecto 10 |
| `kind` | string | enum | no | `providers` \| `requests`. Por defecto según el modo del token; `requests` **exige** JWT con perfil de prestador |
| `cat` | string | **20** | no | Solo con `kind=requests`: limita a una categoría. Sin ella, se usan **las categorías del perfil** del prestador |
| `limit` | int32 | **1–100** | no | Por defecto **60**. Tope alto porque son puntos, no tarjetas |

## 3. Respuesta

`200 OK`

```json
{
  "kind": "providers",
  "center": { "lat": 20.0539, "lng": -99.3403 },
  "radiusKm": 5,
  "count": 12,
  "summary": "12 prestadores a menos de 5 km que atienden este punto",
  "points": [
    {
      "id": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01",
      "point": { "lat": 20.0536, "lng": -99.3398 },
      "state": "open",
      "title": "Plomería del Centro",
      "subtitle": "Plomería · Abierto",
      "distanceKm": 1.2,
      "exact": true
    }
  ]
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `count` | int32 | — | Total que cumple, aunque `points` venga recortado por `limit` |
| `summary` | string | **120** | Texto listo para la UI |
| `points[].point` | Point | — | **El difuminado** (`publicZone.point`) salvo que sea un local |
| `points[].state` | string | enum | `providers`: `open` \| `closed`. `requests`: `today` \| `other` |
| `points[].title` | string | **80** | Nombre del prestador, o título de la solicitud |
| `points[].subtitle` | string | **80** | Ficha que la maqueta muestra al tocar: servicio · urgencia o estado |
| `points[].exact` | bool | — | `true` solo cuando el punto es el **local publicado** de un negocio |

**No se devuelve nunca**: el punto exacto de un prestador a domicilio, el teléfono, el nombre del
cliente que publicó la solicitud, ni la ubicación precisa de la solicitud (siempre `publicPoint`).

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Coordenadas o `radiusKm` fuera de rango |
| 401 | `auth.token_expired` | Con `kind=requests` |
| 403 | `provider.profile_required` | `kind=requests` sin perfil de prestador |
| 400 | `request.limit_exceeded` | `limit` mayor que 100 |
| 429 | `rate.limit_exceeded` | Política `public` |

## 5. Datos

**`kind=providers`** — `$geoNear` sobre `provider_profiles`:

```js
db.provider_profiles.aggregate([
  { $geoNear: { near: { type: "Point", coordinates: [lng, lat] }, distanceField: "distM",
                maxDistance: radiusKm * 1000, spherical: true, query: { published: true } } },
  { $match: { $expr: { $or: [
      { $lte: ["$distM", { $multiply: ["$radiusKm", 1000] }] },
      { $and: [ { $in: ["$attention", ["shop", "both"]] }, { $lte: ["$distM", 10000] } ] } ] } } },
  { $project: { location: 0, phone: 0 } },     // el punto exacto no sale del servidor
  { $limit: limit }
])
```

**`kind=requests`** — `$geoNear` sobre `service_requests` con
`query: { category: { $in: cats }, status: "open" }` y el filtro de radio del prestador.

| Colección | Índice usado |
|---|---|
| `provider_profiles` | `{location: "2dsphere", categories: 1}` |
| `service_requests` | `{location: "2dsphere", category: 1, status: 1}` |

`state` se calcula con `status(p)` de la maqueta: **día real y hora fija 9:41** (`NOWMIN`), que es lo
que hace coincidir el "Abierto ahora" con la barra de estado del prototipo.

## 6. Caché y rate limit

- **L2 (Redis) 60 s**, clave `map:{kind}:{geohash6}:{radiusKm}:{catsHash}`.
  Un geohash de 6 caracteres son celdas de ~1.2 km: suficiente para que varios usuarios de la misma
  colonia compartan la respuesta, y bastante fino para que los puntos no se desplacen de forma
  visible.
- Invalida con los tags `map:{geohash6}`, `requests:{geohash6}` y `provider:{id}`.
- Rate limit **`public`** (60/min por IP). El arrastre del mapa **no** debe disparar llamadas: la app
  consulta al soltar y con *debounce*.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** `2dsphere` + `$geoNear` | Consulta por radio con distancia calculada |
| **Redis** | Caché L2 por celda geohash |
| **HybridCache** | Protección contra *stampede* al mover el mapa |
| Geohash (`NGeoHash`) | Clave de caché estable por celda |

## 8. Seguridad y privacidad

1. **El punto exacto se excluye en la proyección** (`$project: { location: 0 }`), no en el DTO: no
   sale ni de la base. Solo los negocios con local tienen `exact: true`, y publican ese punto a
   propósito.
2. Las solicitudes se dibujan siempre con `publicPoint` difuminado: nadie puede deducir el domicilio
   de un cliente juntando puntos del mapa.
3. `kind=requests` exige perfil de prestador verificado en el servidor: un cliente no puede ver el
   mapa de solicitudes de otros con solo cambiar un parámetro.
4. Sin `cat`, las categorías se toman **del perfil del prestador**, no del parámetro: así nadie ve
   solicitudes de rubros que no le corresponden.
5. El `limit` alto es seguro porque la respuesta no lleva datos personales; aun así, el tope de 100
   evita volcados masivos del directorio.
