# Editar mi cuenta · `PATCH /v1/identity/me`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.13 Cuenta → editar datos personales · `ACT.edsave` |
| Spec SDD | nv-04 |
| Idempotente | Sí (mismo cuerpo, mismo resultado) |

## 1. Función

Cambia el **nombre** y la **foto** de la persona. No cambia el correo (eso va por verificación) ni la
contraseña (eso va por su propio flujo).

Los datos personales (`uname`) y los del perfil profesional (`name` del perfil) son **distintos**:
este endpoint toca solo los personales.

## 2. Petición

```http
PATCH /v1/identity/me
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "name": "Ana L. Hernández", "photoUploadId": "019400b2-1c3e-7f21-9b84-3d5e6f70a1b2" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `name` | string | **80** | no | Mínimo 2 caracteres. **Moderado** antes de guardar |
| `photoUploadId` | uuid | — | no | Id de una subida ya confirmada y aprobada. `null` explícito borra la foto |

Al menos un campo debe venir; un cuerpo vacío → `400`.

## 3. Respuesta

`200 OK` con el mismo cuerpo de [`GET /v1/identity/me`](get-me.md).

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Cuerpo vacío, nombre de menos de 2 caracteres o de más de 80 |
| 403 | `content.inappropriate` | Content Safety rechazó el nombre |
| 404 | `upload.not_found` | Subida inexistente, ajena, no aprobada o con finalidad distinta de avatar; no se distinguen |
| 409 | `account.concurrent_update` | Otra petición cambió la cuenta; consultar y reintentar |
| 503 | `server.dependency_unavailable` | Moderación no configurada, caída o respuesta no verificable |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `updateOne({ _id: sub }, { $set: { name, photoUrl, updatedAt } })` | `_id` |
| `uploads` | `findOne({ _id: photoUploadId, ownerId: sub, status: "approved" })` — **propiedad y estado en la misma consulta** | `{ownerId, createdAt}` |

Escrituras derivadas:

- `outbox`: **`account.profile_updated`** `{ accountId, name, photoUrl }` → el worker propaga a
  `user_profiles` de Discovery y a `conversations` de Engagement (donde el nombre y la foto están
  desnormalizados para la bandeja).
- Invalida los tags `account:{id}` y, si tiene perfil, `provider:{providerId}`.

## 6. Caché y rate limit

- Invalida `me:{accountId}` y los tags de arriba.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Azure AI Content Safety** | Moderación del nombre **antes** de guardar (síncrona, en el handler) |
| **Front Door CDN** | La `photoUrl` resultante apunta al CDN |
| Outbox → Storage Queue | Propagación del nombre y la foto a los otros módulos |
| **Redis** | Invalidación por tags |

## 8. Seguridad y privacidad

1. El `sub` fija el recurso: no hay id en la ruta, no hay BOLA.
2. La subida se valida con **propiedad y estado en la misma consulta**
   (`_id` + `ownerId` + `status` + `purpose: avatar`), nunca con dos llamadas (constitución §III.2).
3. El nombre pasa por moderación: es texto que otras personas verán en el chat y en las opiniones.
4. El nombre viaja a otros módulos **por evento**, nunca escribiendo en su base directamente.
