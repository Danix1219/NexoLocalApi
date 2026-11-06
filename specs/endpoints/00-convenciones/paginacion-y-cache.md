# Paginación, caché y Redis

> **Cambio respecto a la guía original**: la caché L2 ya **no** vive en la colección `cache_entries`
> de MongoDB. Ahora es **Redis**. También desaparece `rate_counters`.
> Enmienda el ADR 0005.

---

## 1. Paginación por cursor

Nunca `skip`/`offset`: con `skip` el servidor recorre los documentos saltados y el resultado cambia
si alguien publica mientras paginas.

**Petición**: `?cursor=<opaco>&limit=<1..50>`
**Respuesta**:

```json
{ "items": [ … ], "nextCursor": "MDE5NDAwYTEt…", "hasMore": true }
```

| Campo | Tipo | Máximo | Regla |
|---|---|---|---|
| `items` | array | = `limit` | |
| `nextCursor` | string | 64 | Ausente cuando no hay más |
| `hasMore` | bool | — | |

- El cursor es **Base64Url** de `{ afterId, sort, filterHash }`. Lleva el `filterHash` para rechazar
  un cursor reusado con otros filtros (`400 request.cursor_invalid`).
- Al ir por UUID v7 como `_id`, el orden por `_id` **es** orden cronológico: la página siguiente es
  `{ _id: { $lt: afterId } }` con el índice que ya existe.
- En listas ordenadas por otra cosa (`lastMessageAt`, `createdAt`, distancia) el cursor lleva también
  el valor de desempate: `{ sortValue, afterId }`.
- `limit` por defecto **20**, máximo **50** (`400 request.limit_exceeded`).

---

## 2. Caché en tres niveles

| Nivel | Qué | Dónde | Invalidación |
|---|---|---|---|
| **L1** | Memoria del proceso, por réplica | `HybridCache` local | TTL corto + tags |
| **L2** | **Redis** (compartido por las réplicas, con protección contra *stampede*) | Azure Cache for Redis / contenedor en dev | TTL + tags al recibir eventos |
| **Salida** | Output cache + `ETag` / `Cache-Control` en los GET públicos | API | `If-None-Match` → `304` |
| **Cliente** | `dio_cache_interceptor`, `drift`, `cached_network_image` | App | Al refrescar, cambiar de cuenta y en logout |

### TTL por dato

| Dato | L1 | L2 (Redis) | Clave |
|---|---|---|---|
| Categorías y especialidades | 1 h | 24 h | `cat:all:v1` |
| Perfil público del prestador | 2 min | 10 min | `prov:{id}:v1` |
| Búsqueda de prestadores | 30 s | 60 s | `search:{hash}` |
| Sugerencias del buscador | — | 5 min | `sugg:{q}` |
| Feed por zona | 30 s | 60 s | `feed:{geohash6}:{cursor}` |
| Puntos del mapa | — | 60 s | `map:{kind}:{geohash6}:{radiusKm}` |
| Opiniones de un prestador | — | 5 min | `rev:{providerId}:{cursor}` |
| Pantallas de inicio y cuenta | **sin caché** | — | Son personales |
| Pedidos, chat, notificaciones | **sin caché** | — | Datos vivos |

`{hash}` = SHA-256 corto (16 hex) del conjunto normalizado de filtros, para que dos búsquedas
equivalentes compartan entrada.

### Invalidación por tags

| Tag | Lo emite | Invalida |
|---|---|---|
| `provider:{id}` | `provider.updated`, `review.created`, `work.created` | `prov:{id}`, `search:*` del área, `rev:{id}` |
| `feed:{geohash6}` | `work.created`, `work.deleted` | `feed:*`, `map:providers:*` de la celda |
| `requests:{geohash6}` | `request.created`, `request.closed` | `map:requests:*`, oportunidades |
| `categories` | cambio del catálogo (manual) | `cat:all` |

La invalidación la dispara el **worker** al consumir el evento del outbox, no el handler: así una
réplica caída no deja la caché sucia.

---

## 3. Para qué más se usa Redis

| Uso | Estructura | TTL | Nota |
|---|---|---|---|
| Caché L2 | `string` (bytes de `HybridCache`) | según tabla | `nexo:cache:*` |
| Códigos de verificación de correo | `hash` con el **hash** del código y `attempts` | 15 min | `nexo:otp:email:{emailHash}`. El código en claro **nunca** se guarda |
| Tickets de verificación | `string` de un solo uso (`GETDEL`) | 30 min | `nexo:ticket:{ticket}` |
| Contadores exactos de rate limit | `INCR` + `EXPIRE` | ventana | `nexo:rl:{politica}:{particion}:{ventana}` |
| Idempotencia de POST | `string` con el hash del cuerpo y la respuesta | 24 h | `nexo:idem:{key}` |
| Bloqueo por intentos de login | `INCR` | 15 min | `nexo:lock:{emailHash}`; a los 5 → `423` |
| Tickets de WebSocket | `string` de un solo uso | 60 s | `nexo:ws:{ticket}` |
| Pub/sub de SignalR entre réplicas | canal | — | Backplane del Hub de chat |
| Lista negra de `jti` | `string` | = vida del access token (10 min) | `nexo:jti:{jti}` |

**Regla**: nada que deba sobrevivir a un reinicio de Redis vive solo en Redis. Los códigos y tickets
son reemplazables (el usuario pide otro); los contadores de rate limit se reinician y eso es
aceptable; la lista negra de `jti` dura menos que el token.

### Fallo de Redis

`Microsoft.Extensions.Http.Resilience` con circuit breaker. Si Redis no responde:

- La caché L2 se **omite** (se sirve desde L1 o desde Mongo). No se cae la API.
- Los contadores exactos de rate limit pasan a **modo conservador**: se aplica el límite en memoria
  por réplica, que es más estricto.
- Los códigos de verificación y el login **sí dependen** de Redis: se responde
  `503 server.dependency_unavailable`. Es preferible a aceptar un código sin poder contarlo.

---

## 4. ETag y respuestas condicionales

- Los GET públicos (`categories`, `providers/{id}`, `works`, `reviews`) devuelven
  `ETag: "<sha256-16>"` del cuerpo serializado y `Cache-Control: public, max-age=<L1>`.
- Con `If-None-Match` coincidente se responde **`304`** sin cuerpo.
- Los GET autenticados llevan `Cache-Control: private, no-store` salvo que se indique lo contrario
  en el `.md` del endpoint.
- Las imágenes de CDN llevan `Cache-Control: public, max-age=31536000, immutable` porque la URL
  cambia con el contenido (nombre por hash). Ver [`../14-imagenes-cdn/`](../14-imagenes-cdn/).

---

## 5. Lecturas y escrituras en Mongo

- Escrituras: `writeConcern: majority`.
- Lecturas donde la frescura no es crítica (feed, búsqueda, sugerencias):
  `readPreference: secondaryPreferred`.
- Lecturas de pedidos, chat y notificaciones: **primario**, siempre.
