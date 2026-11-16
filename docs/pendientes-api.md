# Pendientes y trabajo a medias — API

Lista viva de lo que falta o quedó parcial. Se actualiza en cada entrega; el detalle de cada decisión
está en [Memoria.md](../Memoria.md) y el estado por grupo en [estado-desarrollo-endpoints.md](estado-desarrollo-endpoints.md).

Última revisión: 2026-11-16 (rama `feature/08-pedidos`).

## Bloquea el despliegue

| # | Qué | Efecto si no se hace | Dónde |
|---|---|---|---|
| 1 | Configurar `Orders__KeyVaultKeyId` en el App Service `nexolocal` (identificador versionado de `orders-address-kek-dev`; la identidad administrada con `Key Vault Crypto User`). En local: `Orders__AddressEncryptionKeyBase64` en `.env` | **Todo pedido a domicilio responde 503**; los pedidos en el local del prestador sí funcionan | `OrderAddressProtection.cs`, `.env.example` |
| 2 | SDK .NET: `global.json` pide 10.0.302 y la máquina de desarrollo tiene 10.0.403 | No compila sin cambiar temporalmente `rollForward`; instalar 10.0.302 o acordar subir la versión | `global.json` |

| 3 | Poner `ContentSafety__Blocklist=nexo-groserias-es` en el App Service. La lista ya existe en Azure (160 entradas, creada y probada el 2026-11-16) y la variable ya está en el `.env` local. Para cambiar palabras: editar `scripts/blocklist/groserias-es.txt` y correr `python -I scripts/blocklist/sync_blocklist.py` (`--check "texto"` para probar) | En Azure las groserías leves seguirán pasando hasta configurarla | `scripts/blocklist/` |

| 4 | **Push FCM.** Crear/usar el proyecto de Firebase, registrar la app Android, poner `google-services.json` en la app y la cuenta de servicio en `Firebase__CredentialsJson` (App Service y `.env`) | Sin esto no llegan avisos al teléfono; la campana sí funciona | `FcmPushSender.cs`, app `android/app/` |

## Grupos sin implementar (la app muestra datos de ejemplo)

La app en modo `auto` usa datos de ejemplo con aviso "demo" cuando una ruta responde 404 sin código
Nexo. Si se compila con `apiMode=live`, estas pantallas fallan.

| Grupo | Rutas que la app ya llama |
|---|---|
| 15 Pantallas | Ninguna: la app compone sus pantallas con las rutas de cada grupo. Spec sin consumidor hoy |
| Asistente | `POST /v1/assist/draft` (sin spec en `specs/endpoints`) |

## Parcial por grupo

### 08 Pedidos
- **Outbox sin consumidores.** Se guardan `order.created/accepted/rejected/cancelled/resolved/address_changed`, pero nadie envía push, ni reintenta la sincronización con la solicitud si Discovery falló en el momento.
- Pedidos creados antes del 2026-11-15 tienen `category` vacía (la app muestra la genérica). Corregible con un script de una vez.
- `GET /orders` (lista) sigue devolviendo `conversationId: null`; solo el detalle lo trae. La app cae al detalle, no rompe.
- `timeline` sin el campo `text` compuesto; la app lo muestra vacío.
- Sin Polly (timeout/circuit breaker) en la llamada a Discovery; solo los tiempos de Mongo.

### 09 Opiniones
- Lectura con `Cache-Control: public, max-age=60` en lugar de Redis L2 + ETag de la ficha.
- Sin push al prestador por opinión nueva (outbox `review.created` sin consumidor).
- Sin retiro por moderación: todas nacen `approved`. El promedio ya se recalcula completo, así que aguantará el retiro cuando exista.
- Si Discovery falla al guardar el promedio, se corrige hasta la siguiente opinión.

### 10 Mensajes
- **Fotos en el chat no funcionan:** la app sube con `purpose=chat`, que `MediaRules` no acepta (400). Además las imágenes se sirven en URL pública; hace falta media privada antes de habilitarlo.
- Sin hub SignalR (`ws-hubs-chat.md`); `deliveredToSocket` siempre `false`. La app actual consulta por HTTP.
- Sin push de mensaje nuevo (outbox `message.sent` sin consumidor).
- `alsoReport` del bloqueo se ignora: la app abre el formulario de reporte por su cuenta después de bloquear.
- Sin auditoría `audit_events` del bloqueo ni contador `stats.contactClicks`.

### 12 Contacto y reportes
- Nadie revisa los reportes todavía: falta el worker de `report.created` que avise al equipo por correo y oculte el objetivo tras 3 reportes de cuentas distintas, y una pantalla o proceso para que una persona los resuelva (`status`, `resolution`, `reviewedBy`).
- `contact.phone_copied` no incrementa aún `stats.contactClicks` del prestador.
- Sin `audit_events` con cadena de hash (ni aquí ni en el bloqueo del chat).
- Un prestador sin `channels.call` nunca muestra su número, aunque haya pedido aceptado: así lo dice la ficha; confirmar con el equipo si es lo deseado.

### 11 Guardados
- `unavailableReason` siempre `unpublished`; la ficha distingue `deleted` / `unpublished` / `closed`.
- Sin caché.

### 14 Imágenes
- Sin CDN/Front Door en la API: no existe variable ni código que la use; las imágenes salen por `/v1/discovery/media/{id}`.

### 13 Notificaciones
- Push apagado hasta configurar Firebase (ver tabla de despliegue).
- Sin SignalR: la campana se actualiza al abrirla o al refrescar, no en vivo.
- Sin avisos de `work.recommended`, `request.closed` ni `account.security` (tipos de la ficha sin evento que los produzca hoy).
- Al desplegar se procesarán los eventos de outbox acumulados: pueden aparecer avisos antiguos.

## Transversal
- `NexoProblem.Describe` devuelve **500** para cualquier código no mapeado. Están mapeados los de 08, 09, 10 y 11; faltan los de 12 y algunos de uploads (`upload.too_large`, `upload.expired`, etc.) que aún no se usan. Al añadir un código nuevo, añadir su estado y un caso en `NexoProblemTests`.
- El outbox de Engagement ya se consume (notificaciones). Siguen sin consumidor útil: reintentar la sincronización pedido↔solicitud, revisión de reportes (correo/ocultar) y estadística de clics de contacto.
- Política `publish` de rate limit definida pero no registrada; las rutas usan `user`/`chat` más topes diarios propios.
