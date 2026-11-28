# Estado y borrado de subidas, portada del perfil · `/v1/discovery/uploads/{id}`, `/v1/discovery/me/cover`

| Campo | Valor |
|---|---|
| Módulo | Discovery (Media) |
| Quién | Cuenta autenticada, dueña de la subida |
| Spec SDD | nv-14 |
| Rate limit | `user` |
| Relacionadas | [post-uploads](post-uploads.md), [post-uploads-id-complete](post-uploads-id-complete.md), [pipeline y purga](pipeline-cdn-y-purga.md) |

## Rutas

| Método | Ruta | Qué hace | Respuestas |
|---|---|---|---|
| GET | `/v1/discovery/uploads/{id}` | Estado de una subida propia (`pending`, `processing`, `approved`) para que la app espere el procesamiento y la moderación. Rechazada o vencida responde con su código (`upload.rejected_by_moderation`, `upload.expired`) | 200, 400, 401, 404 `upload.not_found` |
| DELETE | `/v1/discovery/uploads/{id}` | Marca como `deleted` una subida propia (el usuario quitó la foto); un proceso posterior limpia el blob (`cleaned=false`) | 204, 401, 404 `upload.not_found` |
| GET | `/v1/discovery/me/cover` | URL de la portada actual del perfil propio, o `null` | 200, 401 |
| PUT | `/v1/discovery/me/cover` | Fija como portada una subida propia `approved` de propósito `cover`. Cuerpo `{ "uploadId": "…" }`. Devuelve `{ "url" }` | 200, 401, 404 `upload.not_found` |

## Reglas

- Nunca se aceptan subidas de otra cuenta (404, no 403, para no revelar existencia).
- Las imágenes pasan por Content Safety al completar la subida; solo una `approved` puede ser portada.
- Respuestas de imagen con `X-Content-Type-Options: nosniff`.
