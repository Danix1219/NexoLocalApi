# Mi perfil profesional · `GET /v1/discovery/providers/me`

> Estado ejecutable del incremento 2026-11-09: [implementación, conciliaciones y límites](implementacion.md). La caché y propagación cloud descritas como objetivo abajo no están activadas. Las rutas usan no-store, Identity por contrato y eventos locales durables; no se acredita aceptación cloud.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **Pr** (autenticado, con perfil) |
| Pantalla de la maqueta | 7.11 Mi perfil / Cuenta → "Ver perfil" · `V.myprofile` |
| Spec SDD | nv-06 (función); nv-02 (contratos) |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve el perfil propio **completo**, incluidos los campos que nunca se muestran a terceros: el
punto exacto, el desglose de completitud y las estadísticas de visitas y clics de contacto.

Es la vista de edición y la que pinta la tarjeta "Completa tu perfil" de la maqueta
(`profileTodo`), que usa una fórmula distinta a la del registro.

## 2. Petición

```http
GET /v1/discovery/providers/me
Authorization: Bearer <access_token>
```

## 3. Respuesta

`200 OK`

```json
{
  "providerId": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01",
  "kind": "business",
  "name": "Plomería del Centro",
  "owner": "Carlos Ramírez",
  "sector": "servicios-hogar",
  "activity": "Reparación de fugas y tuberías",
  "categories": ["plomeria"],
  "specialties": ["fugas", "calentadores"],
  "bio": "Atendemos fugas…",
  "experience": 8,
  "crew": "team",
  "teamSize": 3,
  "attention": "both",
  "location": { "lat": 20.0539, "lng": -99.3403 },
  "publicZone": { "label": "Centro", "point": { "lat": 20.0536, "lng": -99.3398 } },
  "radiusKm": 10,
  "schedule": { "days": [{ "open": "08:00", "close": "18:00" }, null, null, null, null, null, null], "urgent": true },
  "visit": { "mode": "free" },
  "prices": [{ "service": "Cambio de llave", "fromCents": 35000, "unit": "from", "suffix": "" }],
  "payment": ["cash", "transfer"],
  "responseTime": "sameDay",
  "warranty": "30 días en mano de obra",
  "invoice": true,
  "web": "https://plomeriadelcentro.mx",
  "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
  "coverUrl": null,
  "portfolio": [],
  "channels": { "chat": true, "call": false },
  "emailVerified": true,
  "isNew": true,
  "jobs": 0,
  "recs": 0,
  "rating": { "avg": 0, "count": 0 },
  "completeness": 79,
  "policyVersion": "provider-completeness/1",
  "todo": [
    { "key": "photo", "label": "Agrega tu foto", "points": 7, "done": true },
    { "key": "schedule", "label": "Publica tu horario", "points": 14, "done": true },
    { "key": "prices", "label": "Agrega precios", "points": 14, "done": true },
    { "key": "works", "label": "Publica un trabajo", "points": 14, "done": false },
    { "key": "terms", "label": "Completa condiciones", "points": 9, "done": true },
    { "key": "cover", "label": "Agrega una portada", "points": 7, "done": false }
  ],
  "stats": { "views": 128, "contactClicks": 9, "interestsGiven": 14 },
  "updatedAt": "2026-11-05T12:00:00Z"
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `location` | Point | **Solo aquí**: es su propio punto exacto |
| `todo[]` | array | Fórmula `profileTodo` de la maqueta: base 35 % + foto 7, horario 14, precios 14, trabajos 14, condiciones 9, portada 7 |
| `stats.views` | int32 | Visitas al perfil (`S.stats.v`) |
| `stats.contactClicks` | int32 | Clics de contacto (`S.stats.c`) |
| `stats.interestsGiven` | int32 | **"Con tu interés"**: solicitudes en las que mostró interés. Lo aporta Engagement por contrato |

**No se devuelve nunca**: el celular (está en [`GET /identity/me`](../02-cuenta/get-me.md)), ni dato
alguno de otros prestadores.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `provider.not_found` | La cuenta no tiene perfil. La app abre el alta |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `provider_profiles` | `findOne({ accountId: sub })` | `accountId` **único** |

`stats.interestsGiven` llega por **interfaz de contrato** a Engagement
(`IInterestStatsContract`), con timeout de 2 s y circuit breaker. Si falla, el campo se devuelve
como `null` y la app lo oculta: una estadística no justifica romper la pantalla.

`stats.views` se incrementa en [`GET /providers/{id}`](get-providers-id.md), no aquí.

## 6. Caché y rate limit

- **L1 60 s**, clave `provme:{accountId}`. `Cache-Control: private, no-store`.
- Invalida con el tag `provider:{id}`.
- Rate limit **`user`**.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Lectura por `accountId` |
| **Polly v8** (`Http.Resilience`) | Timeout 2 s + circuit breaker en la llamada a Engagement |
| **HybridCache** (L1) | Caché corta |
| **Front Door CDN** | Imágenes |

## 8. Seguridad y privacidad

1. El perfil se resuelve por `accountId: sub`: **no hay id en la ruta**, así que no hay BOLA.
2. Es el **único** endpoint que devuelve `location` exacto de un prestador a domicilio, y solo a su
   dueño.
3. La llamada entre módulos es de **solo lectura por contrato**, con degradación elegante: Discovery
   nunca consulta la base de Engagement (constitución §II.1 y §II.6).
4. Las estadísticas son agregados; no se expone **quién** vio el perfil.
