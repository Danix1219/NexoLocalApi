# Guardar · `POST /v1/discovery/saved/{kind}/{id}`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Icono de guardar en tarjetas de feed, perfil y solicitudes · `ACT.savew`, `ACT.savep` |
| Spec SDD | nv-10 |
| Idempotente | Sí |

## 1. Función

Guarda un prestador, un trabajo o una solicitud en la lista personal. Es una de las acciones que la
maqueta pone detrás del **gate de cuenta**.

Tres tipos, como en el prototipo (`S.savedP`, `S.savedW`, `S.savedR`).

## 2. Petición

```http
POST /v1/discovery/saved/provider/019400d4-9e5a-7b23-8c91-4f6a7b8c9d01
Authorization: Bearer <access_token>
```

Sin cuerpo.

| Parámetro | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `kind` (ruta) | string | enum | sí | `provider` \| `work` \| `request` |
| `id` (ruta) | uuid v7 | — | sí | El objetivo debe existir y estar publicado |

## 3. Respuesta

`200 OK`

```json
{ "kind": "provider", "targetId": "019400d4-…", "saved": true, "savedCounts": { "provider": 5, "work": 12, "request": 1 } }
```

| Campo | Tipo | Regla |
|---|---|---|
| `saved` | bool | Siempre `true` tras esta llamada |
| `savedCounts` | object | Totales por tipo, para el contador de Cuenta |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `kind` fuera del enum |
| 404 | `resource.not_found` | El objetivo no existe o está despublicado |
| 403 | `saved.own_content` | No se guarda el propio perfil ni los propios trabajos |
| 401 | `auth.token_expired` | El visitante recibe `401` y la app abre el gate |
| 429 | `rate.limit_exceeded` | Política `user` |

**No hay `409`** por guardar lo ya guardado: el índice único lo absorbe.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `saved` | `updateOne({ accountId: sub, kind, targetId: id }, { $setOnInsert: { createdAt } }, { upsert: true })` | **único `{accountId, kind, targetId}`**; `{accountId, createdAt: -1}` |

La existencia del objetivo se comprueba según `kind` contra `provider_profiles`, `works` o
`service_requests`, con una sola consulta proyectando solo `_id`.

`savedCounts` sale de un `$facet` por `kind` sobre la colección `saved` del usuario.

Escrituras derivadas: ninguna. Guardar es privado: **no se notifica a nadie** ni se publica un
contador en el objetivo. Que alguien guarde un perfil no es asunto del prestador.

## 6. Caché y rate limit

- Invalida `saved:{accountId}:*`.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `upsert` con índice único de tres campos |
| **Redis** | Invalidación de la lista del usuario |

## 8. Seguridad y privacidad

1. **Guardar es privado.** No hay contador público ni notificación: el objetivo nunca sabe quién lo
   guardó. Un "N personas te guardaron" convertiría un gesto privado en una señal pública.
2. El índice único de `{accountId, kind, targetId}` hace la operación idempotente sin lectura previa.
3. Se comprueba que el objetivo exista y esté publicado: no se guardan referencias a nada.
4. `saved.own_content` evita inflar la propia lista con contenido propio, que no tiene sentido.
5. El visitante recibe `401` del servidor: el gate de cuenta no es solo de interfaz.
