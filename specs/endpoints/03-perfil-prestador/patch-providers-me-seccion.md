# Editar una sección del perfil · `PATCH /v1/discovery/providers/me/{seccion}`

> Estado ejecutable del incremento 2026-11-09: [implementación, conciliaciones y límites](implementacion.md). La caché y propagación cloud descritas como objetivo abajo no están activadas. Las rutas usan no-store, Identity por contrato y eventos locales durables; no se acredita aceptación cloud.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **Pr** (autenticado, con perfil) |
| Pantalla de la maqueta | 7.3 pasos opcionales `hours`, `prices`, `terms`, `portfolio`; y 7.11 edición · `ACT.edsave` |
| Spec SDD | nv-06 (función); nv-02 (contratos) |
| Idempotente | Sí (mismo cuerpo, mismo resultado) |

## 1. Función

Un endpoint por **sección** del perfil, en vez de un `PATCH` monolítico: así cada pantalla de la
maqueta manda solo lo suyo, la validación es específica y el conflicto entre dos ediciones
simultáneas de secciones distintas no existe.

| `{seccion}` | Pantalla de la maqueta | Campos |
|---|---|---|
| `basic` | paso `basic` | `name`, `owner`, `experience`, `crew`, `teamSize`, `photoUploadId`, `coverUploadId` |
| `services` | paso `services` | `sector`, `activity`, `trades[]`, `specialties[]`, `bio` (recalcula categorías en `business`) |
| `zone` | paso `zones` | `location`, `radiusKm`, `attention` |
| `hours` | paso `hours` | `schedule.days[7]`, `schedule.urgent` |
| `prices` | paso `prices` | `visit`, `prices[]` |
| `terms` | paso `terms` | `channels`, `payment[]`, `responseTime`, `warranty`, `invoice`, `web` |
| `portfolio` | paso `portfolio` | `portfolioUploadIds[]` |

## 2. Petición

```http
PATCH /v1/discovery/providers/me/hours
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{
  "schedule": {
    "days": [
      { "open": "08:00", "close": "18:00" },
      { "open": "08:00", "close": "18:00" },
      null, null, null,
      { "open": "09:00", "close": "14:00" },
      null
    ],
    "urgent": true
  }
}
```

| Parámetro / campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `seccion` (ruta) | string | enum | sí | Una de las 7 de la tabla. Otra → `404` |
| `schedule.days` | array | **exactamente 7** | en `hours` | Índice 0 = lunes. `null` = cerrado. **Al menos un día abierto** |
| `schedule.days[].open` / `.close` | string | **5** | — | `"HH:mm"`, 24 h. **`close > open`** |
| `schedule.urgent` | bool | — | no | "Atiendo urgencias" |
| `visit.mode` | string | enum | en `prices` | `hidden` \| `free` \| `paid` |
| `visit.amountCents` | int32 | **0 – 99 999 900** | si `paid` | |
| `prices[]` | array | **máx. 3** | no | `{ service: 60, fromCents: int32, unit: "from"\|"quote", suffix: 20 }`. **Moderado** el `service` |
| `payment[]` | array\<string> | **5** | no | `cash`\|`transfer`\|`card`\|`deposit`\|`other` |
| `responseTime` | string | enum | no | `sameDay`\|`h24`\|`h48`\|`week` |
| `warranty` | string | **120** | no | **Moderada**. Declarada, nunca "verificada" |
| `web` | string | **200** | no | Debe empezar con `https://`. Solo `business` |
| `channels.call` | bool | — | no | `true` **solo si** hay correo confirmado, móvil declarado válido y consentimiento explícito |
| `portfolioUploadIds[]` | array\<uuid> | **6** | no | Subidas aprobadas y propias. El array **reemplaza** el portafolio completo |
| `location`, `radiusKm`, `attention` | ver [tipos](../00-convenciones/tipos-y-limites.md) | | en `zone` | Recalcula `publicZone` |
| `bio` | string | **20–600** | en `services` | **Moderada**. Recalcula categorías en `business` |

## 3. Respuesta

`200 OK` con el perfil completo, igual que [`GET /providers/me`](get-providers-me.md), para que la
app no tenga que pedirlo otra vez. Incluye el `completeness` recalculado.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | `seccion` desconocida |
| 404 | `provider.not_found` | La cuenta no tiene perfil |
| 400 | `provider.schedule_invalid` | No son 7 días, `close <= open`, o ningún día abierto |
| 400 | `provider.radius_invalid` | Fuera del enum, o `0` con `attention ≠ shop` |
| 400 | `provider.bio_too_short` | Menos de 20 caracteres |
| 400 | `provider.categories_required` | Tras recalcular no quedó ninguna categoría |
| 400 | `provider.prices_limit` | Más de 3 servicios con precio |
| 400 | `content.images_limit` | Más de 6 fotos de portafolio |
| 403 | `auth.email_not_verified` | Intentó activar `channels.call` sin correo confirmado |
| 400 | `phone.invalid` | Intentó activar `channels.call` sin móvil declarado válido |
| 403 | `content.inappropriate` | Content Safety rechazó `bio`, `warranty` o el nombre de un servicio |
| 404 | `upload.not_found` · 409 `upload.not_pending` | Subidas inválidas o ajenas |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `provider_profiles` | `updateOne({ accountId: sub }, { $set: { <solo los campos de la sección>, updatedAt } })` | `accountId` único |
| `uploads` | `find({ _id: { $in: ids }, ownerId: sub, status: "approved" })` — **propiedad y estado en la misma consulta** | `{ownerId, createdAt}` |

- Se escriben **solo** los campos de la sección: un `$set` parcial, nunca el documento completo, para
  no pisar lo que otra pantalla acaba de guardar.
- `updatedAt` siempre se refresca: alimenta la insignia *Sin actualizar* (≥ 7 días) y el orden por
  relevancia.
- En `zone` se recalculan `publicZone.label` y `publicZone.point` (difuminado 500–800 m).
- En `services` y `business` se recalculan las `categories` con `inferCats`.

Escrituras derivadas:

- `outbox`: **`provider.updated`** `{ providerId, section, categories?, publicZone? }` → invalida los
  tags `provider:{id}`, `search:*` y, si cambió la zona o las categorías, `map:providers:*` y
  `feed:{geohash6}` de las celdas vieja y nueva.
- Las fotos que salen del portafolio se **encolan para purga** del CDN.

## 6. Caché y rate limit

- Invalida `provider:{id}`, `provme:{accountId}` y lo que indique el evento.
- Rate limit **`user`** (120/min). Sin `publish`: editar el propio perfil no es publicar contenido
  nuevo en el feed.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure AI Content Safety** | Moderación de `bio`, `warranty` y nombres de servicio, antes de guardar |
| **MongoDB** | `$set` parcial por sección |
| **Front Door CDN** | URLs nuevas; purga por tag de las imágenes retiradas |
| **Redis** | Invalidación por tags |
| Outbox → Storage Queue | Propagación de la invalidación y de los cambios de zona |

## 8. Seguridad y privacidad

1. El recurso se resuelve por `accountId: sub`: **no se puede editar el perfil de otro**.
2. Las subidas se validan con `_id` + `ownerId` + `status` **en una sola consulta**: no se puede
   adjuntar la imagen de otra persona ni una que la moderación rechazó.
3. Activar `channels.call` requiere correo confirmado, móvil declarado y consentimiento explícito.
   El servidor calcula la insignia de correo; no afirma posesión del teléfono declarado.
4. `warranty`, `payment` y `responseTime` son **declarados**; la API y la UI nunca los presentan como
   comprobados por Nexo Local (constitución §IV.3).
5. Cambiar la zona recalcula el punto difuminado en el servidor; el cliente nunca decide el
   difuminado.
6. El `$set` acotado a la sección evita que una pantalla vieja de la app borre campos que no conoce.
