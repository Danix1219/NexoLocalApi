# Marcar notificaciones como leídas · `POST /v1/engagement/notifications/read`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Hoja de notificaciones, al abrirla o al tocar un aviso · `notifSheet` |
| Spec SDD | nv-11 |
| Idempotente | Sí |

## 1. Función

Marca como leídas una, varias o todas las notificaciones del usuario, y devuelve el contador
actualizado para el punto rojo de la campana.

## 2. Petición

```http
POST /v1/engagement/notifications/read
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "notificationIds": ["0194060e-239a-74c5-d6e7-f8091a2b3c4d"] }
```

o, para todas:

```json
{ "all": true }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `notificationIds[]` | array\<uuid v7> | **máx. 50** | uno de los dos | Ids concretos |
| `all` | bool | — | uno de los dos | `true` marca todas las no leídas |

Enviar los dos, o ninguno, → `400`.

## 3. Respuesta

`200 OK`

```json
{ "markedCount": 1, "unreadCount": 1 }
```

| Campo | Tipo | Regla |
|---|---|---|
| `markedCount` | int32 | Cuántas se marcaron de verdad |
| `unreadCount` | int32 | Las que quedan sin leer |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | Ambos campos, ninguno, o más de 50 ids |
| 401 | `auth.token_expired` | |
| 429 | `rate.limit_exceeded` | Política `user` |

**No hay `404`** por un id inexistente o ajeno: simplemente no se marca y no cuenta en
`markedCount`. Así la app puede mandar una lista sin validar nada antes, y un id ajeno no produce un
error que confirmaría su existencia.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `notifications` | `updateMany({ accountId: sub, readAt: null, _id: { $in: ids } }, { $set: { readAt: now } })` | **`{accountId, createdAt: -1}`** |
| idem con `all` | `updateMany({ accountId: sub, readAt: null }, { $set: { readAt: now } })` | mismo índice |

- **`accountId: sub` siempre en el filtro**: es lo que hace que un id ajeno no se marque, sin
  necesidad de validarlo aparte.
- `readAt: null` evita reescribir las ya leídas, con lo que el sello conserva la hora de la primera
  lectura y `markedCount` es exacto.

Escrituras derivadas:

- **SignalR**: se emite al grupo `user:{accountId}` el `unreadCount` nuevo, para que el punto rojo
  desaparezca en los otros dispositivos de la misma cuenta.
- Sin eventos de outbox: leer un aviso no interesa a nadie más.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `updateMany` acotado por cuenta y estado |
| **Azure SignalR** | Sincronización del contador entre dispositivos |
| **Redis** | Backplane de SignalR |

## 8. Seguridad y privacidad

1. `accountId: sub` en el filtro del `updateMany`: **imposible marcar notificaciones de otra
   persona**, y no hace falta comprobar la propiedad de cada id por separado.
2. No se devuelve error por ids ajenos o inexistentes: evita usar este endpoint para sondear qué ids
   existen.
3. El tope de 50 ids por llamada acota el tamaño de la operación.
4. La sincronización por SignalR evita el caso molesto de limpiar la campana en el teléfono y
   encontrarla con el punto rojo en otra sesión.
