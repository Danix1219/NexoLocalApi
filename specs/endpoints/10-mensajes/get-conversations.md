# Bandeja de mensajes · `GET /v1/engagement/conversations`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.16 Mensajes — bandeja · `S.mtab`, globo de no leídos en la barra |
| Spec SDD | nv-08 |
| Idempotente | Sí (lectura) |

## 1. Función

La bandeja de conversaciones. **Diferencia por modo**: en Modo Cliente muestra los chats con
prestadores; en Modo Proveedor, los chats con clientes. El globo de no leídos de la barra inferior
sale del total que devuelve este endpoint.

## 2. Petición

```http
GET /v1/engagement/conversations?role=client&filter=all&cursor=&limit=20
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `role` | string | enum | no | `client` \| `provider`. Por defecto, el modo del token |
| `filter` | string | enum | no | `all` \| `unread`. Por defecto `all` |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "items": [
    {
      "conversationId": "01940309-…",
      "counterpart": {
        "id": "019400d4-…",
        "displayName": "Plomería del Centro",
        "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
        "kind": "provider",
        "badges": ["phoneVerified"]
      },
      "lastPreview": "Puedo pasar mañana a las 9",
      "lastMessageAt": "2026-11-05T19:30:00Z",
      "ago": "hace 20 minutos",
      "unread": 2,
      "requestId": "01940107-…",
      "activeOrder": { "orderId": "01940208-…", "status": "accepted", "statusText": "Aceptado" },
      "blocked": false,
      "blockedByMe": false
    }
  ],
  "nextCursor": null,
  "hasMore": false,
  "totalUnread": 3
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `counterpart.displayName` | string | **80** | Nombre del perfil del prestador, o nombre del cliente **si hay pedido aceptado**; iniciales si no |
| `lastPreview` | string | **120** | Recorte del último mensaje. Si es imagen: "Envió una foto" |
| `unread` | int32 | — | No leídos **de este usuario** en esta conversación |
| `activeOrder` | object \| null | — | Para pintar el aviso del pedido dentro del chat |
| `blocked` | bool | — | `true` si alguna de las partes bloqueó |
| `totalUnread` | int32 | — | Suma para el globo de la barra inferior |

**No se devuelve nunca**: teléfono de la contraparte, su dirección, ni el contenido completo de los
mensajes (solo el recorte del último).

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `role` o `filter` fuera del enum |
| 403 | `provider.profile_required` | `role=provider` sin perfil |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `conversations` | `find({ clientId: sub })` o `find({ providerId: miPerfil })`, orden `lastMessageAt: -1` | **`{clientId, lastMessageAt: -1}`** y **`{providerId, lastMessageAt: -1}`** |
| `orders` | `find` de pedidos activos por par, con `$in` | `{clientId, status, createdAt: -1}` |
| `accounts` / `provider_profiles` | Nombre y foto por contrato, en **una** llamada con `$in` | — |

Campos desnormalizados en `conversations` para que la bandeja sea **una sola consulta**:
`lastMessageAt`, `lastPreview`, `unread: { client, provider }`. Sin ellos haría falta un `$lookup` a
`messages` por conversación, que es el patrón que hunde una bandeja de chat.

El cursor usa `{ lastMessageAt, _id }`: `lastMessageAt` cambia con cada mensaje, así que el desempate
por `_id` es imprescindible para no repetir ni perder filas al paginar.

`readPreference` primario: un no leído desactualizado se nota de inmediato.

## 6. Caché y rate limit

- **Sin caché.** `Cache-Control: private, no-store`.
- Rate limit **`user`** (120/min). La bandeja se actualiza por **SignalR**, no por sondeo: la app se
  suscribe al hub y solo vuelve a pedir la lista al entrar a la pantalla.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Índices por papel y `lastMessageAt`; campos desnormalizados |
| **Azure SignalR** | Actualización en vivo de la bandeja y del globo de no leídos |
| **Polly v8** | Timeout + circuit breaker al pedir nombres |
| **Front Door CDN** | Avatares |

## 8. Seguridad y privacidad

1. El filtro es `clientId: sub` o `providerId` del perfil del `sub`: **solo las conversaciones
   propias**.
2. El nombre completo del cliente aparece **solo si hay pedido aceptado**, igual que la dirección:
   la misma gradación en todo el producto.
3. `lastPreview` se guarda ya recortado y moderado; nunca se arma en la bandeja a partir del mensaje
   crudo.
4. De una conversación bloqueada se devuelve el hecho del bloqueo, no el motivo ni quién lo pidió,
   salvo `blockedByMe` que sí es información propia.
5. Sin sondeo: evitar que la app pregunte cada pocos segundos es también una medida de costo en el
   plan gratuito.
