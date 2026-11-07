# Opiniones de un prestador · `GET /v1/engagement/providers/{id}/reviews`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **P/C/Pr** (público) |
| Pantalla de la maqueta | 7.8 Perfil → pestaña "Opiniones" · `EXPS`, `trustSheet` |
| Spec SDD | nv-09 |
| Idempotente | Sí (lectura) |

## 1. Función

Lista paginada de las opiniones aprobadas de un prestador, con el desglose por estrellas que la hoja
de confianza de la maqueta muestra.

## 2. Petición

```http
GET /v1/engagement/providers/019400d4-…/reviews?cursor=&limit=20
If-None-Match: "b2c3d4e5f6071829"
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | `providerId` |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK` (o `304`)

```json
{
  "providerId": "019400d4-…",
  "summary": {
    "avg": 4.7,
    "count": 13,
    "recommendCount": 11,
    "breakdown": { "5": 9, "4": 3, "3": 1, "2": 0, "1": 0 }
  },
  "items": [
    {
      "reviewId": "0194030a-…",
      "stars": 5,
      "text": "Llegó puntual, dejó todo limpio y explicó qué había pasado.",
      "recommend": true,
      "author": { "displayName": "Ana L.", "photoUrl": null },
      "category": "plomeria",
      "ago": "hace 2 días",
      "createdAt": "2026-11-05T22:00:00Z"
    }
  ],
  "nextCursor": "MDE5NDAzMGEt…",
  "hasMore": false,
  "trustLevel": "userOpinion"
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `summary.breakdown` | object | — | Conteo por estrella, para las barras de la hoja de confianza |
| `author.displayName` | string | **40** | Nombre de pila e inicial |
| `category` | string | **20** | Categoría del servicio calificado; da contexto a la opinión |
| `trustLevel` | string | enum | Siempre `userOpinion`. **La API etiqueta el nivel de confianza** |

**No se devuelve nunca**: `clientId` ni `orderId` (ligarían una opinión a un servicio concreto y, con
eso, a una dirección y una fecha), el nombre completo del autor, ni su foto si no la tiene pública.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `provider.not_found` | No existe o está despublicado |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 429 | `rate.limit_exceeded` | Política `public` |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `reviews` | `find({ providerId: id, "moderation.status": "approved" })` orden `_id: -1` | **`{providerId, createdAt: -1}`** |
| `reviews` | `$facet` con `$group` por `stars` para el `breakdown` | mismo índice |
| `accounts` | Nombre y foto del autor por contrato, en **una** llamada con `$in` | — |

El `summary` se calcula con un `$facet` en el mismo *pipeline* de la primera página y se cachea; en
las páginas siguientes se reusa el de la caché.

`readPreference: secondaryPreferred`.

## 6. Caché y rate limit

- **L2 (Redis) 5 min**, clave `rev:{providerId}:{cursor}:{limit}`.
- `ETag` + `Cache-Control: public, max-age=300`.
- Invalida con el tag `provider:{id}` (opinión nueva o retirada por moderación).
- Rate limit **`public`** (60/min por IP).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Lectura por `providerId`; `$facet` para el desglose |
| **Redis** | Caché L2 de 5 min |
| **Polly v8** | Timeout + circuit breaker al pedir nombres a Identity |
| **Front Door CDN** | Avatares de los autores |

## 8. Seguridad y privacidad

1. **No se expone `orderId` ni `clientId`.** Ligar públicamente una opinión a un pedido permitiría
   cruzarla con la fecha y la zona del servicio, y de ahí deducir quién la escribió.
2. Solo opiniones con `moderation.status: approved`: lo retirado desaparece del público y del
   promedio.
3. El autor va con **nombre de pila e inicial**, igual que al publicarla.
4. `trustLevel: "userOpinion"` viene en la respuesta para que la UI no pueda presentar esto como
   verificación de Nexo Local (constitución §IV.3).
5. Sin endpoint de edición ni borrado por el autor en esta versión: una opinión publicada se retira
   por moderación o con la baja de la cuenta, que la anonimiza.
