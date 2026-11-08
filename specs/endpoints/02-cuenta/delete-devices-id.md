# Quitar dispositivo · `DELETE /v1/identity/devices/{id}`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado, dueño del dispositivo) |
| Pantalla de la maqueta | Cuenta → desactivar notificaciones; también en logout |
| Spec SDD | nv-02 |
| Idempotente | Sí — borrar lo ya borrado responde `204` |

## 1. Función

Da de baja un dispositivo para que deje de recibir push. Lo llama la app al desactivar las
notificaciones y como parte del cierre de sesión.

## 2. Petición

```http
DELETE /v1/identity/devices/019400c3-8d4f-7a12-b7e5-1f2a3b4c5d60
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Id del dispositivo. Debe pertenecer al `sub` |

## 3. Respuesta

`204 No Content` — sin cuerpo.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | No existe **o no es de esta cuenta**. No se distingue, para no confirmar la existencia de un dispositivo ajeno |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `devices` | `deleteOne({ _id: id, accountId: sub })` — **id y propiedad en el mismo filtro** | `_id`, `accountId` |

Si `deletedCount == 0` se responde `404` sin consultar de nuevo: una sola ida a la base.

`audit_events`: `device.removed` `{ accountId, deviceId }`.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`**.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Borrado filtrado por propiedad |

## 8. Seguridad y privacidad

1. **Propiedad en el mismo filtro del borrado** (`_id` + `accountId`): no se lee primero y se borra
   después, que sería dos llamadas y una ventana de carrera (constitución §III.2).
2. `404` tanto si no existe como si es de otra cuenta: no se confirma la existencia de recursos
   ajenos (ver [`../00-convenciones/errores.md`](../00-convenciones/errores.md) §2.3).
3. El borrado es inmediato y definitivo: no hay papelera de dispositivos.
