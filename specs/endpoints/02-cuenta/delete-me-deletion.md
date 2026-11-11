# Cancelar la eliminación · `DELETE /v1/identity/me/deletion`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado, cuenta en `pendingDeletion`) |
| Pantalla de la maqueta | Cuenta → aviso de baja pendiente con "Cancelar" |
| Spec SDD | nv-12 (coordinación global); nv-04 (cuenta local) |
| Idempotente | Sí sobre el estado; si ya está activa, responde `409` sin nuevas escrituras |

## 1. Función

Revierte la baja dentro de los 30 días y devuelve la cuenta a `active`. Es la contraparte obligatoria
de [`POST /me/deletion`](post-me-deletion.md): sin ella la ventana de gracia no serviría de nada.

Resultado global previsto una vez integrados los consumidores de nv-12 (no es la respuesta síncrona actual):

| Se recupera | No se recupera |
|---|---|
| La cuenta y el acceso | Las solicitudes que se cerraron |
| El perfil profesional, visible de nuevo | Los pedidos `pending` que se cancelaron |
| Los trabajos publicados | Los dispositivos (hay que volver a conceder el permiso de notificaciones) |
| Guardados, conversaciones e historial | |

La app debe decirlo así antes de que el usuario solicite la baja; el servidor lo repite en la
respuesta.

## 2. Petición

```http
DELETE /v1/identity/me/deletion
Authorization: Bearer <access_token>
```

Sin cuerpo. **No se reexige la contraseña**: cancelar es la acción segura, y poner trabas a
deshacer un borrado va en contra del usuario.

## 3. Respuesta

`200 OK`

```json
{
  "status": "active",
  "cancelledAt": "2026-11-08T10:02:00Z",
  "restorationStatus": "queued"
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `status` | string | `active` |
| `restorationStatus` | string | `queued`: el evento es durable; no implica que otros dominios hayan restaurado datos |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `account.not_pending_deletion` | La cuenta ya está `active`: no hay nada que cancelar |
| 410 | `account.deletion_already_executed` | Pasaron los 30 días y el job ya anonimizó. **Irreversible** |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `updateOne({ _id: sub, status: "pendingDeletion" }, { $set: { status: "active" }, $unset: { deletionRequestedAt: "", deletionReason: "" } })` | `_id` + `status` **en el mismo filtro** |

Si `modifiedCount == 0`, la cuenta no estaba pendiente → `409`, sin segunda consulta.

Escrituras derivadas:

- `outbox`: **`account.deletion_cancelled`** `{ accountId }` → el worker vuelve a publicar el perfil
  en Discovery y reabre las conversaciones en Engagement.
- `audit_events`: `account.deletion_cancelled`, con cadena de hash.
- **MailKit**: correo de aviso ("tu cuenta sigue activa").

## 6. Caché y rate limit

- Invalida `me:{accountId}`, `account:{id}`, `provider:{providerId}` y las búsquedas de su zona
  (el perfil vuelve a aparecer).
- Rate limit **`user`**.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Actualización condicionada al estado |
| Outbox → Storage Queue | Reactivación en Discovery y Engagement |
| **MailKit** | Correo de aviso |
| **Redis** | Invalidación por tags |

## 8. Seguridad y privacidad

1. **El estado va en el filtro del `updateOne`**, no en una lectura previa: así dos peticiones
   simultáneas no pueden ambas "cancelar" ni dejar la cuenta en un estado raro.
2. No se pide contraseña **a propósito**: cancelar restaura el estado anterior y nunca destruye nada,
   así que exigirla solo perjudicaría a quien se arrepintió.
3. `410` cuando ya se ejecutó la baja: es honesto decir que es irreversible, en vez de fingir que se
   puede recuperar.
4. El correo de aviso cierra el círculo: si alguien cancela una baja que el titular sí quería, el
   titular se entera.
