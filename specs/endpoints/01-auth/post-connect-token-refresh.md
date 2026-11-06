# Renovar la sesión · `POST /connect/token` (grant `refresh_token`)

| Campo | Valor |
|---|---|
| Módulo | Identity (OpenIddict) |
| Quién | **C/Pr** (quien tiene refresh token) |
| Pantalla de la maqueta | Ninguna: es silencioso. Sostiene "sesión siempre viva" |
| Spec SDD | nv-01 |
| Idempotente | No — **cada uso rota el token** |

## 1. Función

Cambia un refresh token válido por un par nuevo. Lo invoca el interceptor de Dio al recibir un `401`,
sin que el usuario note nada.

**Rotación obligatoria**: el refresh usado se marca como consumido y se emite uno nuevo. Si llega un
refresh ya consumido, se asume robo y **se revoca toda la familia**.

## 2. Petición

`Content-Type: application/x-www-form-urlencoded`

```
grant_type=refresh_token
refresh_token=rt_8c1f…
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `grant_type` | string | — | sí | `refresh_token` |
| `refresh_token` | string | **512** | sí | Opaco. Lo valida OpenIddict contra su almacén |

## 3. Respuesta

`200 OK` — misma forma que el login, con `refresh_token` **nuevo**.

```json
{
  "access_token": "eyJhbGciOi…",
  "token_type": "Bearer",
  "expires_in": 600,
  "refresh_token": "rt_b94e…"
}
```

El `mode` y `has_provider` del nuevo access token se **releen de la base**, no se copian del token
anterior: así un cambio de modo o la creación del perfil profesional se reflejan en la siguiente
renovación.

**No se devuelve nunca**: el refresh anterior, ni datos de la cuenta.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 401 | `auth.refresh_reused` | El token ya fue rotado. **Se revoca la familia completa** y la app manda a login |
| 401 | `auth.token_expired` | Pasaron los 30 días |
| 401 | `auth.token_revoked` | Hubo logout o revocación administrativa |
| 403 | `account.pending_deletion` | |
| 429 | `rate.limit_exceeded` | Política `auth` |

## 5. Datos

| Colección | Operación |
|---|---|
| `openiddict.tokens` | Marca el token como `redeemed`, inserta el nuevo con el mismo `authorizationId` (la familia) |
| `openiddict.authorizations` | Lectura para validar la familia; al detectar reuso, revoca todos sus tokens |
| `accounts` | `findOne` para releer `currentMode`, `hasProvider` y `status` |

Al detectar reuso se escribe `audit_events: session.refresh_reuse_detected`
`{ accountId, tokenId, ip, userAgent }` y se emite `outbox: session.revoked` para que el worker
notifique al dueño ("se cerró tu sesión por seguridad").

Poda: un `IHostedService` de OpenIddict borra los tokens caducados a diario.

## 6. Caché y rate limit

- Sin caché (`no-store`).
- Rate limit **`auth`**: 20/min por IP. Suficiente: la app renueva como mucho cada 10 min.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **OpenIddict 7** | Rotación, detección de reuso y revocación de familia (`UseRollingRefreshTokens`) |
| **MongoDB** | `openiddict.*` |
| **Dio** (app) | Interceptor que renueva al recibir `401`, con cola para no disparar dos renovaciones a la vez |

## 8. Seguridad y privacidad

1. **Rotación con detección de reuso** es la defensa principal: un refresh robado sirve una sola vez
   y su uso delata el robo, que corta la sesión legítima y la ilegítima.
2. Los permisos se **releen de la base** en cada renovación: el token no es la fuente de verdad.
3. La renovación **no extiende indefinidamente** la ventana: la familia caduca a los 30 días del
   primer inicio de sesión.
4. El refresh token no se escribe nunca en logs, ni completo ni truncado.
