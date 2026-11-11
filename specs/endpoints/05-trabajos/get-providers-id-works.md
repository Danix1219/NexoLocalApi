# Trabajos de un prestador · `GET /v1/discovery/providers/{id}/works`

> Refinamiento ejecutable 2026-11-10: [implementación, decisiones y límites](implementacion.md) prevalece sobre los ejemplos históricos de caché/Redis/medios. Autoridad de arquitectura: nv-08.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **P/C/Pr** (público) |
| Pantalla de la maqueta | 7.8 Perfil de prestador → pestaña "Trabajos" · `S.ptab` |
| Spec SDD | nv-03 |
| Idempotente | Sí (lectura) |

## 1. Función

Lista paginada de los trabajos publicados por un prestador, para la pestaña de su perfil. Va aparte
de [`GET /providers/{id}`](../03-perfil-prestador/get-providers-id.md) para que el perfil cargue
rápido y los trabajos se paginen por su cuenta.

## 2. Petición

```http
GET /v1/discovery/providers/019400d4-…/works?cursor=&limit=12
Authorization: Bearer <access_token>      # opcional
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto **12** (rejilla de 3 columnas) |

## 3. Respuesta

`200 OK`

```json
{
  "items": [
    {
      "workId": "019400f6-…",
      "category": "plomeria",
      "title": "Cambio de tubería en cocina",
      "textExcerpt": "Cambiamos la bajada completa…",
      "images": [{ "url": "…", "thumbUrl": "…", "w": 1200, "h": 1200 }],
      "zoneLabel": "Centro",
      "recs": 12,
      "recsText": "12 recomendaciones",
      "tag": "Urgencia resuelta",
      "ago": "hace 3 días",
      "saved": false,
      "recommended": false
    }
  ],
  "nextCursor": "MDE5NDAwZjYt…",
  "hasMore": true,
  "total": 18
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `textExcerpt` | string | **300** | Recorte a 3 líneas |
| `saved` / `recommended` | bool | — | **Solo con token**; ausentes para el visitante |
| `total` | int32 | — | = `provider_profiles.jobs`, ya desnormalizado: no se cuenta la colección |

**No se devuelve nunca**: punto exacto (no se guarda), datos del cliente del trabajo, ni teléfono.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `provider.not_found` | El prestador no existe o está despublicado |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 429 | `rate.limit_exceeded` | Política `public` |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `works` | `find({ providerId: id, "moderation.status": "approved" })` ordenado por `_id: -1` | **`{providerId: 1, createdAt: -1}`** |
| `saved`, `recommendations` | Una consulta de marcado con `$in` de los ids de la página | únicos `{accountId, kind, targetId}`, `{accountId, workId}` |

El orden por `_id: -1` equivale a `createdAt: -1` (UUID v7) y hace estable el cursor sin un campo de
desempate extra.

`readPreference: secondaryPreferred`.

## 6. Caché y rate limit

- **L2 (Redis) 5 min**, clave `works:{providerId}:{cursor}:{limit}` para la parte pública.
- `saved` y `recommended` se resuelven por petición, fuera de la caché.
- Invalida con el tag `provider:{id}` (trabajo creado, borrado o recomendado).
- Rate limit **`public`**. `ETag` + `Cache-Control: public, max-age=60`.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Lectura por `providerId` con el índice compuesto |
| **Redis** | Caché L2 de la página pública |
| **Front Door CDN** | Imágenes y miniaturas |

## 8. Seguridad y privacidad

1. Solo se listan trabajos con `moderation.status: approved`: lo pendiente o rechazado no se publica,
   ni al dueño por esta vía (lo ve en su propia pantalla de perfil).
2. La parte cacheada no contiene nada personal; el estado de guardado y recomendación se calcula por
   usuario.
3. `total` sale del contador desnormalizado: evita un `countDocuments` en cada carga de perfil.
4. Sin datos de terceros: un trabajo describe lo que hizo el prestador, no para quién.
