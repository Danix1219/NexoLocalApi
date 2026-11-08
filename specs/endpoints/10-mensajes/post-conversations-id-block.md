# Bloquear o desbloquear · `POST /v1/engagement/conversations/{id}/block`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **parte** de la conversación |
| Pantalla de la maqueta | 7.16 Chat → menú "⋯" → Bloquear · `ACT.block` |
| Spec SDD | nv-08 |
| Idempotente | Sí |

## 1. Función

Impide que la otra parte siga escribiendo. Es la herramienta de defensa del usuario frente al acoso,
y va junto al reporte: bloquear **corta el contacto ya**, reportar **pide revisión humana**.

El bloqueo es **unilateral y recíproco en efecto**: si A bloquea a B, ninguno de los dos puede
escribir en esa conversación, pero solo A puede levantarlo.

## 2. Petición

```http
POST /v1/engagement/conversations/01940309-…/block
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "blocked": true, "alsoReport": false }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | El `sub` debe ser parte |
| `blocked` | bool | — | sí | `true` bloquea, `false` desbloquea |
| `alsoReport` | bool | — | no | Atajo de la maqueta: bloquear y abrir el reporte |

## 3. Respuesta

`200 OK`

```json
{
  "conversationId": "01940309-…",
  "blocked": true,
  "blockedByMe": true,
  "canSend": false,
  "activeOrderWarning": "Tienes un pedido aceptado con esta persona. Bloquear no lo cancela."
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `blocked` | bool | — | Hay bloqueo de alguna de las partes |
| `blockedByMe` | bool | — | Si el `sub` es quien bloqueó. **Solo el que bloqueó puede levantarlo** |
| `activeOrderWarning` | string \| null | **200** | Aviso si queda un pedido activo entre ambos |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `conversation.not_found` | No existe o no eres parte |
| 403 | `conversation.blocked_by_other` | Intenta desbloquear un bloqueo que **puso la otra parte** |
| 401 | `auth.token_expired` | |

**No hay error** por bloquear lo ya bloqueado: el `$addToSet` lo absorbe.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `conversations` | Bloquear: `$addToSet: { blockedBy: sub }`. Desbloquear: `$pull: { blockedBy: sub }` | único `{clientId, providerId}` |
| `orders` | `countDocuments` de pedidos activos entre ambos, para el aviso | `{clientId, status, createdAt: -1}` |

`blockedBy` es un array de **máximo 2** elementos. `$addToSet` y `$pull` hacen la operación
idempotente y permiten que cada parte levante **solo su propio** bloqueo: `$pull: { blockedBy: sub }`
no puede quitar el del otro.

`canSend` en el chat se evalúa como `blockedBy.length === 0`.

Escrituras derivadas:

- `outbox`: **`conversation.blocked`** / **`conversation.unblocked`** `{ conversationId, actorId }`.
- **No se notifica a la otra parte.** Avisar "te bloquearon" invita a represalias por otro canal; la
  persona lo descubre al intentar escribir, y recibe un mensaje neutro.
- `audit_events`: `conversation.blocked`, útil si después hay un reporte.

**Bloquear no cancela el pedido.** Son cosas distintas: si hay un servicio aceptado, el compromiso
sigue y hay que cancelarlo explícitamente. El aviso lo dice.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `$addToSet` / `$pull` sobre `blockedBy` |
| **Azure SignalR** | Se cierra el grupo para esa conversación; la otra parte deja de recibir en vivo |
| `audit_events` | Registro del bloqueo |

## 8. Seguridad y privacidad

1. **Efecto inmediato en las dos direcciones**: el bloqueo no sirve de nada si la otra parte puede
   seguir escribiendo mientras tanto.
2. `$pull: { blockedBy: sub }` garantiza que **nadie levante el bloqueo que otro puso**, sin
   necesidad de comprobar nada antes.
3. **No se avisa a quien fue bloqueado.** Es la práctica correcta: reduce el riesgo de escalada.
4. El aviso del pedido activo evita que alguien crea que bloquear cancela el servicio y se quede
   esperando a un prestador que ya no va a llegar.
5. El bloqueo queda auditado, lo que da contexto a una revisión humana posterior si hay reporte.
