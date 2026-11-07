# Pedir URL de subida · `POST /v1/discovery/uploads`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Cualquier selector de foto: registro, perfil, publicación, chat · `change` de `<input type=file>` |
| Spec SDD | nv-16 |
| Idempotente | No — cada llamada crea una intención de subida |

## 1. Función

Devuelve una **URL SAS** para que la app suba el archivo **directamente a Azure Blob Storage**, sin
pasar por la API. Después se confirma con
[`POST /uploads/{id}/complete`](post-uploads-id-complete.md).

Por qué subida directa y no `multipart` a la API: una imagen de 5 MB atravesando el contenedor
consume memoria, ancho de banda y tiempo de petición del plan gratuito, y no aporta nada — la
validación de verdad ocurre después, en el worker.

## 2. Petición

```http
POST /v1/discovery/uploads
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "purpose": "work", "contentType": "image/jpeg", "size": 2458112, "width": 3024, "height": 3024 }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `purpose` | string | enum | sí | `avatar` \| `cover` \| `work` \| `portfolio` \| `chat` \| `request` |
| `contentType` | string | enum | sí | `image/jpeg` \| `image/png` \| `image/webp`. Nada más |
| `size` | int32 | **1 – 5 242 880** | sí | Bytes. Máximo **5 MB** |
| `width` / `height` | int32 | **1–4096** | no | Si se envían y exceden 4096, se rechaza aquí y no tras subir |

## 3. Respuesta

`201 Created`

```json
{
  "uploadId": "019400b2-1c3e-7f21-9b84-3d5e6f70a1b2",
  "uploadUrl": "https://nexostorage.blob.core.windows.net/incoming/019400b2…?sv=2024-11-04&sig=…",
  "method": "PUT",
  "headers": { "x-ms-blob-type": "BlockBlob", "Content-Type": "image/jpeg" },
  "expiresInSeconds": 900,
  "maxSizeBytes": 5242880
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `uploadUrl` | string | **1000** | SAS de **solo escritura**, sobre un blob concreto, válida 15 min |
| `method` | string | — | `PUT` |
| `expiresInSeconds` | int32 | — | 900 |

**No se devuelve**: la URL final del CDN. Todavía no existe: el archivo no está moderado.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `upload.content_type_invalid` | No es jpeg, png ni webp |
| 413 | `upload.too_large` | `size` mayor que 5 MB |
| 400 | `upload.dimensions_too_large` | Más de 4096 px de lado |
| 403 | `provider.profile_required` | `purpose` de `work` o `portfolio` sin perfil de prestador |
| 429 | `rate.limit_exceeded` | Política **`upload`**: 30 al día |
| 403 | `account.pending_deletion` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `uploads` | `insertOne` | `{ownerId, createdAt}`; **TTL en `expiresAt`** para las pendientes |

```js
{
  _id: UUID_v7, ownerId: sub, purpose: "work",
  blobPath: "incoming/019400b2-1c3e-7f21-9b84-3d5e6f70a1b2",
  contentType: "image/jpeg", size: 2458112,
  status: "pending",                    // pending | uploaded | approved | rejected
  expiresAt: ISODate,                   // +15 min; TTL limpia las abandonadas
  createdAt: ISODate
}
```

La SAS se firma para **ese blob exacto**, solo con permiso de escritura (`w`), 15 minutos y
`Content-Type` fijado. No se emite una SAS de contenedor.

El TTL sobre `expiresAt` borra el registro de las subidas que nunca se completaron; un
`CleanupJob` del worker borra también los blobs huérfanos de `incoming/`.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`upload`**: **30 al día** por `sub`, exacto en Redis.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure.Storage.Blobs** | Firma de la SAS de escritura, acotada al blob |
| **Azure Key Vault** | Llave de la cuenta de almacenamiento |
| **MongoDB** | `uploads` con TTL |
| **Redis** | Contador exacto del límite diario |
| **image_picker** + **flutter_image_compress** (app) | La app comprime antes de subir, con lo que casi nunca se llega a los 5 MB |

## 8. Seguridad y privacidad

1. **La SAS es de un solo blob, solo escritura y 15 minutos.** No se entrega una llave de
   contenedor: si la URL se filtra, lo único que permite es sobrescribir ese archivo concreto antes
   de que se modere.
2. El archivo cae en `incoming/`, un contenedor **privado y no servido por el CDN**. Nada es público
   hasta pasar la moderación.
3. El tipo y el tamaño se validan **antes** de dar la URL y **otra vez** en el worker leyendo los
   bytes: el `contentType` declarado por el cliente no es de fiar.
4. El `purpose` acota qué se puede subir según el papel: un cliente sin perfil no sube fotos de
   portafolio.
5. El límite de 30 al día evita usar el almacenamiento gratuito como disco.
6. El nombre del blob es un UUID v7, no el nombre original del archivo: los nombres de archivo
   filtran información (modelo de cámara, rutas, nombres de personas).
