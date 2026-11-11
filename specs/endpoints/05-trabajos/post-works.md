# Publicar un trabajo · `POST /v1/discovery/works`

> Refinamiento ejecutable 2026-11-10: [implementación, decisiones y límites](implementacion.md) prevalece sobre los ejemplos históricos de caché/Redis/medios. Autoridad de arquitectura: nv-08.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **Pr** (autenticado, con perfil) |
| Pantalla de la maqueta | 7.10 Publicar → "Publicar un trabajo" · `ACT.pubsave`, campos `ptitle/pprob/pdid/ptime` |
| Spec SDD | nv-03 |
| Idempotente | Sí — `Idempotency-Key` obligatorio |

## 1. Función

El prestador publica un trabajo hecho, que aparece en su perfil y en el feed de la zona. Es el
contenido que alimenta las tarjetas estilo Instagram del feed.

## 2. Petición

```http
POST /v1/discovery/works
Authorization: Bearer <access_token>
Idempotency-Key: 3f8a1c5e-9b24-4d07-b1e5-6a7c8d9e0f12
Content-Type: application/json
```

```json
{
  "category": "plomeria",
  "title": "Cambio de tubería en cocina",
  "text": "Cambiamos la bajada completa y dejamos probada la instalación.",
  "problem": "Fuga en la bajada del fregadero",
  "did": "Reemplazo de 4 m de tubería de cobre",
  "time": "3 horas",
  "tag": "Urgencia resuelta",
  "location": { "lat": 20.0539, "lng": -99.3403 },
  "imageUploadIds": ["019400b2-…", "019400b3-…"]
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `category` | string | **20** | sí | Slug. **Debe estar entre las categorías del perfil** |
| `title` | string | **90** | sí | Mínimo 5 caracteres. **Moderado** |
| `text` | string | **1000** | sí | Mínimo 20 caracteres. **Moderado** |
| `problem`, `did`, `time` | string | **300** cada uno | no | **Moderados** |
| `tag` | string | **40** | no | Etiqueta libre. **Moderada** |
| `location.lat` / `.lng` | double | −90..90 / −180..180 | sí | Dónde se hizo. Se guarda **solo difuminado** |
| `imageUploadIds[]` | array\<uuid> | **máx. 6** | no | Subidas propias y aprobadas |

## 3. Respuesta

`201 Created` · `Location: /v1/discovery/works/{id}`

```json
{
  "workId": "019400f6-ab12-7c34-9d56-7e8f90a1b2c3",
  "category": "plomeria",
  "title": "Cambio de tubería en cocina",
  "images": [{ "url": "…", "thumbUrl": "…", "w": 1200, "h": 1200 }],
  "zoneLabel": "Centro",
  "publicPoint": { "lat": 20.0536, "lng": -99.3398 },
  "recs": 0,
  "createdAt": "2026-11-05T17:10:00Z",
  "moderation": { "status": "approved", "checkedAt": "2026-11-05T17:09:58Z" }
}
```

**No se devuelve nunca**: el `location` exacto que se envió. Solo su versión difuminada.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 403 | `provider.profile_required` | La cuenta no tiene perfil de prestador |
| 400 | `work.category_not_in_profile` | La categoría no está entre las del perfil |
| 403 | `content.inappropriate` | Content Safety rechazó texto o imagen |
| 400 | `content.images_limit` | Más de 6 imágenes |
| 404 | `upload.not_found` · 409 `upload.not_pending` | Subidas inválidas o ajenas |
| 429 | `rate.limit_exceeded` | Política `publish`: **30 trabajos al día** |
| 400 | `validation_failed` | Longitudes fuera de rango |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `works` | `insertOne` | `{providerId: 1, createdAt: -1}`; `{publicPoint: "2dsphere", createdAt: -1}`; `{category: 1, createdAt: -1}` |
| `uploads` | `find({ _id: { $in: ids }, ownerId: sub, status: "approved" })` | `{ownerId, createdAt}` |
| `provider_profiles` | `$inc: { jobs: 1 }` y `$set: { updatedAt }` | `accountId` único |

`publicPoint` y `zoneLabel` los calcula el servidor con el mismo difuminado de 500–800 m del perfil.
El `location` exacto **no se guarda** en `works`: a diferencia del perfil, aquí no se necesita para
`serves()`, así que no existe razón para conservarlo.

Escrituras derivadas:

- `outbox`: **`work.created`** `{ workId, providerId, category, publicPoint }` → invalida
  `feed:{geohash6}` y `provider:{id}`.
- `audit_events`: no aplica (no es acción sensible).

Validator: `title` `maxLength 90`, `text` `maxLength 1000`, `images` `maxItems 6`,
`moderation.status` en `approved|rejected|pending`.

## 6. Caché y rate limit

- Invalida `feed:{geohash6}`, `provider:{id}` y `prov:{id}`.
- Rate limit **`publish`**: 30 trabajos al día por `sub`, contador exacto en Redis.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure AI Content Safety** | Moderación de `title`, `text`, `problem`, `did`, `time`, `tag` y de cada imagen, **antes** de insertar |
| **Front Door CDN** | URLs de las imágenes publicadas |
| **MongoDB** | `works` con índices geoespacial y por prestador |
| **Redis** | Idempotencia, rate limit exacto, invalidación |
| Outbox → Storage Queue | Invalidación del feed |

## 8. Seguridad y privacidad

1. La categoría se valida **contra el perfil**: nadie publica en un rubro que no declaró, que es cómo
   se colaría en búsquedas ajenas.
2. **El punto exacto no se guarda.** Guardar dónde vive un cliente al que se le hizo un trabajo sería
   un dato personal de un tercero que no dio su consentimiento.
3. Moderación **síncrona** de todos los textos y las imágenes antes de persistir
   (constitución §III.5).
4. Las subidas se validan con `_id` + `ownerId` + `status` en una consulta.
5. `jobs` se incrementa en el perfil para la tarjeta de completitud y el orden de búsqueda.
