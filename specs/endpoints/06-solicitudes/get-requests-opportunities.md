# Solicitudes para ti (oportunidades) · `GET /v1/discovery/requests/opportunities`

> Implementación local: rige el [refinamiento del incremento](implementacion.md) para cuota, búsqueda, contratos y capacidades pendientes. Los ejemplos cloud y de pedidos representan el objetivo posterior.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **Pr** (autenticado, con perfil) |
| Pantalla de la maqueta | 7.5 Inicio — prestador "Oportunidades" y 7.7 Buscar — prestador · `proList()` |
| Spec SDD | nv-09 (solicitudes; refinamiento local) |
| Idempotente | Sí (lectura) |

## 1. Función

Las solicitudes que le aparecen a un prestador: de **su rubro**, **dentro de su radio**, ordenadas
por urgencia. Es la pantalla que convierte una solicitud en trabajo.

Filtros de la maqueta (`proList`):

| Filtro | Por defecto | Qué hace |
|---|---|---|
| Dentro de mi radio | **activo** | Solo las que alcanza su cobertura |
| Coincide con mi servicio | activo | Solo sus categorías |
| Urgentes (hoy) | inactivo | Solo `urgency = today` |
| Categoría | — | Una categoría concreta |
| Texto | — | Busca en título y descripción |

La maqueta permite expresar interés **incluso en otra categoría** si la solicitud le es visible, por
eso `myServiceOnly` se puede desactivar.

## 2. Petición

```http
GET /v1/discovery/requests/opportunities?cat=plomeria&today=false&inRadius=true
    &myServiceOnly=true&q=fuga&cursor=&limit=20
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `cat` | string | **20** | no | Slug. Debe estar entre las categorías del perfil si `myServiceOnly` |
| `today` | bool | — | no | Solo urgencia `today` |
| `inRadius` | bool | — | no | Por defecto **`true`** |
| `myServiceOnly` | bool | — | no | Por defecto **`true`** |
| `q` | string | **80** | no | Texto sobre título y descripción, normalizado sin acentos |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "items": [
    {
      "requestId": "01940107-…",
      "category": "plomeria",
      "title": "Fuga en el baño",
      "descriptionExcerpt": "Hay una fuga debajo del lavabo…",
      "urgency": "today",
      "urgencyText": "Hoy",
      "zoneLabel": "Centro",
      "publicPoint": { "lat": 20.0536, "lng": -99.3398 },
      "distanceText": "a ~2 km",
      "imageUrl": "https://cdn…",
      "interestCount": 3,
      "ago": "hace 2 horas",
      "iAmInterested": false,
      "inMyRadius": true,
      "matchesMyService": true,
      "client": { "displayName": "A. L.", "photoUrl": null }
    }
  ],
  "nextCursor": "MDE5NDAxMDct…",
  "hasMore": true,
  "total": 14,
  "appliedFilters": ["inRadius", "myServiceOnly"]
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `publicPoint` | Point | — | **Difuminado**, siempre |
| `distanceText` | string | **30** | Aproximado (`kmTxt`) |
| `iAmInterested` | bool | — | Si este prestador ya expresó interés |
| `client.displayName` | string | Nombre de la cuenta | Nombre completo publicado por Identity para atribuir la solicitud |

### Lo que NO se devuelve nunca

| Campo | Por qué |
|---|---|
| `location` exacto | La dirección llega solo tras aceptar el pedido |
| Teléfono del cliente | Constitución §III.6 |
| Correo del cliente | Nunca |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 403 | `provider.profile_required` | La cuenta no tiene perfil de prestador |
| 400 | `validation_failed` | `cat` no es de su perfil con `myServiceOnly`, o enums inválidos |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 429 | `rate.limit_exceeded` | Política `user` |

## 5. Datos

```js
db.service_requests.aggregate([
  { $geoNear: { near: provider.location, distanceField: "distM",
                maxDistance: provider.radiusKm * 1000 || 10000, spherical: true,
                query: { status: "open", category: { $in: cats }, clientId: { $ne: sub } } } },
  { $sort: { urgencyRank: 1, _id: -1 } },
  { $project: { location: 0 } },     // el punto exacto no sale del servidor
  { $limit: limit }
])
```

| Colección | Índice |
|---|---|
| `service_requests` | **`{location: "2dsphere", category: 1, status: 1}`** |
| `interests` (Engagement) | `iAmInterested` por contrato, con `$in` de los ids de la página |

- `urgencyRank`: `today = 0`, `week = 1`, `noRush = 2`. Se guarda como campo calculado al insertar,
  para poder ordenar con el índice en vez de en memoria.
- `clientId: { $ne: sub }`: un prestador **no ve sus propias solicitudes** como oportunidad.
- `radiusKm = 0` (solo local) usa `LOCAL_KM = 10` km como alcance.
- `readPreference: secondaryPreferred`.

## 6. Caché y rate limit

- **L2 (Redis) 30 s** para la parte pública, clave
  `opps:{geohash6}:{radiusKm}:{catsHash}:{filtros}:{cursor}`.
- `iAmInterested` se resuelve por petición, fuera de la caché.
- Invalida con el tag `requests:{geohash6}` (solicitud creada o cerrada).
- Rate limit **`user`** (120/min).
- `Cache-Control: private, max-age=15`.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** `2dsphere` + `$geoNear` | Solicitudes dentro del radio con distancia |
| **Atlas Search** | Solo cuando viene `q`, sobre título y descripción |
| **Redis** | Caché L2 por celda y contadores |
| **Polly v8** | Timeout + circuit breaker al consultar `interests` en Engagement |

## 8. Seguridad y privacidad

1. **El punto exacto se excluye en la proyección**, no en el DTO. La distancia se da aproximada:
   con la distancia exacta desde varios puntos se podría trilaterar el domicilio del cliente.
2. El cliente aparece con el nombre público de su cuenta y su avatar actual, como autor de la solicitud.
   El correo, teléfono y ubicación exacta permanecen ocultos antes de aceptar el pedido.
3. Las categorías se toman **del perfil**, no del parámetro, cuando `myServiceOnly` está activo.
4. `clientId: { $ne: sub }` evita el caso absurdo de interesarse en la propia solicitud.
5. Las solicitudes `closed` y `assigned` desaparecen de aquí: respetan la decisión del cliente de
   dejar de recibir ofertas.

## Refinamiento de implementación local (2026-11-10)
Aplican las [decisiones, límites y evidencia del incremento](implementacion.md), incluida cuota transaccional Mongo, búsqueda normalizada sin Atlas Search, resumen por contratos y bloqueo de imágenes/reseñas sin infraestructura o pedido autorizado. Esta nota concilia los ejemplos históricos; no acredita despliegue.


Refinamiento 2026-11-10: la respuesta de lista incluye `description` completo (hasta 600 caracteres del contrato de creación) junto a `descriptionExcerpt` de hasta 200 para clientes antiguos. La app contrae la vista a tres líneas y ofrece Ver más/Ver menos. No cambia ubicación exacta ni datos privados.
