# Buscar prestadores · `GET /v1/discovery/search/providers`

> Refinamiento ejecutable 2026-11-10: [contrato y l?mites del incremento](implementacion-directorio.md) prevalece sobre los ejemplos hist?ricos de índices/caché/hora fija y especialidades de esta ficha. Autoridad de arquitectura: nv-06.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **P/C** (público) |
| Pantalla de la maqueta | 7.6 Buscar — solicitante · `results()`, `matchQ`, `FILTERS`, `SORTN` |
| Spec SDD | nv-04 · nv-05 |
| Idempotente | Sí (lectura) |

## 1. Función

La búsqueda principal del cliente. Reproduce `results()` de la maqueta en cuatro pasos:

1. **Base**: prestadores de la categoría `cat` que pasan `matchQ` — todas las palabras de `q`,
   normalizadas sin acentos, contra nombre + bio + palabras clave + especialidades + categorías.
2. **Filtros**: Abierto ahora · Visita sin costo · Con precios · Atiende urgencias, más el grupo
   propio **Tipo de atención**.
3. **Solo los que atienden tu punto** (`serves`). Si ninguno cumple y `expanded=true`, se devuelven
   los de fuera ordenados por distancia, con aviso.
4. **Orden**: Relevancia (correo confirmado primero, luego actualizado más recientemente) ·
   Más cercanos · Más recomendados.

## 2. Petición

```http
GET /v1/discovery/search/providers?q=fuga+calentador&cat=plomeria&lat=20.0539&lng=-99.3403
    &radiusKm=10&open=true&free=true&prices=true&urgent=false&attention=home
    &sort=relevance&expanded=false&cursor=&limit=20
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `q` | string | **80** | no | Texto libre. Se normaliza: minúsculas, sin acentos, se parte en palabras. **Todas** deben coincidir (AND) |
| `cat` | string | **20** | no | Slug de categoría. Sin él, todas |
| `lat` / `lng` | double | −90..90 / −180..180 | no | Sin ellos no se aplica `serves` ni el orden por distancia |
| `radiusKm` | int32 | **{2,5,10,20}** | no | Por defecto **10**, o el guardado del usuario |
| `open` | bool | — | no | Abierto ahora |
| `free` | bool | — | no | Visita sin costo (`visit.mode = free`) |
| `prices` | bool | — | no | Tiene al menos un precio publicado |
| `urgent` | bool | — | no | `schedule.urgent = true` |
| `attention` | string | enum | no | `all` \| `home` (excluye solo-local) \| `shop` (local y local+domicilio, **ordenado por distancia**) |
| `sort` | string | enum | no | `relevance` (por defecto) \| `near` \| `recs` |
| `expanded` | bool | — | no | Si no hay resultados en zona, incluir los de fuera |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "items": [
    {
      "providerId": "019400d4-…",
      "name": "Plomería del Centro",
      "kind": "business",
      "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
      "categories": ["plomeria"],
      "specialties": ["fugas", "calentadores"],
      "bioExcerpt": "Atendemos fugas, cambio de tuberías e instalación…",
      "zoneLabel": "Centro",
      "attentionText": "Va a domicilio",
      "distanceKm": 1.2,
      "distanceText": "a menos de 1 km",
      "status": { "open": false, "text": "Cerrado · abre mañana a las 8:00" },
      "minPrice": { "fromCents": 35000, "text": "Desde $350" },
      "visit": { "mode": "free", "text": "Visita sin costo" },
      "rating": { "avg": 4.6, "count": 12 },
      "recs": 34,
      "badges": ["emailVerified"],
      "updatedText": "Actualizado hace 2 días"
    }
  ],
  "nextCursor": "MDE5NDAwZDQt…",
  "hasMore": true,
  "total": 23,
  "outOfZone": false,
  "appliedFilters": ["open", "free", "attention:home"],
  "notice": null
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `bioExcerpt` | string | **160** | Recorte de `bio`; la bio completa está en el perfil |
| `distanceText` | string | **30** | `kmTxt()`: "a menos de 1 km" / "a ~N km" |
| `outOfZone` | bool | — | `true` cuando se usó `expanded` y estos resultados están fuera del radio |
| `appliedFilters` | array | — | Para pintar los chips activos y poder quitarlos (`rmf`) |
| `notice` | string 120 | — | Aviso de la maqueta cuando no había nada en zona |

**No se devuelve nunca**: teléfono, punto exacto de prestadores a domicilio (solo `zoneLabel` y
distancia aproximada), ni correo.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `q` de más de 80, enum inválido, coordenadas fuera de rango |
| 400 | `request.limit_exceeded` | `limit` mayor que 50 |
| 400 | `request.cursor_invalid` | Cursor de otra combinación de filtros (`filterHash` distinto) |
| 429 | `rate.limit_exceeded` | Política `public` |

## 5. Datos

Dos caminos según haya o no texto:

**Con `q`** — Atlas Search `providers_search` (`compound.must` con `text` sobre `name`, `bio`,
`specialties`, `activity`), después `$geoNear` no es posible en el mismo *pipeline*, así que se
filtra la distancia con `$geoWithin` + `$expr` de `serves()` y se calcula la distancia en la
proyección.

**Sin `q`** — `$geoNear` directo, que ya devuelve `distM` ordenado.

| Colección | Índices |
|---|---|
| `provider_profiles` | **Atlas Search** `providers_search`; `{location: "2dsphere", categories: 1}`; `{updatedAt: -1}` |

Orden (patrón **Strategy**, una clase por criterio):

| `sort` | Implementación |
|---|---|
| `relevance` | `emailVerified: -1`, luego `updatedAt: -1`, luego `_id: -1` |
| `near` | `distM: 1`, luego `_id: -1` |
| `recs` | `recs: -1`, luego `rating.avg: -1`, luego `_id: -1` |

El desempate final por `_id` es lo que hace estable la paginación por cursor.

`readPreference: secondaryPreferred`: una búsqueda puede ir 30 s desactualizada sin daño.

## 6. Caché y rate limit

- **L2 (Redis) 60 s**, clave `search:{hash}` donde `{hash}` = SHA-256 corto del conjunto normalizado
  de filtros **incluido el geohash6** del punto y el cursor.
- Invalida con los tags `provider:{id}` y los de la celda.
- Rate limit **`public`** (60/min por IP). El buscador de la app usa *debounce* de 300 ms.
- `Cache-Control: public, max-age=30` + `ETag`.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Atlas Search** | Búsqueda de texto con normalización de acentos (analizador `lucene.spanish`) |
| **MongoDB** `2dsphere` | Filtro y orden por distancia |
| **Redis** | Caché L2 de resultados por combinación de filtros |
| **HybridCache** | Protección contra *stampede* en búsquedas populares |
| Patrón **Strategy** | Un `IProviderSortStrategy` por criterio de orden |

## 8. Seguridad y privacidad

1. La proyección excluye `location` y `phone` **en la consulta**. La distancia se devuelve
   redondeada (`distanceKm` con un decimal, `distanceText` aproximado) para no permitir
   trilateración: con tres búsquedas desde puntos distintos y una distancia exacta se podría
   calcular el domicilio de un prestador.
2. `serves()` se evalúa **en el servidor**: la app no puede pedir prestadores fuera de su zona salvo
   con `expanded`, que marca el resultado como tal.
3. Los filtros son los de la maqueta y nada más: no hay parámetro para filtrar por teléfono, correo
   ni datos personales.
4. `q` se normaliza y se acota a 80 caracteres: evita consultas de texto patológicas sobre el índice
   de búsqueda.
5. La caché es `public` porque la respuesta no depende de la identidad del usuario, solo de su punto
   y sus filtros.
