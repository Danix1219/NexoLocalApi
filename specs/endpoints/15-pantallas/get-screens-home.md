# Pantalla de inicio · `GET /v1/screens/home`

| Campo | Valor |
|---|---|
| Módulo | Screens |
| Quién | **P/C/Pr** |
| Pantalla de la maqueta | 7.4 Inicio — solicitante y 7.5 Inicio — prestador · `V.home`, `S.htab` |
| Spec SDD | nv-14 |
| Idempotente | Sí (lectura) |

## 1. Función

Compone en **una sola petición** todo lo que necesita la pantalla de inicio. El módulo Screens no
tiene base de datos: llama en paralelo (`Task.WhenAll`) a los contratos de los otros módulos y
devuelve el resultado con `partial: true` si alguna parte falló.

Sin esto, la pantalla de inicio del cliente serían cuatro peticiones en serie desde un teléfono con
red móvil.

## 2. Petición

```http
GET /v1/screens/home?mode=client&lat=20.0539&lng=-99.3403
Authorization: Bearer <access_token>      # opcional (visitante)
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `mode` | string | enum | no | `client` \| `provider`. Por defecto, el del token |
| `lat` / `lng` | double | −90..90 / −180..180 | sí | Punto de referencia |

## 3. Respuesta

`200 OK` — **la forma depende del modo**.

### Modo Cliente

```json
{
  "mode": "client",
  "zoneLabel": "Centro",
  "partial": false,
  "feed": { "items": [ … ], "nextCursor": "…", "hasMore": true },
  "myRequests": {
    "items": [
      { "requestId": "01940107-…", "title": "Fuga en el baño", "status": "open",
        "interestCount": 3, "summaryText": "3 interesados · Compara y elige a quién pedirle",
        "interestedPreview": [ … ] }
    ],
    "total": 2
  },
  "myOrders": {
    "items": [ { "orderId": "01940208-…", "status": "accepted", "statusText": "Aceptado",
                 "providerName": "Plomería del Centro" } ],
    "total": 1
  },
  "unreadMessages": 3
}
```

### Modo Proveedor

```json
{
  "mode": "provider",
  "zoneLabel": "Centro",
  "partial": false,
  "ordersToAnswer": { "items": [ … ], "total": 1 },
  "acceptedOrders": { "items": [ … ], "total": 2 },
  "opportunities": { "items": [ … ], "total": 14 },
  "community": { "items": [ … ], "hasMore": true },
  "profileTodo": { "completeness": 45, "next": { "key": "works", "label": "Publica un trabajo" } },
  "unreadMessages": 1
}
```

### Visitante

```json
{ "mode": "guest", "zoneLabel": "Centro", "partial": false,
  "feed": { "items": [ … ], "hasMore": true, "limited": true },
  "tour": { "steps": 4 } }
```

| Campo | Tipo | Regla |
|---|---|---|
| `partial` | bool | **`true` si alguna sección falló**; la que falló llega como `null` |
| `limited` | bool | Feed recortado del visitante |
| `profileTodo.next` | object | La siguiente acción de la tarjeta "Completa tu perfil" |

**No se devuelve nunca**: direcciones, teléfonos ni nombres completos antes de un pedido aceptado.
Cada sección aplica las exclusiones de su endpoint de origen.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Faltan `lat`/`lng` |
| 403 | `provider.profile_required` | `mode=provider` sin perfil |
| 401 | `auth.token_expired` | |

**No se devuelve `503` si falla una sección**: se responde `200` con `partial: true` y esa parte en
`null`. Una pantalla de inicio a medias es mejor que ninguna.

## 5. Datos

**Sin colecciones propias.** Composición en paralelo:

| Sección | Contrato | Módulo |
|---|---|---|
| `feed` | `IFeedContract.GetFeedAsync(point, limit)` | Discovery |
| `myRequests` | `IMyRequestsContract.GetAsync(sub)` | Discovery |
| `myOrders` / `ordersToAnswer` / `acceptedOrders` | `IOrdersContract.GetByRoleAsync(sub, role, status)` | Engagement |
| `opportunities` | `IOpportunitiesContract.GetAsync(providerId, point)` | Discovery |
| `unreadMessages` | `IUnreadContract.GetTotalAsync(sub)` | Engagement |
| `profileTodo` | `IProviderCompletenessContract.GetAsync(providerId)` | Discovery |

```csharp
var tasks = new[] { feedTask, requestsTask, ordersTask, unreadTask };
await Task.WhenAll(tasks.Select(t => t.WithTimeout(2_000).Fallback(null)));
```

Cada llamada: **timeout 2 s**, circuit breaker y **bulkhead** propio, de modo que una sección lenta
no agota el pool de conexiones de las demás.

## 6. Caché y rate limit

- **Sin caché**: la pantalla es personal (mis solicitudes, mis pedidos, mis no leídos).
  `Cache-Control: private, no-store`.
- Las **secciones públicas** (`feed`, `community`) se sirven desde la caché L2 de su propio endpoint,
  así que la composición aprovecha esa caché sin cachear el conjunto.
- Para el **visitante** sí hay caché: `L2 30 s` con clave `screen:home:guest:{geohash6}`, porque no
  hay nada personal.
- Rate limit **`user`**, o **`public`** sin token.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| `Task.WhenAll` | Composición en paralelo |
| **Polly v8** | Timeout, circuit breaker y bulkhead por sección |
| **Redis** | Caché de las secciones públicas y de la variante de visitante |
| Patrón **Facade** | Screens es una fachada: no tiene lógica de negocio ni base propia |

## 8. Seguridad y privacidad

1. Screens **no consulta ninguna base de datos**: todo pasa por los contratos de los otros módulos,
   con sus reglas de privacidad y de propiedad ya aplicadas. Es lo que evita que una pantalla de
   composición se convierta en una puerta trasera que salta las comprobaciones.
2. Cada sección se resuelve con el `sub` del token, nunca con un id recibido por parámetro.
3. La degradación con `partial: true` **no afecta a la seguridad**: si una sección falla, falta; nunca
   se sustituye por datos de otro usuario ni se rellena con algo genérico.
4. La variante de visitante se cachea **porque no contiene nada personal**; la del usuario
   autenticado, nunca.
5. `mode` es una pista de presentación: el acceso a las secciones de prestador exige perfil, y lo
   comprueba el servidor.
