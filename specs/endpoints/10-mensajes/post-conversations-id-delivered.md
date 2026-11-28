# Confirmar entrega · `POST /v1/engagement/conversations/{id}/delivered`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **parte** de la conversación (el receptor) |
| Spec SDD | nv-08 |
| Rate limit | `chat` |
| Idempotente | Sí: repetir con el mismo `upToMessageId` no cambia nada |

## 1. Función

El receptor avisa que su app ya recibió los mensajes hasta `upToMessageId` (doble marca gris). Es distinto
de leído (`/read`, marcas celestes). Un aviso SignalR por sí solo no cuenta como entrega.

## 2. Petición

```json
{ "upToMessageId": "0194030a-…" }
```

## 3. Respuestas

| Estado | Cuándo |
|---|---|
| 200 | Recibos actualizados |
| 400 | Falta `upToMessageId` |
| 401 | Sin sesión |
| 403 / 404 | No es parte de la conversación o no existe |
| 503 | Mongo no disponible |
