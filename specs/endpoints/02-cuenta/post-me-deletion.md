# Solicitar eliminación de cuenta · `POST /v1/identity/me/deletion`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.13 Cuenta → "Eliminar cuenta" · `ACT.delacct` |
| Spec SDD | nv-12 (coordinación global); nv-04 (cuenta local) |
| Idempotente | Sí sobre el estado: repetir no adelanta la fecha y responde `409` con la fecha vigente |

## 1. Función

Inicia la baja de la cuenta con **ventana de gracia de 30 días**, que es lo que exige la constitución
§IV.2 (derechos ARCO, LFPDPPP: borrado real en ≤ 30 días con anonimización). Durante la ventana la
cuenta queda en `pendingDeletion`. La [cancelación](delete-me-deletion.md) requiere una sesión
válida. Desde este incremento, login/refresh emiten un token `recovery_only` durante la gracia.
Permite consultar cuenta, cancelar baja y cerrar sesión. Al solicitar baja se invalidan todos los
tokens anteriores mediante `auth_epoch`; hay que iniciar sesión de nuevo para cancelar.

Efectos globales previstos de nv-12 tras consumir el evento (todavía no implementados en los otros dominios):

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
| 409 | `account.concurrent_update` | La cuenta cambió durante la operación |
| 403 | `account.pending_deletion` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `updateOne` de `status: "pendingDeletion"`, `deletionRequestedAt`, `deletionReason` | `{status, deletionRequestedAt}` — lo usa el job diario |
| `openiddict.tokens` | Revoca todas las familias; incrementa `auth_epoch` para rechazar JWT anteriores incluso tras cancelar | `subject` |
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

Alcance de este [incremento](implementacion.md): solicitud/cancelación local y eventos durables.
Los efectos en pedidos/perfiles, anonimización final, eliminación de medios y prueba de plazos
pertenecen a nv-12. No hay operaciones de pedidos implementadas actualmente; no se simula una
consulta de pedidos ni se afirma que la saga global esté aceptada. Los consumidores deben
integrarse antes de habilitar esos dominios junto con baja de cuenta.

1. **Se reexige la contraseña**: un token robado no debe poder borrar la cuenta.
2. La ventana de 30 días protege del borrado impulsivo o malicioso y es el plazo que fija la
   constitución.
3. **El borrado es real, no un `status`**: a los 30 días se anonimiza de verdad. Lo que se conserva
   (pedidos y opiniones) va sin dato personal, porque la contraparte tiene derecho a su propio
   historial.
4. La coordinación con pedidos aceptados y el plazo global de borrado son aceptación pendiente
   de nv-12; la solicitud local no prueba esas reglas distribuidas.
5. El evento queda en `audit_events` con cadena de hash para poder demostrar **cuándo** se pidió y
   **cuándo** se ejecutó.
