# Cerrar sesión · `POST /v1/identity/logout`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.13 Cuenta → "Cerrar sesión" · `ACT.logout` |
| Spec SDD | nv-01 |
| Idempotente | Sí — cerrar una sesión ya cerrada responde `204` |

## 1. Función

Cierra la sesión de **este dispositivo**: revoca la familia del refresh token, mete el `jti` del
access token en la lista negra y da de baja el token FCM para que no sigan llegando notificaciones.

La app complementa en local: `SecureStorage.deleteAll()` + borrado de las tablas de sesión en drift
(constitución del front §III.2).

## 2. Petición

```http
POST /v1/identity/logout
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "refreshToken": "rt_8c1f…", "fcmToken": "d9A…", "allDevices": false }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `refreshToken` | string | **512** | no | Si se envía, se revoca esa familia. Si falta, se revocan todas las del `sub` |
| `fcmToken` | string | **4096** | no | Se borra el dispositivo correspondiente |
| `allDevices` | bool | — | no | Por defecto `false`. `true` = cerrar en todos los dispositivos |

## 3. Respuesta

`204 No Content` — sin cuerpo.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 401 | `auth.token_expired` | El access token ya caducó. La app igualmente borra lo local |
| 429 | `rate.limit_exceeded` | Política `auth` |

**No falla** si el refresh ya estaba revocado o el `fcmToken` no existe: cerrar sesión siempre debe
poder completarse.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `openiddict.tokens` | Revoca los tokens de la familia (o de todas, con `allDevices`) | `authorizationId` |
| `revoked_tokens` | `insertOne { _id: jti, accountId, expiresAt }` | TTL en `expiresAt` (= `exp` del access token, 10 min) |
| `devices` | `deleteOne({ fcmToken, accountId })` o `deleteMany({ accountId })` con `allDevices` | `fcmToken` único, `accountId` |

Redis: `SET nexo:jti:{jti}` con TTL igual a la vida restante del access token — es la lista que
consulta el middleware de autenticación, mientras `revoked_tokens` queda como respaldo auditable.

`audit_events`: `session.logout` `{ accountId, allDevices, deviceCount }`.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`auth`**.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **OpenIddict** | Revocación de la familia |
| **Redis** | Lista negra de `jti`, consultada en cada petición autenticada |
| **MongoDB** | `revoked_tokens` (TTL) y `devices` |

## 8. Seguridad y privacidad

1. El access token sigue siendo válido criptográficamente hasta su `exp`; por eso **la lista negra de
   `jti` es imprescindible**, si no, el logout no surtiría efecto por hasta 10 minutos.
2. La lista negra vive en Redis con TTL = vida restante del token: no crece sin control.
3. El borrado del token FCM evita el caso incómodo de recibir notificaciones de una cuenta de la que
   ya se salió — más aún si el teléfono es compartido.
4. `allDevices` es la herramienta de respuesta ante un robo de credenciales.
5. Nunca se responde error por un token ya revocado: dificultaría cerrar sesión, que es una acción de
   seguridad y debe ser siempre posible.
