# Mis guardados · `GET /v1/discovery/saved`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.12 Guardados, que se abre **desde Cuenta** · `V.saved` |
| Spec SDD | nv-10 |
| Idempotente | Sí (lectura) |

## 1. Función

La lista de guardados, filtrable por tipo. En la maqueta esta pantalla no está en la barra inferior:
se entra desde Cuenta, y agrupa prestadores, trabajos y solicitudes.

## 2. Petición

```http
GET /v1/discovery/saved?kind=provider&cursor=&limit=20
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `kind` | string | enum | no | `provider` \| `work` \| `request` \| `all`. Por defecto `all` |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "counts": { "provider": 4, "work": 12, "request": 1 },
  "items": [
    {
      "kind": "provider",
      "targetId": "019400d4-…",
      "savedAt": "2026-11-04T10:00:00Z",
      "available": true,
      "provider": {
        "providerId": "019400d4-…",
        "name": "Plomería del Centro",
        "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
        "categories": ["plomeria"],
        "zoneLabel": "Centro",
        "rating": { "avg": 4.7, "count": 13 },
        "status": { "open": false, "text": "Cerrado · abre mañana a las 8:00" }
      }
    },
    {
      "kind": "work",
      "targetId": "019400f6-…",
      "savedAt": "2026-11-03T14:20:00Z",
      "available": false,
      "unavailableReason": "deleted"
    }
  ],
  "nextCursor": null,
  "hasMore": false
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `available` | bool | — | `false` si el objetivo se borró o despublicó |
| `unavailableReason` | string | enum | `deleted` \| `unpublished` \| `closed` (solicitud cerrada) |
| `provider` / `work` / `request` | object | — | Solo uno, según `kind`. Ausente si `available: false` |

Se devuelven las entradas no disponibles con `available: false` **en vez de ocultarlas**: así la app
puede mostrar "Este trabajo ya no está disponible" con un botón para quitarlo, en lugar de que la
lista cambie de tamaño sin explicación.

**No se devuelve nunca**: teléfono, punto exacto de prestadores a domicilio, ni la dirección de las
solicitudes guardadas.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `kind` fuera del enum |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `saved` | `find({ accountId: sub, kind? })` orden `_id: -1` | **`{accountId, createdAt: -1}`** |
| `provider_profiles`, `works`, `service_requests` | **Una consulta por tipo** con `$in` de los ids de la página | `_id` |

Nunca una consulta por elemento: como máximo tres consultas con `$in`, una por tipo presente en la
página. El patrón N+1 en una lista de 50 guardados serían 50 viajes a la base.

`counts` sale de un `$facet` por `kind`.

Las entradas cuyo objetivo no aparece en el resultado del `$in` se marcan `available: false`.

`readPreference: secondaryPreferred`.

## 6. Caché y rate limit

- **L1 60 s**, clave `saved:{accountId}:{kind}:{cursor}`. Sin L2: es una lista personal y la
  compartición entre usuarios es nula. `Cache-Control: private, no-store`.
- Invalida al guardar o quitar, y con los tags `provider:{id}` de los objetivos.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `saved` por cuenta; `$in` por tipo; `$facet` para los conteos |
| **HybridCache** (L1) | Caché corta en memoria |
| **Front Door CDN** | Fotos de los objetivos |

## 8. Seguridad y privacidad

1. `accountId: sub` en el filtro: **solo los propios guardados**, sin parámetro de usuario.
2. De cada objetivo se devuelve **lo público**: esta pantalla no es una vía lateral para obtener el
   teléfono o la ubicación exacta de un prestador guardado.
3. Las solicitudes guardadas se muestran con zona, nunca con la dirección.
4. Las entradas huérfanas se marcan en vez de desaparecer: es más honesto y permite limpiarlas.
5. Lista estrictamente personal: no se expone a nadie más, ni en agregado.
