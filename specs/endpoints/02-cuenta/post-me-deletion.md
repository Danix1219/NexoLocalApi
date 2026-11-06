# Solicitar eliminación de cuenta · `POST /v1/identity/me/deletion`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.13 Cuenta → "Eliminar cuenta" · `ACT.delacct` |
| Spec SDD | nv-15 |
| Idempotente | Sí — pedirla dos veces no adelanta la fecha |

## 1. Función

Inicia la baja de la cuenta con **ventana de gracia de 30 días**, que es lo que exige la constitución
§IV.2 (derechos ARCO, LFPDPPP: borrado real en ≤ 30 días con anonimización). Durante la ventana la
cuenta queda en `pendingDeletion`: se puede iniciar sesión solo para
[cancelar la baja](delete-me-deletion.md).

Efecto inmediato al solicitarla:

- El perfil profesional **deja de aparecer** en búsqueda, mapa y feed.
- Las solicitudes abiertas se **cierran**.
- Los pedidos `pending` se **cancelan**; los `accepted` se conservan (hay una contraparte esperando)
  y la baja real espera a que se resuelvan o a que venzan los 30 días.
- No se reciben más notificaciones push.

## 2. Petición

```http
POST /v1/identity/me/deletion
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "password": "Nexo2026ok", "reason": "Ya no necesito el servicio" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `password` | string | **8–64** | sí | **Se reexige la contraseña**: es una acción destructiva |
| `reason` | string | **300** | no | Texto libre, solo para estadística agregada. No se publica |

## 3. Respuesta

`202 Accepted`

```json
{
  "status": "pendingDeletion",
  "deletionRequestedAt": "2026-11-05T16:45:00Z",
  "effectiveAt": "2026-12-05T16:45:00Z",
  "daysRemaining": 30,
  "canCancel": true
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `effectiveAt` | date | `deletionRequestedAt` + 30 días |
| `daysRemaining` | int32 | 0–30 |
| `canCancel` | bool | `true` durante toda la ventana |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 401 | `auth.invalid_credentials` | Contraseña incorrecta |
| 409 | `account.already_pending_deletion` | Ya estaba solicitada; el cuerpo trae la fecha vigente |
| 409 | `account.has_active_orders` | Hay pedidos `accepted` sin resolver. El `detail` los enumera por cantidad y la app pide resolverlos o cancelarlos primero |
| 403 | `account.pending_deletion` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `updateOne` de `status: "pendingDeletion"`, `deletionRequestedAt`, `deletionReason` | `{status, deletionRequestedAt}` — lo usa el job diario |
| `openiddict.tokens` | Revoca todas las familias salvo la sesión actual | `subject` |
| `devices` | `deleteMany({ accountId })` | `accountId` |

Escrituras derivadas:

- `outbox`: **`account.deletion_requested`** `{ accountId, effectiveAt }`. El worker, en cada módulo:
  - Discovery: oculta `provider_profiles`, cierra `service_requests` abiertas, despublica `works`.
  - Engagement: cancela pedidos `pending`, cierra conversaciones a nuevos mensajes.
- `audit_events`: `account.deletion_requested` — **evento con cadena de hash**, porque es de los
  sensibles junto con copiar número y revelar dirección.
- **MailKit**: correo de confirmación con la fecha efectiva y cómo cancelar.

El borrado real lo ejecuta el **`DeletionJob`** del worker: a los 30 días anonimiza
(`email → deleted+{id}@nexolocal.invalid`, `name → "Usuario eliminado"`, borra `phone`,
`passwordHash`, fotos del CDN) y conserva solo lo que debe sobrevivir por integridad: pedidos
resueltos y opiniones, ya anonimizados.

## 6. Caché y rate limit

- Invalida `me:{accountId}`, `account:{id}`, `provider:{providerId}` y las búsquedas de su zona.
- Rate limit **`user`**.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Argon2id** | Verificación de la contraseña |
| **MailKit** | Correo de confirmación de la solicitud |
| Outbox → Storage Queue | Propagación a Discovery y Engagement |
| **Worker `DeletionJob`** | Borrado y anonimización a los 30 días |
| **Front Door CDN** | Purga de las imágenes del usuario al ejecutarse la baja |

## 8. Seguridad y privacidad

1. **Se reexige la contraseña**: un token robado no debe poder borrar la cuenta.
2. La ventana de 30 días protege del borrado impulsivo o malicioso y es el plazo que fija la
   constitución.
3. **El borrado es real, no un `status`**: a los 30 días se anonimiza de verdad. Lo que se conserva
   (pedidos y opiniones) va sin dato personal, porque la contraparte tiene derecho a su propio
   historial.
4. `account.has_active_orders` evita dejar a alguien colgado con un servicio aceptado; no es un
   obstáculo al derecho ARCO: a los 30 días la baja procede de todos modos.
5. El evento queda en `audit_events` con cadena de hash para poder demostrar **cuándo** se pidió y
   **cuándo** se ejecutó.
