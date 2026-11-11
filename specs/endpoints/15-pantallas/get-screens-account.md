# Pantalla de cuenta · `GET /v1/screens/account`

| Campo | Valor |
|---|---|
| Módulo | Screens |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.13 Cuenta · `V.account` |
| Spec SDD | nv-14 |
| Idempotente | Sí (lectura) |

## 1. Función

Compone la pestaña Cuenta: datos de la cuenta, modo activo, estadísticas, conteo de guardados, estado
del perfil profesional y los enlaces de privacidad, términos, sesión y eliminación.

Es también la pantalla desde la que se abre **Guardados**, que en la maqueta no está en la barra
inferior.

## 2. Petición

```http
GET /v1/screens/account
Authorization: Bearer <access_token>
```

Sin parámetros.

## 3. Respuesta

`200 OK`

```json
{
  "partial": false,
  "account": {
    "accountId": "019400a1-…",
    "email": "ana.lopez@example.com",
    "name": "Ana López",
    "photoUrl": null,
    "phone": "+527715550100",
    "emailVerified": true,
    "status": "active",
    "deletionRequestedAt": null,
    "memberSince": "2026-11-02T18:41:02Z"
  },
  "mode": { "current": "client", "canSwitch": true, "hasProvider": true },
  "provider": {
    "providerId": "019400d4-…",
    "name": "Plomería del Centro",
    "completeness": 45,
    "nextTodo": { "key": "works", "label": "Publica un trabajo", "points": 14 },
    "published": true
  },
  "stats": {
    "asClient": { "requests": 2, "orders": 3, "reviewsGiven": 1 },
    "asProvider": { "views": 128, "contactClicks": 9, "interestsGiven": 14, "ordersResolved": 12, "rating": { "avg": 4.7, "count": 13 } }
  },
  "savedCounts": { "provider": 4, "work": 12, "request": 1, "total": 17 },
  "search": { "zoneLabel": "Centro", "radiusKm": 10 },
  "links": {
    "privacy": "https://nexolocal.mx/privacidad",
    "terms": "https://nexolocal.mx/terminos",
    "support": "https://nexolocal.mx/ayuda"
  },
  "notifications": { "enabled": true, "deviceCount": 1 }
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `account.email` / `.phone` | string | 254 / 13 | **Los propios**, completos |
| `mode.canSwitch` | bool | — | `true` siempre: sin perfil, cambiar abre el alta |
| `provider` | object \| null | — | `null` si no tiene perfil profesional |
| `stats.asProvider.interestsGiven` | int32 | — | **"Con tu interés"**: solicitudes en las que mostró interés |
| `stats.asProvider.ordersResolved` | int32 | — | Actividad propia. **No es "ventas verificadas"** |
| `search` | object | — | Zona y radio guardados, para el selector |

**No se devuelve nunca**: datos de otras personas, ni quién vio el perfil (solo el agregado).

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 401 | `auth.token_expired` · `auth.token_revoked` | |
| 404 | `resource.not_found` | La cuenta fue eliminada |

Si falla una sección que no sea `account`, llega como `null` con `partial: true`.

## 5. Datos

**Sin colecciones propias.** Composición en paralelo:

| Sección | Contrato | Módulo |
|---|---|---|
| `account`, `notifications.deviceCount` | `IAccountContract.GetMeAsync(sub)` | Identity |
| `provider`, `search` | `IProviderMeContract.GetAsync(sub)` | Discovery |
| `savedCounts` | `ISavedContract.GetCountsAsync(sub)` | Discovery |
| `stats.asClient` | `IClientStatsContract.GetAsync(sub)` | Engagement + Discovery |
| `stats.asProvider` | `IProviderStatsContract.GetAsync(providerId)` | Engagement + Discovery |

`account` es **obligatoria**. Las estadísticas son lo primero que degrada: sin ellas la pantalla
sigue siendo utilizable.

Los `links` son configuración del host (`appsettings`), no una llamada a nada.

## 6. Caché y rate limit

- **Sin caché.** Es íntegramente personal. `Cache-Control: private, no-store`.
- Las estadísticas se pueden servir de **L1 60 s** dentro de su propio contrato: un contador de
  visitas con un minuto de retraso no engaña a nadie.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| `Task.WhenAll` | Composición en paralelo |
| **Polly v8** | Timeout, circuit breaker y bulkhead por sección |
| **Redis** | Caché corta de las estadísticas; contador de visitas |
| Patrón **Facade** | Screens compone, no decide |

## 8. Seguridad y privacidad

1. Todo se resuelve con el `sub`: **no hay parámetro de cuenta**, así que no hay superficie de BOLA.
2. El correo y el celular propios se devuelven completos; la restricción de la constitución §III.6
   aplica a los datos **de otra persona**.
3. Las estadísticas son **agregados**: `views` y `contactClicks` no dicen quién vio ni quién copió el
   número. Lo individual queda en `contact_log`, que no tiene endpoint de lectura.
4. `ordersResolved` es actividad declarada en la plataforma y la UI no debe presentarla como ventas
   verificadas (documento oficial, experiencia del prestador).
5. Los enlaces de **privacidad, términos y eliminación** están en esta respuesta porque la
   constitución del front §IV.3 exige rutas claras hacia ellos.
6. `deletionRequestedAt` permite mostrar los días restantes y ofrecer cancelar la baja (ARCO).
