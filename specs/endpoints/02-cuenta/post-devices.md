# Registrar dispositivo para push · `POST /v1/identity/devices`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Permiso de notificaciones (pantalla previa propia) |
| Spec SDD | nv-04 |
| Idempotente | Sí — el mismo `fcmToken` actualiza en vez de duplicar (`upsert`) |

## 1. Función

Asocia el token FCM del teléfono a la cuenta para poder enviar notificaciones push de mensajes,
interés, pedidos y resolución. La app lo llama al conceder el permiso y cada vez que Firebase rota el
token.

## 2. Petición

```http
POST /v1/identity/devices
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "fcmToken": "d9A3f…", "platform": "android", "appVersion": "1.4.2" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `fcmToken` | string | **4096** | sí | Token de FCM. Índice **único** |
| `platform` | string | enum | sí | `android` (único valor por ahora) |
| `appVersion` | string | **20** | sí | SemVer |

## 3. Respuesta

`201 Created` (nuevo) o `200 OK` (actualizado)

```json
{ "deviceId": "019400c3-8d4f-7a12-b7e5-1f2a3b4c5d60", "lastSeenAt": "2026-11-05T16:30:00Z" }
```

**No se devuelve nunca**: el `fcmToken` completo ni la lista de otros dispositivos de la cuenta.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Token vacío, `platform` fuera del enum o `appVersion` no SemVer |
| 409 | `device.token_owned_by_other_account` | El token pertenece a otra cuenta: se **reasigna** y se responde `200` (el teléfono cambió de usuario). Solo es error si la otra cuenta está activa y el token se usó hace menos de 1 min |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `devices` | `updateOne({ fcmToken }, { $set: { accountId, platform, appVersion, lastSeenAt } }, { upsert: true })` | `fcmToken` **único**; `accountId` |

TTL: `lastSeenAt` + **90 días** — un teléfono que no aparece en tres meses deja de recibir push y se
limpia solo.

`audit_events`: solo si el token se reasignó de otra cuenta (`device.reassigned`).

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `devices` con índice único y TTL |
| **FirebaseAdmin** | El worker usa estos tokens para enviar push |

## 8. Seguridad y privacidad

1. El `upsert` por `fcmToken` evita acumular dispositivos muertos y resuelve la reasignación cuando
   alguien más inicia sesión en el mismo teléfono.
2. Nunca se devuelve la lista de dispositivos de la cuenta en este endpoint: no hay razón para que la
   app la necesite y es metadato de ubicación/uso.
3. El TTL de 90 días es también una medida de minimización de datos (LFPDPPP).
4. Las notificaciones que se envíen a estos tokens **no llevan domicilio ni contenido sensible**:
   se leen en la pantalla bloqueada (constitución §7).
