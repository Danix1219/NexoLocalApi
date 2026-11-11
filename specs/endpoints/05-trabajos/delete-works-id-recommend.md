# Quitar recomendación · `DELETE /v1/discovery/works/{id}/recommend`

> Refinamiento ejecutable 2026-11-10: [implementación, decisiones y límites](implementacion.md) prevalece sobre los ejemplos históricos de caché/Redis/medios. Autoridad de arquitectura: nv-08.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C** (autenticado, quien recomendó) |
| Pantalla de la maqueta | Mismo icono de recomendar, en estado activo · `ACT.rec` (alterna) |
| Spec SDD | nv-03 |
| Idempotente | Sí |

## 1. Función

Retira la recomendación propia. La maqueta usa el mismo botón para poner y quitar; aquí son dos
métodos sobre el mismo recurso, que es lo que corresponde en REST y evita un endpoint "toggle" cuyo
resultado depende del estado anterior.

## 2. Petición

```http
DELETE /v1/discovery/works/019400f6-ab12-7c34-9d56-7e8f90a1b2c3/recommend
Authorization: Bearer <access_token>
```

Sin cuerpo.

| Parámetro | Tipo | Requerido | Regla |
|---|---|---|---|
| `id` (ruta) | uuid v7 | sí | |

## 3. Respuesta

`200 OK`

```json
{ "workId": "019400f6-…", "recommended": false, "recs": 12, "recsText": "12 recomendaciones" }
```

Se devuelve el contador actualizado para que la tarjeta se repinte sin otra llamada.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `work.not_found` | El trabajo no existe |
| 401 | `auth.token_expired` | |
| 429 | `rate.limit_exceeded` | Política `user` |

**No hay error** si no había recomendación: se responde `200` con `recommended: false`. Quitar algo
que no estaba deja el estado deseado.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `recommendations` | `deleteOne({ accountId: sub, workId: id })` | **único `{accountId, workId}`** |
| `works` | `$inc: { recs: -1 }` **solo si `deletedCount == 1`**, con `$max: 0` lógico para no bajar de cero | `_id` |

El `$inc` condicionado al borrado real es lo que evita que varias llamadas seguidas dejen el
contador en negativo.

Escrituras derivadas:

- `outbox`: **`work.recommendation_removed`** `{ workId, providerId, accountId }` → el worker
  recalcula `provider_profiles.recs` e invalida `provider:{id}`.
- **No** se envía push: avisar "te quitaron una recomendación" solo haría daño y no habilita ninguna
  acción.

## 6. Caché y rate limit

- Invalida `provider:{id}` y las entradas de feed que contienen el trabajo.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Borrado por clave única; `$inc` condicionado |
| **Redis** | Invalidación por tags |
| Outbox → Storage Queue | Recálculo del contador del prestador |

## 8. Seguridad y privacidad

1. El filtro lleva `accountId: sub`: **solo se puede quitar la propia** recomendación.
2. El contador nunca baja de cero, aunque lleguen peticiones duplicadas.
3. Sin notificación al prestador: no toda acción merece un aviso, y esta solo generaría desánimo sin
   nada que hacer al respecto.
4. Retirar la recomendación es parte del control del usuario sobre lo que publicó (ARCO).
