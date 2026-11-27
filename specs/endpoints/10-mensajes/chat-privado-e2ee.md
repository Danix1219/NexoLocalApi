# Chat privado cifrado de extremo a extremo · `/v1/engagement/chat-keys/*`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | Cuenta autenticada; en rutas de conversación, solo **participantes** de una conversación no bloqueada |
| Spec SDD | nv-08 (mensajes), nv-12 (privacidad) |
| Activación | `Chat__E2eeEnabled=true` (apagado por defecto). App: `--dart-define=CHAT_E2EE=true` |
| Caché | `Cache-Control: private, no-store` en todas |
| Rate limit | `user` en el grupo; `chat` en envío/edición |
| Diseño detallado | [docs/chat-privado-e2ee.md](../../../docs/chat-privado-e2ee.md), [docs/chat-edicion-eliminacion.md](../../../docs/chat-edicion-eliminacion.md) |

## 1. Función

Protocolo Signal (X3DH/PQXDH + Double Ratchet). El servidor solo guarda claves **públicas**, preclaves y
sobres cifrados por dispositivo; nunca ve texto, fotos ni voz. Sin moderación de contenido (no se puede
leer); se conservan bloqueo, límite de abuso y reporte de la conversación.

## 2. Rutas

| Método | Ruta | Qué hace | Respuestas |
|---|---|---|---|
| PUT | `/devices` | Registra o actualiza la identidad pública del dispositivo, clave firmada, Kyber y hasta 100 preclaves. Repetir no repone preclaves consumidas ni revive identidades revocadas | 204, 400, 401, 409 |
| GET | `/devices` | Dispositivos propios (slots, máximo 5) | 200, 401 |
| DELETE | `/devices/{deviceId}` | Revoca un dispositivo propio (`deviceId` 1–5) | 204, 400, 401 |
| POST | `/devices/{deviceId}/claim` | Toma un paquete de preclaves de **otro dispositivo propio** (consumo atómico en Mongo) | 200, 401, 404 |
| GET | `/conversations/{conversationId}/devices` | Dispositivos de ambos participantes para cifrar | 200, 401, 403, 404 |
| POST | `/conversations/{conversationId}/devices/{deviceId}/claim` | Toma preclaves del dispositivo del otro participante | 200, 401, 403, 404, 409 |
| POST | `/conversations/{conversationId}/enable` | Activa el modo privado cuando ambos tienen claves; después el contrato de texto legado se rechaza | 200, 401, 403, 409 contacto sin claves |
| POST | `/conversations/{conversationId}/messages` | Envía un sobre cifrado por dispositivo destino. `Idempotency-Key` UUID obligatorio | 201, 400, 401, 403, 409, 429 |
| PATCH | `/conversations/{conversationId}/messages/{messageId}` | Reemplaza el sobre de un mensaje **propio** (`editOf` autenticado). Devuelve `messageId`, `editedAt` | 200, 400, 401, 403, 404, 409 eliminado |
| DELETE | `/conversations/{conversationId}/messages/{messageId}` | Borrado lógico (`kind=deleted`) del mensaje propio | 204, 401, 403, 404 |
| PUT | `/conversations/{conversationId}/attachments/{attachmentId}` | Sube ciphertext opaco (≤ 8 MiB) a Blob privado `chat-private`; `Content-Length` obligatorio | 204, 400, 401, 403 no cifrada o bloqueada, 404, 409 ya existe, 413 |
| GET | `/conversations/{conversationId}/attachments/{attachmentId}` | Descarga autenticada del ciphertext; sin SAS ni CDN | 200, 401, 403, 404 |

## 3. Reglas de seguridad

- Sobre ligado a conversación, emisor, destinatario e id de operación; reintentos reutilizan el mismo ciphertext.
- Adjuntos: clave y nonce distintos por archivo, AEAD con conversación + id como datos asociados; la clave viaja dentro del mensaje Signal.
- Un evento SignalR no prueba entrega; la entrega se confirma con `POST /conversations/{id}/delivered`.
- Contenedor Blob con acceso público deshabilitado; el adaptador rechaza uno público.

## 4. Pruebas

`ChatKeyTests` (unitarias), `ChatEncryptionPersistenceTests` (integración con Mongo real: consumo concurrente
de preclaves, revocación, no degradación, autoría al editar/borrar) y `ChatNegotiationTests` (seguridad).
