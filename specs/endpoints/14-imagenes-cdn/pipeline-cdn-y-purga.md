# Pipeline de imágenes, CDN y purga (no es un endpoint)

> Documento de apoyo del bloque 14. Describe la infraestructura que usan
> [`POST /uploads`](post-uploads.md) y [`POST /uploads/{id}/complete`](post-uploads-id-complete.md),
> y cómo se borra una imagen.
>
> **Cambio de tecnología**: las imágenes ya no se sirven desde el Blob, sino desde
> **Azure Front Door (CDN)**. Nuevo ADR 0011.

---

## 1. Por qué un CDN

| Sin CDN (Blob directo) | Con Front Door |
|---|---|
| Cada foto del feed sale de la región del Blob | Sale del borde más cercano |
| El egreso del Blob consume cuota del plan gratuito | El borde absorbe las lecturas repetidas |
| Sin compresión automática ni HTTP/2 por defecto | Brotli y HTTP/2 de serie |
| La URL del Blob revela la cuenta de almacenamiento | Dominio propio `cdn.nexolocal.mx` |

El feed es la pantalla de entrada y está hecho de fotos: es el 90 % del tráfico de bytes de la app.

---

## 2. Arquitectura

```
App ──PUT (SAS 15 min)──▶ Blob: incoming/{uploadId}        [privado, sin CDN]
                                   │
                                   ▼  Storage Queue: upload.completed
                          Worker · ImagePipeline
                           1. magic bytes + dimensiones
                           2. Content Safety (imagen)
                           3. SkiaSharp: EXIF fuera, WebP, 1600 px
                           4. miniatura 400×400
                           5. SHA-256 → nombre
                                   │
                                   ▼
                          Blob: public/{prefijo}/{hash}.webp
                                   │
                                   ▼
                     Azure Front Door · cdn.nexolocal.mx
                                   │
                                   ▼
                          App (y cached_network_image)
```

| Contenedor | Acceso | Servido por CDN |
|---|---|---|
| `incoming/` | privado, SAS de escritura por blob | **no** |
| `public/` | privado; solo Front Door lo lee con identidad administrada | **sí** |

El `public/` **no es público**: Front Door es el único que puede leerlo. Así nadie puede saltarse el
CDN ni descubrir la cuenta de almacenamiento.

---

## 3. Reglas de caché por ruta

| Ruta | `Cache-Control` | Por qué |
|---|---|---|
| `/av/*`, `/cv/*` | `public, max-age=31536000, immutable` | Avatares y portadas; URL por hash |
| `/w/*` | `public, max-age=31536000, immutable` | Trabajos y portafolio |
| `/rq/*` | `public, max-age=31536000, immutable` | Fotos de solicitudes |
| `/ch/*` | `private, max-age=3600` | **Imágenes de chat**: privadas, caché corta y no compartida |

**La URL lleva el hash del contenido**, así que una imagen distinta es una URL distinta: no existe el
problema de "actualicé la foto y se ve la vieja", y nunca hace falta purgar por cambio.

---

## 4. Purga

Se purga solo al **borrar**, no al cambiar:

| Disparador | Qué se purga |
|---|---|
| [Borrar un trabajo](../05-trabajos/delete-works-id.md) | Sus imágenes y miniaturas |
| Quitar fotos del portafolio ([`PATCH .../portfolio`](../03-perfil-prestador/patch-providers-me-seccion.md)) | Las retiradas |
| Cambiar avatar o portada | La anterior |
| [Baja de cuenta](../02-cuenta/post-me-deletion.md) ejecutada | Todas las del usuario |
| Moderación que retira contenido | Las afectadas |

Secuencia, en el worker:

1. `DELETE` del blob en `public/`.
2. Purga en Front Door por ruta exacta (`/w/{hash}.webp` y `/w/{hash}_t.webp`).
3. `uploads.status: "deleted"` para dejar rastro de que existió y se borró.

La purga del CDN es **asíncrona y puede tardar minutos**. Como el documento que la referenciaba ya no
existe, nadie llega a esa URL salvo quien la tuviera guardada. Por eso en el caso de la **baja de
cuenta** la purga se verifica y se reintenta: ahí sí es una obligación legal (ARCO) y no basta con
que la referencia desaparezca.

---

## 5. Límites y tipos

| Aspecto | Valor |
|---|---|
| Entrada aceptada | `image/jpeg`, `image/png`, `image/webp` |
| Tamaño máximo de entrada | **5 MB** |
| Dimensión máxima de entrada | 4096 × 4096 px |
| Salida | WebP, lado mayor 1600 px, calidad 82 |
| Miniatura | 400 × 400 px WebP, recorte centrado |
| Imágenes por trabajo o portafolio | **6** |
| Imágenes por mensaje de chat | **1** |
| Nombre | `{sha256-16}.webp` y `{sha256-16}_t.webp` |

---

## 6. Qué no se guarda

| Dato | Por qué |
|---|---|
| **EXIF completo** (incluido **GPS**) | Una foto de un trabajo revelaría la ubicación exacta de la casa de un cliente. Se elimina **siempre** |
| Nombre original del archivo | Filtra información del dispositivo y de la persona |
| El archivo original tras procesar | Se borra de `incoming/`: no queda copia sin moderar ni con EXIF |
| Las imágenes en MongoDB | El cluster Free son 512 MB: solo se guarda la URL (`string`, 200) |

---

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure Blob Storage** | `incoming/` y `public/` |
| **Azure Front Door** | CDN con dominio propio, Brotli, TLS y purga por ruta |
| **Identidad administrada** | Front Door lee `public/` sin llaves en configuración |
| **Azure AI Content Safety** | Moderación de imagen antes de publicar |
| **SkiaSharp** | EXIF fuera, WebP, miniatura (ADR 0009) |
| **Azure.Storage.Queues** | Cola del `ImagePipeline` |
| **Azure SignalR** | `uploadProcessed` a la app |
| **cached_network_image** (app) | Caché en el dispositivo; respeta `Cache-Control` |

**Costo cero**: Front Door en el perfil **eval**; en **dev** la app apunta directo al Blob con SAS de
lectura, para no tener costo fijo (constitución §VI.1 y ADR 0008).

---

## 8. Seguridad y privacidad

1. **`public/` no es público.** Solo Front Door lo lee, con identidad administrada. El CDN es la
   única puerta.
2. **El EXIF se borra siempre.** Es la medida de privacidad más importante de este bloque: sin ella,
   todo el cuidado con las zonas difuminadas se perdería en la primera foto subida.
3. **Nada se publica sin moderar**, y el original se borra tras procesar.
4. Las **URL por hash** no son enumerables y permiten caché inmutable.
5. Las imágenes de **chat** se sirven como privadas y con TTL corto: no comparten la política del
   contenido público.
6. La purga se **verifica y reintenta** en la baja de cuenta, donde borrar es una obligación y no una
   limpieza.
