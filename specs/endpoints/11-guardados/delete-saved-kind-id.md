# Quitar de guardados · `DELETE /v1/discovery/saved/{kind}/{id}`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Mismo icono de guardar en estado activo · `ACT.savew` / `ACT.savep` (alternan) |
| Spec SDD | nv-10 |
| Idempotente | Sí |

## 1. Función

Retira un elemento de la lista de guardados. La maqueta usa el mismo botón para poner y quitar; aquí
son dos métodos sobre el mismo recurso.

## 2. Petición

```http
DELETE /v1/discovery/saved/provider/019400d4-9e5a-7b23-8c91-4f6a7b8c9d01
Authorization: Bearer <access_token>
```

Sin cuerpo.

| Parámetro | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `kind` (ruta) | string | enum | sí | `provider` \| `work` \| `request` |
| `id` (ruta) | uuid v7 | — | sí | |

## 3. Respuesta

`200 OK`

```json
{ "kind": "provider", "targetId": "019400d4-…", "saved": false, "savedCounts": { "provider": 4, "work": 12, "request": 1 } }
```

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `kind` fuera del enum |
| 401 | `auth.token_expired` | |
| 429 | `rate.limit_exceeded` | Política `user` |

**No hay `404`** si no estaba guardado: se responde `200` con `saved: false`. Quitar algo que no
estaba deja el estado deseado, y aquí no se filtra información porque el recurso es la **relación**
del propio usuario, no el objetivo.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `saved` | `deleteOne({ accountId: sub, kind, targetId: id })` | **único `{accountId, kind, targetId}`** |

Una sola operación: no hace falta comprobar que el objetivo exista, porque se borra la relación, no
el objetivo. Eso permite **limpiar guardados que apuntan a contenido ya borrado**.

`savedCounts` se recalcula con el mismo `$facet` por `kind`.

Escrituras derivadas: ninguna.

## 6. Caché y rate limit

- Invalida `saved:{accountId}:*`.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Borrado por clave única |
| **Redis** | Invalidación de la lista |

## 8. Seguridad y privacidad

1. El filtro lleva `accountId: sub`: **solo se borra de la propia lista**.
2. No se valida el objetivo, lo que permite quitar entradas huérfanas (un trabajo borrado por su
   autor, por ejemplo) sin quedar bloqueado.
3. Sin notificaciones ni contadores públicos, igual que al guardar.
4. La operación es parte del control del usuario sobre sus propios datos (ARCO).
