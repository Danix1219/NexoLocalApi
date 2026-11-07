# Confirmar la subida · `POST /v1/discovery/uploads/{id}/complete`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C/Pr** (dueño de la subida) |
| Pantalla de la maqueta | Tras elegir la foto, mientras se muestra el indicador de carga |
| Spec SDD | nv-16 |
| Idempotente | Sí — confirmar lo ya confirmado devuelve el mismo estado |

## 1. Función

Avisa de que el archivo ya está en el Blob y **encola la moderación**. El worker verifica, modera,
genera miniatura, publica en el CDN y deja la subida `approved` o `rejected`.

La respuesta es `202`: cuando llega, la imagen **todavía no es pública**.

## 2. Petición

```http
POST /v1/discovery/uploads/019400b2-1c3e-7f21-9b84-3d5e6f70a1b2/complete
Authorization: Bearer <access_token>
```

Sin cuerpo.

| Parámetro | Tipo | Requerido | Regla |
|---|---|---|---|
| `id` (ruta) | uuid v7 | sí | Subida `pending` del `sub` |

## 3. Respuesta

`202 Accepted`

```json
{
  "uploadId": "019400b2-…",
  "status": "processing",
  "estimatedSeconds": 4,
  "pollUrl": "/v1/discovery/uploads/019400b2-…"
}
```

Y al consultar `GET /v1/discovery/uploads/{id}` una vez listo:

```json
{
  "uploadId": "019400b2-…",
  "status": "approved",
  "url": "https://cdn.nexolocal.mx/w/7a1b2c3d4e5f6071.webp",
  "thumbUrl": "https://cdn.nexolocal.mx/w/7a1b2c3d4e5f6071_t.webp",
  "width": 1600,
  "height": 1600,
  "bytes": 184320
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `status` | string | enum | `processing` \| `approved` \| `rejected` |
| `url` | string | **200** | **URL del CDN (Front Door)**, con nombre por **hash del contenido** |
| `thumbUrl` | string | **200** | Miniatura 400 × 400 WebP |

La app recibe el resultado por **SignalR** (`uploadProcessed`) y usa `pollUrl` solo como respaldo.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `upload.not_found` | No existe **o no es tuya** |
| 409 | `upload.not_pending` | Ya se confirmó o ya se procesó |
| 410 | `upload.expired` | Pasaron los 15 min de la SAS |
| 400 | `upload.blob_missing` | El blob no está: la subida al Blob falló |
| 403 | `upload.rejected_by_moderation` | Al consultar una rechazada |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `uploads` | `findOneAndUpdate({ _id: id, ownerId: sub, status: "pending" }, { $set: { status: "uploaded" } })` — **id, propiedad y estado en el mismo filtro** | `{ownerId, createdAt}` |

Escrituras derivadas:

- `outbox`: **`upload.completed`** `{ uploadId, ownerId, purpose, blobPath }` → lo consume el
  **`ImagePipeline`** del worker.

### Lo que hace el worker (`ImagePipeline`)

1. **Lee los bytes** y verifica el tipo real por los *magic bytes*, no por el `contentType`
   declarado. Un PNG renombrado a `.jpg` o un archivo que no es imagen se rechaza aquí.
2. Comprueba dimensiones y tamaño reales.
3. **Modera con Azure AI Content Safety** (análisis de imagen). Si se rechaza:
   `status: "rejected"`, se borra el blob y se avisa a la app.
4. **Normaliza con SkiaSharp**: reorienta por EXIF, **elimina todos los metadatos EXIF** y convierte
   a WebP con el lado mayor a 1600 px.
5. Genera la **miniatura** 400 × 400 WebP.
6. Calcula el **SHA-256 del resultado** y lo usa como nombre: `{hash}.webp` y `{hash}_t.webp`.
7. Copia a `public/{prefijo}/` y **borra el blob de `incoming/`**.
8. `status: "approved"`, con `url`, `thumbUrl`, `width`, `height`, `bytes`.
9. Emite `upload.approved`, que la app recibe por SignalR.

## 6. Caché y rate limit

- La respuesta no se cachea.
- Las imágenes publicadas se sirven con
  **`Cache-Control: public, max-age=31536000, immutable`**: la URL cambia con el contenido, así que
  nunca hay que invalidar por cambio. Solo se **purga** al borrar.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure Blob Storage** | `incoming/` privado y `public/` como origen del CDN |
| **Azure Front Door** (CDN) | Entrega desde el borde, compresión y TLS. Es el cambio de tecnología de este bloque |
| **Azure AI Content Safety** | Moderación de imagen antes de publicar |
| **SkiaSharp** | Reorientación, borrado de EXIF, WebP, miniatura (ADR 0009) |
| **Azure.Storage.Queues** | Cola del `ImagePipeline` |
| **Azure SignalR** | Aviso `uploadProcessed` a la app |

### Rutas del CDN

| Prefijo | Para qué |
|---|---|
| `/av/` | Avatares |
| `/cv/` | Portadas |
| `/w/` | Trabajos y portafolio |
| `/ch/` | Imágenes de chat |
| `/rq/` | Fotos de solicitudes |

**Las de `/ch/` no se cachean en el borde** con el resto de las reglas: son privadas, y aunque la URL
por hash no es enumerable, se sirven con `private` y TTL corto.

## 8. Seguridad y privacidad

1. **El tipo real se verifica por los bytes.** Confiar en el `contentType` declarado es la vía
   clásica para subir algo que no es una imagen.
2. **Se borran los metadatos EXIF**, que suelen incluir **coordenadas GPS**. Sin esto, la foto de un
   trabajo publicaría la ubicación exacta de la casa de un cliente — justo lo que todo el diseño de
   privacidad evita.
3. **Nada es público antes de la moderación.** `incoming/` es privado y no está detrás del CDN.
4. El **nombre por hash del contenido** hace que las URL no sean enumerables y permite caché
   inmutable de un año.
5. Propiedad y estado en el mismo filtro: no se confirma la subida de otro.
6. El blob original se borra tras procesar: no queda una copia sin moderar ni con EXIF.
7. Las imágenes de chat se sirven como privadas: no comparten la política de caché pública del resto.
