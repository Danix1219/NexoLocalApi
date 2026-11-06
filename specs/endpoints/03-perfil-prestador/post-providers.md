# Crear perfil profesional · `POST /v1/discovery/providers`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C** (autenticado, sin perfil aún) |
| Pantalla de la maqueta | 7.3 Registro flujo `pro`: pasos `type → basic → services → zones → ready` · `ACT.obnext`, `ACT.startpro` |
| Spec SDD | nv-02 |
| Idempotente | Sí — `Idempotency-Key` obligatorio |

## 1. Función

Crea el perfil de prestador y habilita el **Modo Proveedor**. Recoge en una sola llamada lo que la
maqueta reparte en los pasos obligatorios del registro (`type`, `basic`, `services`, `zones`) y deja
los opcionales (`hours`, `prices`, `terms`, `portfolio`) para
[`PATCH /me/{seccion}`](patch-providers-me-seccion.md).

Reglas de la maqueta:

- Dos tipos: **Persona con oficio** (`trade`) y **Negocio de servicios** (`business`).
- En `business` las **categorías se infieren** de `activity` + `bio` (`inferCats`); si nada coincide
  se asigna `otros` y la actividad se usa como nombre del giro.
- `bio` exige **mínimo 20 caracteres** (`obValid`).
- La ubicación se marca en el mapa con radio **2/5/10/20 km**; un negocio **solo-local** va con
  `radiusKm = 0` y aparece por distancia hasta `LOCAL_KM = 10`.
- El atajo **"Publicar con lo básico"** (`ACT.obbasic`) usa exactamente este endpoint y nada más.

## 2. Petición

```http
POST /v1/discovery/providers
Authorization: Bearer <access_token>
Idempotency-Key: 8a1c3e5f-7b92-4d06-a1f3-2c4e6f80b1d3
Content-Type: application/json
```

```json
{
  "kind": "business",
  "name": "Plomería del Centro",
  "owner": "Carlos Ramírez",
  "sector": "servicios-hogar",
  "activity": "Reparación de fugas y tuberías",
  "bio": "Atendemos fugas, cambio de tuberías e instalación de calentadores en toda la zona centro.",
  "experience": 8,
  "crew": "team",
  "teamSize": 3,
  "attention": "both",
  "location": { "lat": 20.0539, "lng": -99.3403 },
  "radiusKm": 10,
  "specialties": ["fugas", "calentadores"],
  "photoUploadId": "019400b2-1c3e-7f21-9b84-3d5e6f70a1b2",
  "coverUploadId": null,
  "acceptedRules": true
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `kind` | string | enum | sí | `trade` \| `business` |
| `name` | string | **80** | sí | Nombre del perfil o comercial. **Moderado** |
| `owner` | string | **80** | solo `business` | Responsable |
| `sector` | string | **80** | solo `business` | Slug de `SECT` |
| `activity` | string | **80** | solo `business` | Alimenta `inferCats`. **Moderada** |
| `trades[]` | array\<string> | **5** | solo `trade` | Oficios declarados |
| `bio` | string | **20–600** | sí | **Moderada** |
| `experience` | int32 | **0–70** | no | Años |
| `crew` | string | enum | no | `alone` \| `withHelpers` \| `team` |
| `teamSize` | int32 | **1–200** | no | Solo `business` con `crew = team` |
| `attention` | string | enum | sí | `home` \| `shop` \| `both`. En `trade` se fuerza a `home` |
| `location.lat` / `.lng` | double | −90..90 / −180..180 | sí | Punto exacto, **privado** |
| `radiusKm` | int32 | **{0,2,5,10,20}** | sí | `0` solo si `attention = shop` |
| `categories[]` | array\<string> | **3** | sí en `trade` | En `business` se **ignora** lo que mande el cliente y se infiere en el servidor |
| `specialties[]` | array\<string> | **5** | no | Deben pertenecer a las categorías resultantes |
| `photoUploadId`, `coverUploadId` | uuid | — | no | Subidas ya aprobadas |
| `acceptedRules` | bool | — | sí | Debe ser `true` (paso `ready` de la maqueta) |

## 3. Respuesta

`201 Created` · `Location: /v1/discovery/providers/{id}`

```json
{
  "providerId": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01",
  "kind": "business",
  "name": "Plomería del Centro",
  "categories": ["plomeria"],
  "inferredCategories": true,
  "attention": "both",
  "publicZone": { "label": "Centro", "point": { "lat": 20.0536, "lng": -99.3398 } },
  "radiusKm": 10,
  "completeness": 45,
  "isNew": true,
  "phoneVerified": false,
  "nextOptionalSteps": ["hours", "prices", "terms", "portfolio"]
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `categories` | array | Las resultantes; si se infirieron, `inferredCategories: true` |
| `publicZone.point` | Point | **Difuminado ≥ 500 m** del real |
| `completeness` | int32 | 0–100 según la fórmula de [tipos y límites](../00-convenciones/tipos-y-limites.md) |
| `nextOptionalSteps` | array | Lo que la maqueta ofrece en "Agregar más detalles (opcional)" |

**No se devuelve nunca**: `location` exacto (ni al propio dueño en este DTO; para eso está
[`GET /providers/me`](get-providers-me.md)), ni el teléfono.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `provider.already_exists` | La cuenta ya tiene perfil (índice único `accountId`) |
| 400 | `provider.bio_too_short` | Menos de 20 caracteres |
| 400 | `provider.categories_required` | `trade` sin categorías, o `business` donde `inferCats` no resolvió nada **y** no hay actividad utilizable |
| 400 | `provider.radius_invalid` | Fuera de {0,2,5,10,20}, o `0` con `attention ≠ shop` |
| 400 | `validation_failed` | `acceptedRules` en `false`, campos de `business` en un `trade`, etc. |
| 403 | `content.inappropriate` | Content Safety rechazó `name`, `activity` o `bio` |
| 404 | `upload.not_found` · 409 `upload.not_pending` | Subidas inválidas |
| 403 | `account.pending_deletion` | |

## 5. Datos

**`nexo_discovery`**

| Colección | Operación | Índice |
|---|---|---|
| `provider_profiles` | `insertOne` | `accountId` **único**; `{location: "2dsphere", categories: 1}`; `{updatedAt: -1}`; Atlas Search `providers_search` |
| `uploads` | `findOne({ _id, ownerId: sub, status: "approved" })` por cada imagen | `{ownerId, createdAt}` |

Campos derivados que **calcula el servidor**, no el cliente:

| Campo | Cómo |
|---|---|
| `publicZone.label` | `areaLabel(location)`: zona más cercana, o "Cerca de X" si está a más de 1.2 km |
| `publicZone.point` | Desplazamiento aleatorio de 500–800 m sobre `location` |
| `categories` | `inferCats(activity + bio)` en `business` |
| `phoneVerified` | Copia de `accounts.phoneVerifiedAt` (llega por el evento `account.phone_verified`) |
| `isNew` | `true` los primeros 30 días |
| `rating` | `{ avg: 0, count: 0 }` |
| `jobs`, `recs` | 0 |
| `channels` | `{ chat: true, call: false }` — `call` solo se activa con celular confirmado |

Escrituras derivadas:

- `outbox`: **`provider.created`** `{ providerId, accountId, categories, publicZone }` → el worker
  pone `hasProvider: true` en `accounts` (Identity) e invalida los tags de búsqueda de la zona.
- Invalida `feed:{geohash6}` y `map:providers:*` de la celda.

Validator (`$jsonSchema`): `bio` `minLength 20` / `maxLength 600`, `name` `maxLength 80`,
`categories` `maxItems 3`, `specialties` `maxItems 5`, `prices` `maxItems 3`,
`portfolio` `maxItems 6`, `radiusKm` en el enum, `attention` en el enum.

## 6. Caché y rate limit

- Invalida `provider:{id}`, `feed:{geohash6}`, `map:providers:*`, `search:*` de la zona.
- Rate limit **`user`**. No hace falta `publish`: el índice único ya limita a uno por cuenta.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure AI Content Safety** | Moderación de `name`, `activity` y `bio` **antes** de insertar (síncrona) |
| **MongoDB** `2dsphere` | Índice geoespacial para `serves()` |
| **Atlas Search** | Índice `providers_search` con `autocomplete` sobre `name`, `bio`, `specialties`, `activity` |
| **Front Door CDN** | `photoUrl` y `coverUrl` |
| **Redis** | Idempotencia e invalidación por tags |
| Outbox → Storage Queue | `hasProvider` en Identity |

## 8. Seguridad y privacidad

1. **`location` exacto es privado.** Se guarda porque `serves()` lo necesita para calcular distancias,
   pero la proyección de salida lo excluye siempre que `attention = home`. Solo un negocio con local
   publica su punto, y lo hace a propósito.
2. `publicZone.point` se difumina **en el servidor** a 500–800 m: si lo hiciera la app, bastaría
   interceptar la petición para conocer el punto real.
3. Las categorías de un negocio se infieren **en el servidor**: si se confiara en el cliente,
   cualquiera se colocaría en todas las categorías.
4. `channels.call` nace en `false` y solo puede activarse con el celular confirmado: así la insignia
   y el canal no se pueden falsear.
5. El `409` por índice único cierra la carrera de dos creaciones simultáneas.
6. La moderación es **síncrona en el handler**, no diferida: el texto no llega a la base sin revisar
   (constitución §III.5).
