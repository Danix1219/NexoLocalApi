# Tipos y límites — diccionario de datos

> **Fuente única de verdad de tipos y tamaños.** Los `.md` de cada endpoint citan esta tabla en vez de
> repetirla. Si un campo no está aquí, no existe: agrégalo aquí primero.
>
> Base de datos: **MongoDB Atlas (cluster Free M0, 512 MB)**. Los tamaños se aplican en tres lugares:
> el `Validator` de FluentValidation (entrada), el **JSON Schema validator** de la colección
> (`$jsonSchema` con `maxLength`) y el DTO de salida.

---

## 1. Convenciones generales

| Aspecto | Regla |
|---|---|
| `_id` | `Guid.CreateVersion7()` (UUID v7, ordenable por tiempo) → `BsonBinaryData` subtipo **4** (`GuidRepresentation.Standard`) |
| Fechas | `DateTime.UtcNow` siempre; en Mongo `BsonType.DateTime` (64 bits, ms). **Nunca** hora local |
| Montos | `int32` en **centavos MXN** (`amountCents`). Nunca `decimal` ni `double` |
| Ubicaciones | GeoJSON `Point` → `{ type: "Point", coordinates: [lng, lat] }` — **lng primero** |
| Textos | `string` UTF-8. El `maxLength` del validator cuenta **caracteres**, no bytes |
| Enums | se guardan como `string` (slug en minúsculas), no como `int`, para que el dump sea legible |
| Booleanos | `bool`; nunca `0/1` ni `"true"` |
| Nulos | un campo opcional ausente **se omite** del documento; no se guarda `null` |
| Arrays | siempre con tope documentado. Sin tope = vector de crecimiento ilimitado = prohibido |
| Moderación | toda colección pública lleva `moderation: { status, checkedAt }` |

### Presupuesto de los 512 MB

- **Las imágenes nunca van a MongoDB.** Solo se guarda su URL de CDN (`string`, 200).
- Los textos largos (`bio`, `text`, `description`) tienen tope explícito por eso.
- Lo volátil vive en **Redis**, no en Mongo: caché L2, contadores de rate limit, códigos de
  verificación, tickets de WebSocket. Ver [`paginacion-y-cache.md`](paginacion-y-cache.md).

---

## 2. Identidad y cuenta

| Campo | Tipo BSON | Máximo | Requerido | Regla |
|---|---|---|---|---|
| `email` | string | **254** | sí | RFC 5321. Se normaliza a minúsculas y se recorta. Índice **único**. Es la identidad de la cuenta |
| `emailCode` | string | **6** | sí | 6 dígitos numéricos. **No se guarda en Mongo**: vive en Redis con TTL 15 min, y solo su hash |
| `verificationTicket` | string | **64** | sí | Opaco, de un solo uso, TTL 30 min en Redis. Prueba que el correo fue verificado |
| `passwordHash` | string | **100** | sí | **Argon2id**. La contraseña en claro: **8–64** caracteres, al menos una letra y un número (regla `pwOK` de la maqueta). **Nunca se devuelve ni se escribe en logs** |
| `name` | string | **80** | sí | Nombre de la persona (paso `who` del registro) |
| `currentMode` | string | enum | sí | `client` \| `provider` |
| `hasProvider` | bool | — | sí | `true` si la cuenta tiene perfil profesional |
| `status` | string | enum | sí | `active` \| `pendingDeletion` \| `deleted` |
| `termsAcceptedAt` | date | — | sí | Instante de aceptación de términos y aviso de privacidad |
| `phone` | string | **13** | **no** | E.164 México: `+52` + 10 dígitos. **Dato opcional del perfil**, ya no es la identidad. Índice único **disperso** |
| `phoneVerifiedAt` | date | — | no | Enciende la insignia *Teléfono confirmado* |
| `fcmToken` | string | **4096** | sí | Token de Firebase Cloud Messaging. Índice **único** |
| `platform` | string | enum | sí | `android` (único valor por ahora) |
| `appVersion` | string | **20** | sí | SemVer, p. ej. `1.4.2` |

---

## 3. Perfil de prestador

| Campo | Tipo BSON | Máximo | Requerido | Regla |
|---|---|---|---|---|
| `kind` | string | enum | sí | `trade` (Persona con oficio) \| `business` (Negocio) |
| `name` | string | **80** | sí | Nombre del perfil o nombre comercial |
| `owner` | string | **80** | solo `business` | Responsable del negocio |
| `sector` | string | **80** | solo `business` | Rubro (`SECT` de la maqueta) |
| `activity` | string | **80** | solo `business` | Actividad principal; alimenta `inferCats` |
| `trades[]` | array\<string> | **5** | solo `trade` | Oficios declarados |
| `categories[]` | array\<string> | **3** | sí | Slugs de las 7 categorías. En `business` se **infieren** de `activity` + `bio` |
| `specialties[]` | array\<string> | **5** | no | Slugs; el catálogo tiene 5 por categoría |
| `bio` | string | **600** | sí | **Mínimo 20 caracteres** (`obValid` de la maqueta) |
| `experience` | int32 | **0–70** | no | Años de oficio |
| `crew` | string | enum | no | `alone` \| `withHelpers` (persona) · `team` (negocio) |
| `teamSize` | int32 | **1–200** | no | Solo `business` |
| `attention` | string | enum | sí | `home` (a domicilio) \| `shop` (solo local) \| `both`. `trade` ⇒ implícito `home` |
| `location` | Point | — | sí | Punto exacto. **PRIVADO**: nunca sale en una respuesta si `attention = home` |
| `publicZone.label` | string | **60** | sí | Zona aproximada (`areaLabel`) |
| `publicZone.point` | Point | — | sí | Punto difuminado a ≥ **500 m** del real |
| `radiusKm` | int32 | **{0,2,5,10,20}** | sí | `0` = solo local; entonces aparece por distancia hasta `LOCAL_KM = 10` |
| `schedule.days` | array | **7** | no | Índice 0 = lunes. Cada día `{ open, close }` como `"HH:mm"` (string **5**) o ausente = cerrado. `close > open` |
| `schedule.urgent` | bool | — | no | "Atiendo urgencias" |
| `visit.mode` | string | enum | no | `hidden` (no publicar) \| `free` (sin costo) \| `paid` (con costo) |
| `visit.amountCents` | int32 | ≥ 0 | si `paid` | Costo de la visita |
| `prices[]` | array | **3** | no | `{ service: string 60, fromCents: int32 ≥ 0, unit: "from"\|"quote", suffix: string 20 }` |
| `payment[]` | array\<string> | **5** | no | `cash` \| `transfer` \| `card` \| `deposit` \| `other` |
| `responseTime` | string | enum | no | `sameDay` \| `h24` \| `h48` \| `week` |
| `warranty` | string | **120** | no | Garantía **declarada**, nunca presentada como verificada |
| `invoice` | bool | — | no | Solo `business` |
| `web` | string | **200** | no | URL `https://`; solo `business` |
| `photoUrl`, `coverUrl` | string | **200** | no | URL de CDN |
| `portfolio[]` | array | **6** | no | `{ url, thumbUrl, w: int32, h: int32 }` |
| `channels.chat` | bool | — | sí | Chat en Nexo (por defecto `true`) |
| `channels.call` | bool | — | no | Mostrar número para llamadas. **Requiere `phoneVerifiedAt`** |
| `jobs`, `recs` | int32 | ≥ 0 | sí | Contadores desnormalizados |
| `rating.avg` | double | **0–5** | sí | Un decimal |
| `rating.count` | int32 | ≥ 0 | sí | |
| `isNew` | bool | — | sí | Perfil con menos de 30 días |
| `updatedAt` | date | — | sí | Alimenta la insignia *Sin actualizar* (≥ 7 días) |

### Completitud del perfil (`completeness`)

`phone 15 · services 15 · zones 15 · schedule 15 · prices 15 · terms 10 · portfolio (≥3 fotos) 15` = 100.
Se calcula en el servidor y se devuelve como `int32 0–100`.

---

## 4. Contenido publicado

| Campo | Tipo BSON | Máximo | Requerido | Regla |
|---|---|---|---|---|
| `title` (trabajo o solicitud) | string | **90** | sí | |
| `text` (trabajo) | string | **1000** | sí | La UI lo recorta a 3 líneas |
| `description` (solicitud) | string | **600** | sí | Mínimo 10 caracteres |
| `problem`, `did`, `time` (trabajo) | string | **300** cada uno | no | Campos `pprob/pdid/ptime` de la maqueta |
| `images[]` | array | **6** | no | `{ url: 200, thumbUrl: 200, w: int32, h: int32 }` |
| `category` | string | **20** | sí | Slug de las 7 categorías |
| `zoneLabel` | string | **60** | sí | |
| `publicPoint` | Point | — | sí | Difuminado |
| `urgency` | string | enum | sí | `today` \| `week` \| `noRush` |
| `contact` | string | enum | sí | `chat` \| `chatCall` |
| `status` (solicitud) | string | enum | sí | `open` \| `assigned` \| `closed` |
| `interestCount` | int32 | ≥ 0 | sí | Desnormalizado |
| `tag` | string | **40** | no | Etiqueta libre del trabajo |

---

## 5. Pedidos, opiniones y mensajes

| Campo | Tipo BSON | Máximo | Requerido | Regla |
|---|---|---|---|---|
| `origin` | string | enum | sí | `post` (desde una solicitud) \| `direct` (desde el perfil o el chat) |
| `description` (pedido) | string | **600** | sí | Qué necesita |
| `attention` (pedido) | string | enum | sí | `home` \| `shop` |
| `placeZone` | string | **60** | sí | Zona que ve el prestador **antes** de aceptar |
| `address` (en claro) | string | **300** | si `home` | **Nunca se guarda en claro.** Se cifra a `addressEnc` |
| `addressEnc` | binData | **512 B** | — | Cifrado de sobre con llave de Key Vault. Se descifra solo si el pedido está `accepted`/`resolved` y lo pide el prestador de ese pedido |
| `addressNote` | string | **120** | no | Referencias ("portón verde") |
| `status` (pedido) | string | enum | sí | `pending` \| `accepted` \| `rejected` \| `cancelled` \| `resolved` |
| `agreedPrice.amountCents` | int32 | **0 – 99 999 900** | no | Máximo $999 999.00 MXN. **Informativo: no se cobra** |
| `agreedPrice.includes` | string | **200** | no | Qué incluye el precio |
| `cancelReason` | string | **200** | no | |
| `version` (order_events) | int32 | ≥ 1 | sí | Único por `streamId` |
| `type` (order_events) | string | **40** | sí | `created` \| `accepted` \| `rejected` \| `cancelled` \| `resolved` \| `addressChanged` |
| `prevHash`, `hash` | string | **64** | sí | SHA-256 hex; cadena de integridad del historial |
| `stars` | int32 | **1–5** | sí | |
| `review.text` | string | **500** | no | Moderado antes de guardar |
| `recommend` | bool | — | no | |
| `message.kind` | string | enum | sí | `text` \| `image` \| `system` |
| `message.text` | string | **1000** | si `text` | Moderado antes de guardar |
| `message.imageUrl` | string | **200** | si `image` | URL de CDN, ya aprobada |
| `lastPreview` | string | **120** | sí | Recorte del último mensaje para la bandeja |
| `blockedBy[]` | array\<uuid> | **2** | no | |
| `report.reason` | string | **300** | sí | |
| `report.targetType` | string | enum | sí | `provider` \| `work` \| `request` \| `conversation` \| `review` |
| `notification.title` | string | **80** | sí | |
| `notification.body` | string | **200** | sí | **Sin domicilio ni contenido sensible** (se lee en pantalla bloqueada) |
| `notification.deepLink` | string | **200** | no | |

---

## 6. Parámetros comunes de la API

| Parámetro | Tipo | Rango | Regla |
|---|---|---|---|
| `cursor` | string | **64** | Opaco (`afterId` codificado). El cliente no lo construye |
| `limit` | int32 | **1–50**, por defecto 20 | Un valor mayor se rechaza con `400 request.limit_exceeded` |
| `lat` | double | **−90 a 90** | |
| `lng` | double | **−180 a 180** | |
| `radiusKm` | int32 | **{2,5,10,20}** | Por defecto 10 |
| `q` (búsqueda) | string | **80** | Se normaliza sin acentos y en minúsculas |
| `sort` | string | enum | `relevance` \| `near` \| `recs` |
| `Idempotency-Key` | header string | **64** | Obligatorio en los POST que crean recursos. Se guarda en Redis 24 h |
| `If-None-Match` | header string | **64** | ETag; responde `304` si no cambió |

---

## 7. Límites de archivos

| Aspecto | Valor |
|---|---|
| Tamaño máximo por imagen | **5 MB** |
| Tipos aceptados | `image/jpeg`, `image/png`, `image/webp` |
| Dimensión máxima | 4096 × 4096 px (el worker rechaza más) |
| Miniatura generada | 400 × 400 px, WebP (SkiaSharp) |
| Imágenes por trabajo o portafolio | **6** |
| Imágenes por mensaje de chat | **1** |

Ver [`../14-imagenes-cdn/`](../14-imagenes-cdn/) para el flujo completo.
