# Iniciar sesión · `POST /connect/token` (grant `password`)

| Campo | Valor |
|---|---|
| Módulo | Identity (OpenIddict) |
| Quién | **P** (público) |
| Pantalla de la maqueta | 7.2 Autenticación, vista `login` · `ACT.aunext` → `doLogin` |
| Spec SDD | nv-01 |
| Idempotente | No |

> **Cambio de tecnología**: el usuario es el **correo**, no el celular.
> Los botones de "cuentas de prueba" (`ACT.audemo`) de la maqueta **no se publican** como capacidad
> de usuario final (constitución §9).

## 1. Función

Autentica con correo y contraseña y emite el par de tokens. Al iniciar sesión se **restaura el último
modo usado** (`acct.mode` en la maqueta) y la ubicación de búsqueda guardada.

Regla de la maqueta: **bloqueo a los 5 intentos fallidos**.

## 2. Petición

`Content-Type: application/x-www-form-urlencoded` (formato OAuth 2.0, lo exige OpenIddict)

```
grant_type=password
username=ana.lopez@example.com
password=Nexo2026ok
scope=openid offline_access nexo_api
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `grant_type` | string | — | sí | `password` |
| `username` | string | **254** | sí | El **correo**, normalizado a minúsculas |
| `password` | string | **8–64** | sí | Nunca se registra en logs |
| `scope` | string | **120** | sí | Debe incluir `offline_access` para recibir refresh token |

**No se envía `client_id`.** El servidor acepta clientes anónimos (`AcceptAnonymousClients`). Mandar
cualquier `client_id` responde **401 `invalid_client`**, porque no hay ninguno registrado.

No se registra un cliente `nexo-mobile` a propósito: una app móvil es un cliente público, no puede
guardar un secreto, y su `client_id` viaja a la vista de cualquiera que mire el tráfico. Registrarlo
no añadiría seguridad y sí un modo de fallo nuevo: toda base nueva tendría que sembrarlo antes de
que nadie pudiera iniciar sesión.

## 3. Respuesta

`200 OK`

```json
{
  "access_token": "eyJhbGciOi…",
  "token_type": "Bearer",
  "expires_in": 600,
  "refresh_token": "rt_8c1f…",
  "scope": "openid offline_access nexo_api"
}
```

Claims del access token:

| Claim | Tipo | Regla |
|---|---|---|
| `sub` | uuid v7 | `accountId` |
| `mode` | string | `client` \| `provider` — el último usado |
| `has_provider` | bool | Si tiene perfil profesional |
| `jti` | uuid | Para la lista negra en logout |
| `exp` | int64 | 10 min |

**No se devuelve nunca**: `passwordHash`, el correo en el cuerpo, ni datos de perfil. El `mode` del
token es informativo; **la autorización real se verifica por recurso en cada petición**.

## 4. Errores

Formato OAuth (`error`, `error_description`) además del `code` de Nexo:

| HTTP | `code` | Cuándo |
|---|---|---|
| 401 | `auth.invalid_credentials` | Correo inexistente **o** contraseña incorrecta — **mensaje único** |
| 423 | `auth.account_locked` | 5 fallos consecutivos. `Retry-After` con los segundos restantes |
| 403 | `auth.email_not_verified` | |
| 403 | `account.pending_deletion` | Cuenta en los 30 días de baja. El `detail` explica cómo cancelarla |
| 400 | `validation_failed` | Falta `grant_type` o `scope` inválido |
| 429 | `rate.limit_exceeded` | Política `auth`: 20/min por IP |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `findOne({ email, status: { $ne: "deleted" } })` | `email` único |
| `openiddict.tokens` | Inserta el refresh token | los crea OpenIddict |

Redis:

| Clave | Operación |
|---|---|
| `nexo:lock:{emailHash}` | `INCR` al fallar; a los **5** se fija TTL 15 min y se responde `423`. Se borra al acertar |

Al acertar se actualiza `accounts.lastLoginAt` y se emite `outbox: session.started`
`{ accountId, ip, userAgent }` para `audit_events`.

**Siempre se verifica la contraseña**, incluso si el correo no existe: se compara contra un hash
señuelo para que el tiempo de respuesta no delate la existencia de la cuenta.

## 6. Caché y rate limit

- Sin caché (`Cache-Control: no-store`).
- Rate limit **`auth`**: 20/min por IP, en memoria por réplica.
- Bloqueo por intentos: aparte del rate limit, exacto en Redis.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **OpenIddict 7** + `OpenIddict.MongoDb` | Servidor OAuth 2.0 / OIDC y almacén de tokens |
| **Argon2id** | Verificación del hash |
| **Redis** | Contador de intentos fallidos |
| OpenTelemetry | Traza `auth.login` con resultado y `emailHash`, nunca el correo |

## 8. Seguridad y privacidad

1. **Un solo mensaje de error** para correo inexistente y contraseña incorrecta, y tiempo de
   respuesta equivalente en ambos casos (hash señuelo).
2. Bloqueo **por cuenta**, con TTL, no permanente: un atacante no puede dejar fuera a alguien de
   forma indefinida, y 5 intentos frenan la fuerza bruta.
3. El access token vive **10 min** para que un logout o un cambio de permisos surta efecto pronto.
4. `mode` y `has_provider` viajan en el token solo como pista para la app. Cada endpoint vuelve a
   comprobar la propiedad del recurso en la misma consulta (constitución §III.2).
5. `password` está excluido del logging estructurado y del volcado de errores.
