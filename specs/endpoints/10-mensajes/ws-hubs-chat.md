# Chat en tiempo real · `WS /hubs/chat` (SignalR)

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (autenticado, con ticket) |
| Pantalla de la maqueta | 7.16 Chat y globo de no leídos de la barra inferior |
| Spec SDD | nv-08 |
| Idempotente | N/A (conexión persistente) |

## 1. Función

Canal en vivo para mensajes, acuses de lectura, avisos de pedido y el contador de no leídos. Es lo
que evita que la app tenga que sondear la bandeja cada pocos segundos.

El **lineamiento 15 del curso exige Redis explícitamente**, y aquí es donde cumple su papel más
visible: el **backplane** que permite dos réplicas de la API sin que un mensaje se pierda porque el
emisor y el receptor están conectados a réplicas distintas.

## 2. Conexión

**Paso 1 — obtener el ticket** (HTTP):

```http
POST /v1/engagement/hubs/chat/ticket
Authorization: Bearer <access_token>
```

```json
{ "ticket": "ws_3f8a1c5e9b244d07b1e56a7c8d9e0f12", "expiresInSeconds": 60, "url": "/hubs/chat" }
```

**Paso 2 — conectar**:

```
wss://api.nexolocal.mx/hubs/chat?ticket=ws_3f8a…
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `ticket` | string | **64** | **Un solo uso**, TTL **60 s**, guardado en Redis |
| `expiresInSeconds` | int32 | — | 60 |

Se usa un ticket y **no el JWT en la query string** porque las URL quedan en logs y proxies; el
ticket caduca en un minuto y se consume al conectar.

## 3. Mensajes del servidor al cliente

| Método | Carga | Cuándo |
|---|---|---|
| `messageReceived` | El mensaje completo (igual que el DTO de [enviar](post-conversations-id-messages.md)) | Mensaje nuevo en una conversación propia |
| `messageRead` | `{ conversationId, upToMessageId, readAt }` | La otra parte leyó |
| `conversationUpdated` | `{ conversationId, lastPreview, lastMessageAt, unread, totalUnread }` | Para repintar la bandeja y el globo |
| `orderEvent` | `{ conversationId, orderId, type, statusText }` | Pedido aceptado, rechazado, cancelado, resuelto o con dirección cambiada |
| `conversationBlocked` | `{ conversationId, canSend: false }` | Alguna parte bloqueó |
| `typing` | `{ conversationId, isTyping }` | Opcional; no se persiste |

## 4. Mensajes del cliente al servidor

| Método | Parámetros | Regla |
|---|---|---|
| `JoinConversation` | `conversationId` (uuid) | **Se verifica la participación en el servidor** antes de añadir al grupo |
| `LeaveConversation` | `conversationId` | |
| `Typing` | `conversationId`, `isTyping` (bool) | Rate limit propio: 1 cada 2 s |

**Enviar mensajes no va por el hub**, va por `POST /messages`. Así la moderación, la idempotencia y
el rate limit viven en un solo camino, y un mensaje no puede entrar sin pasar por Content Safety.

## 5. Errores

| Situación | Respuesta |
|---|---|
| Ticket inválido o caducado | Cierre con `4401` y `code: ws.ticket_invalid` |
| `JoinConversation` de una conversación ajena | Error en el método, `code: conversation.not_found`; **no se une al grupo** |
| Token caducado durante la conexión | Cierre con `4401`; la app renueva y reconecta |
| Demasiadas conexiones | Cierre con `4429`; máximo **3 conexiones** por cuenta |

## 6. Datos

| Clave Redis | Para qué | TTL |
|---|---|---|
| `nexo:ws:{ticket}` | Ticket de un solo uso (`GETDEL` al conectar) | **60 s** |
| Canales de pub/sub de SignalR | Backplane entre réplicas | — |
| `nexo:ws:conn:{accountId}` | Conteo de conexiones por cuenta | mientras viva |

| Colección | Operación |
|---|---|
| `conversations` | `findOne` con participación en el filtro, al unirse a un grupo |

Grupos de SignalR: `conv:{conversationId}` para una conversación y `user:{accountId}` para los avisos
de bandeja y pedidos.

**Nada del chat se persiste desde el hub**: los mensajes los escribe el endpoint HTTP y el hub solo
difunde. Si el hub cayera, los mensajes seguirían guardándose y la app los recuperaría con
`?after=`.

## 7. Caché y rate limit

- Rate limit **`chat`** en el envío HTTP (60/min), no en el hub.
- `Typing`: 1 cada 2 s por conversación, en memoria.
- Máximo **3 conexiones** simultáneas por cuenta (teléfono, otra sesión, reconexión en curso).

## 8. Tecnologías

| Pieza | Para qué |
|---|---|
| **Microsoft.Azure.SignalR** (plan Free) | Servicio gestionado de WebSocket |
| **Redis** (`StackExchange.Redis`) | **Backplane** entre las dos réplicas y tickets de conexión |
| **signalr_netcore** (app Flutter) | Cliente de SignalR para .NET desde Dart |
| **connectivity_plus** (app) | Detecta la reconexión y dispara la recuperación con `?after=` |

### Reconexión

1. La app detecta la caída (evento de SignalR o `connectivity_plus`).
2. Reintenta con **backoff**: 0, 2, 10, 30 s.
3. Al reconectar pide un ticket nuevo, se une a los grupos y llama a
   `GET /messages?after={últimoRecibido}` en cada conversación abierta: así **no se pierde ningún
   mensaje** de la desconexión.

## 9. Seguridad y privacidad

1. **Ticket de un solo uso en vez del JWT en la URL**: una query string viaja a logs de servidor,
   proxies y métricas; un token ahí es una fuga esperando a ocurrir.
2. **La participación se verifica en `JoinConversation`**, en el servidor: conocer un
   `conversationId` no basta para escuchar un chat ajeno.
3. El hub **no escribe nada**: todo mensaje pasa por el endpoint HTTP, donde está la moderación. Sin
   esa separación, el canal en vivo sería la puerta para saltarse Content Safety.
4. Los eventos de pedido llevan **referencias** (`orderId`), nunca la dirección: quien quiera el dato
   pasa por `GET /orders/{id}` y su regla de visibilidad.
5. El límite de 3 conexiones por cuenta protege el plan gratuito de SignalR y detecta sesiones
   anómalas.
6. El cierre `4401` al caducar el token mantiene la sesión del WebSocket alineada con la del HTTP:
   un logout corta también el canal en vivo.
