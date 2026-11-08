# Fijar la contraseña nueva · `POST /v1/identity/password/reset`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **P** (público, con `verificationTicket`) |
| Pantalla de la maqueta | 7.2 Autenticación, vista `newpass` → `done` · `pwReqHTML` |
| Spec SDD | nv-01 |
| Idempotente | No. El ticket se consume |

## 1. Función

Último paso de la recuperación. Consume el ticket obtenido al confirmar el código y guarda la
contraseña nueva. **Cierra todas las sesiones abiertas**: si alguien entró con la contraseña vieja,
deja de tener acceso.

Regla de la maqueta: contraseña nueva **×2 con checklist en vivo** (`pwReqHTML`); el servidor valida
igual, nunca confía en el cliente.

## 2. Petición

```http
POST /v1/identity/password/reset
Content-Type: application/json
```

```json
{
  "verificationTicket": "vt_9f3a1c7e8b24d05f6a1b2c3d4e5f6071",
  "password": "NuevaClave2026",
  "passwordConfirm": "NuevaClave2026"
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `verificationTicket` | string | **64** | sí | De un solo uso; su `purpose` debe ser `resetPassword` |
| `password` | string | **8–64** | sí | Al menos una letra y un número (`pwOK`). No puede ser igual a la actual |
| `passwordConfirm` | string | **8–64** | sí | Debe coincidir con `password` |

## 3. Respuesta

`204 No Content` — sin cuerpo y **sin tokens**: la app lleva a la pantalla `done` y de ahí al login.

Devolver una sesión aquí sería cómodo pero premia a quien solo tuvo acceso temporal al buzón; se
exige iniciar sesión con la contraseña nueva.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `email.ticket_invalid` | Ticket inexistente, caducado, ya usado o con otro `purpose` |
| 400 | `password.too_weak` | Menos de 8, o sin letra, o sin número |
| 400 | `password.mismatch` | `password` y `passwordConfirm` no coinciden |
| 400 | `password.same_as_current` | La contraseña nueva es igual a la vigente |
| 404 | `resource.not_found` | La cuenta fue eliminada entre el código y este paso |
| 503 | `server.dependency_unavailable` | Redis caído |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `updateOne` de `passwordHash`, `passwordChangedAt`, `updatedAt` | `email` único |
| `openiddict.tokens` | **Revoca todos** los tokens del `sub` | `subject` |
| `revoked_tokens` | Inserta los `jti` activos conocidos | TTL |
| `devices` | Se **conservan**: la cuenta sigue siendo la misma y el usuario volverá a entrar |

Redis:

- `GETDEL nexo:ticket:{ticket}` — consumo.
- `DEL nexo:lock:{emailHash}` — se levanta el bloqueo por intentos fallidos.
- `SET nexo:jti:{jti}` por cada sesión revocada.

Escrituras derivadas:

- `outbox`: **`password.changed`** `{ accountId }` → el worker envía el correo de aviso
  ("tu contraseña cambió; si no fuiste tú, contáctanos") y una notificación push.
- `audit_events`: `password.reset_completed` `{ accountId, ip, userAgent }`.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`auth`** (20/min por IP). La política `otp` ya limitó la obtención del código.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Argon2id** | Hash de la contraseña nueva, mismos parámetros que en el registro |
| **Redis** | Consumo del ticket, levantar el bloqueo, lista negra de `jti` |
| **OpenIddict** | Revocación total de sesiones |
| **MailKit** | Correo de aviso del cambio, encolado al worker |

## 8. Seguridad y privacidad

1. **Revocar todas las sesiones** es el punto central: sin eso, cambiar la contraseña no expulsa a
   quien ya estaba dentro.
2. **El aviso por correo es obligatorio.** Es la única forma de que el titular note un cambio que no
   hizo, y por eso se envía aunque el cambio sea legítimo.
3. `password.same_as_current` se comprueba contra el hash vigente, no guardando contraseñas antiguas:
   no se mantiene historial de contraseñas.
4. El ticket, de un solo uso y con 30 minutos de vida, es lo que liga este paso al control del buzón.
5. Se levanta el bloqueo por intentos fallidos: tras un restablecimiento legítimo sería absurdo
   seguir bloqueado.
