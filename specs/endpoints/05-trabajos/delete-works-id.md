# Borrar un trabajo · `DELETE /v1/discovery/works/{id}`

> Refinamiento ejecutable 2026-11-10: [implementación, decisiones y límites](implementacion.md) prevalece sobre los ejemplos históricos de caché/Redis/medios. Autoridad de arquitectura: nv-08.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **Pr** (autenticado, autor del trabajo) |
| Pantalla de la maqueta | 7.11 Mi perfil → menú "⋯" del trabajo · `ACT.delw` |
| Spec SDD | nv-03 |
| Idempotente | Sí |

## 1. Función

Retira un trabajo del perfil y del feed. Borrado **real**, no lógico: es contenido del propio
prestador y no hay contraparte con derecho a conservarlo.

## 2. Petición

```http
DELETE /v1/discovery/works/019400f6-ab12-7c34-9d56-7e8f90a1b2c3
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Requerido | Regla |
|---|---|---|---|
| `id` (ruta) | uuid v7 | sí | Debe ser un trabajo del prestador del `sub` |

## 3. Respuesta

`204 No Content` — sin cuerpo.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `work.not_found` | No existe **o no es tuyo**. No se distingue |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `works` | `findOneAndDelete({ _id: id, providerId: <perfil del sub> })` — **id y propiedad en el mismo filtro** | `{providerId: 1, createdAt: -1}` |
| `provider_profiles` | `$inc: { jobs: -1 }` (sin bajar de 0) | `accountId` |
| `recommendations` | `deleteMany({ workId: id })` | `{accountId, workId}` único |

Se usa `findOneAndDelete` porque hace falta el documento borrado para saber qué imágenes purgar y de
qué celda invalidar el feed; si devuelve `null` → `404`, en una sola ida a la base.

Escrituras derivadas:

- `outbox`: **`work.deleted`** `{ workId, providerId, publicPoint, imageUrls }` → el worker invalida
  `feed:{geohash6}` y `provider:{id}`, y **purga las imágenes** del CDN y del Blob.
- Las recomendaciones del trabajo se borran: el contador `recs` del prestador se recalcula en el
  worker con la suma de sus trabajos vivos.

## 6. Caché y rate limit

- Invalida `feed:{geohash6}`, `provider:{id}` y `prov:{id}`.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `findOneAndDelete` filtrado por propiedad |
| **Front Door CDN** | Purga por ruta de las imágenes retiradas |
| **Azure Blob** | Borrado de los blobs originales y las miniaturas |
| Outbox → Storage Queue | Purga e invalidación en el worker |

## 8. Seguridad y privacidad

1. **Propiedad en el mismo filtro del borrado**: no se lee primero y se borra después.
2. `404` tanto si no existe como si es de otro prestador.
3. Las imágenes se purgan del CDN y se borran del Blob: borrar el documento y dejar la foto accesible
   por URL sería un borrado falso.
4. Las recomendaciones ajenas al trabajo se eliminan con él; no quedan huérfanas apuntando a nada.
5. El borrado es inmediato y definitivo, como corresponde a contenido propio.
