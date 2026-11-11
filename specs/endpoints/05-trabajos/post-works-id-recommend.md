# Recomendar un trabajo · `POST /v1/discovery/works/{id}/recommend`

> Refinamiento ejecutable 2026-11-10: [implementación, decisiones y límites](implementacion.md) prevalece sobre los ejemplos históricos de caché/Redis/medios. Autoridad de arquitectura: nv-08.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C** (autenticado) |
| Pantalla de la maqueta | Feed e icono de recomendar en la tarjeta · `ACT.rec` |
| Spec SDD | nv-03 |
| Idempotente | Sí — recomendar lo ya recomendado devuelve el mismo estado |

## 1. Función

Marca un trabajo como recomendado. Es una de las acciones que la maqueta pone detrás del **gate de
cuenta** (`ACT.rec` envuelto para el visitante), junto con guardar, comparar y contactar.

Suma al contador del trabajo (`recs`, que la UI muestra como "N recomendaciones") y al del prestador,
que alimenta el orden *Más recomendados* de la búsqueda.

## 2. Petición

```http
POST /v1/discovery/works/019400f6-ab12-7c34-9d56-7e8f90a1b2c3/recommend
Authorization: Bearer <access_token>
```

Sin cuerpo.

| Parámetro | Tipo | Requerido | Regla |
|---|---|---|---|
| `id` (ruta) | uuid v7 | sí | Trabajo publicado y aprobado |

## 3. Respuesta

`200 OK`

```json
{ "workId": "019400f6-…", "recommended": true, "recs": 13, "recsText": "13 recomendaciones" }
```

| Campo | Tipo | Regla |
|---|---|---|
| `recommended` | bool | Siempre `true` tras esta llamada |
| `recs` | int32 | Contador ya actualizado |

**No se devuelve nunca**: quiénes recomendaron. Es un contador agregado, no una lista de personas.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `work.not_found` | No existe, no está aprobado, o el prestador está despublicado |
| 403 | `work.own_content` | Un prestador no recomienda sus propios trabajos |
| 401 | `auth.token_expired` | El visitante recibe `401` y la app abre el gate de cuenta |
| 429 | `rate.limit_exceeded` | Política `user` |

**No hay `409`**: recomendar dos veces no es un error, es el mismo estado. El índice único lo absorbe.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `recommendations` | `updateOne({ accountId: sub, workId: id }, { $setOnInsert: { createdAt } }, { upsert: true })` | **único `{accountId, workId}`** |
| `works` | `$inc: { recs: 1 }` **solo si el upsert insertó** (`upsertedId != null`) | `_id` |

El índice único más el `upsert` es lo que hace la operación idempotente sin una comprobación previa:
si ya existía, no se inserta y no se incrementa.

Escrituras derivadas:

- `outbox`: **`work.recommended`** `{ workId, providerId, accountId }` → el worker recalcula
  `provider_profiles.recs` e invalida `provider:{id}`.
- Notificación push al prestador: "Recomendaron tu trabajo". Sin revelar quién si el prestador no
  tiene relación con esa persona.

## 6. Caché y rate limit

- Invalida `provider:{id}` y las entradas de `feed:{geohash6}` que contienen el trabajo.
- Rate limit **`user`** (120/min). No necesita `publish`: no crea contenido visible.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `upsert` con índice único; `$inc` condicionado |
| **FirebaseAdmin** (worker) | Push al prestador |
| **Redis** | Invalidación por tags |
| Outbox → Storage Queue | Recálculo del contador del prestador |

## 8. Seguridad y privacidad

1. Una recomendación por cuenta y trabajo, garantizada por el **índice único**, no por una lectura
   previa: así no se infla el contador con llamadas repetidas.
2. **No se expone quién recomendó.** El contador es agregado; la relación individual queda en la
   colección, que no tiene endpoint de lectura.
3. `work.own_content` evita el autobombo, que falsearía el orden *Más recomendados*.
4. Una recomendación es **opinión de usuarios**, el tercer nivel de confianza, y nunca se presenta
   como verificación de Nexo Local (constitución §IV.3).
5. El visitante recibe `401` del servidor: el gate no es solo de UI.
