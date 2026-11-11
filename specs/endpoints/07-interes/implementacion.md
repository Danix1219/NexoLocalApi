# Incremento 07-interes — 2026-11-10

## Alcance y archivos
Se implementan las tres fichas bajo Engagement: `POST /v1/engagement/requests/{id}/interest`, `DELETE /v1/engagement/requests/{id}/interest` y `GET /v1/engagement/requests/{id}/interested`. Application/Interests concentra casos; Infrastructure/MongoInterestStore mantiene la persistencia; EngagementEndpointMapping expone HTTP; Host compone contratos de Discovery y moderación. Los IDs persistidos de prestador son IDs del perfil, distintos del accountId del JWT.

## Comportamiento
- Expresar interés requiere cuenta activa, perfil publicado, pedido abierto/visible en radio y nota moderada (máximo 200). No permite auto-interés. La repetición activa devuelve 200; primera expresión o reactivación, 201. No habilita chat.
- Retirada marca `withdrawnAt`, es idempotente y rechaza interés asignado o pedido activo.
- Interesados solo para el dueño; 404 no distingue inexistente/ajeno, filtra retirados, ordena por rating/recomendaciones/disponibilidad/reciente, pagina con cursor y entrega ficha pública y acciones.
- Mongo mantiene índice único `{requestId, providerId}`. Expresión/reactivación, retirada y su evento (`interest.expressed` / `interest.withdrawn`) se guardan en la misma transacción local del replica set.
- `GET` entrega `Cache-Control: private, no-store`; JWT y rate limit `user` en las tres rutas.

## Verificación T-992
- El puente Host valida una solicitud por ID con `$geoNear`, limitación por radio y proyección segura; permite leer estado `assigned` solo para devolver el conflicto contractual y no lo incorpora al feed.
- Discovery está protegido por timeout de 2 s, circuito de 30 s tras tres fallos y bulkhead local de 64 operaciones concurrentes. Se usa `CancellationTokenSource`, `SemaphoreSlim` y un guard de circuito propio del Host para mantener la política explícita y sin añadir un paquete de resiliencia.
- Códigos HTTP para `interest.own_request` (403), `interest.request_assigned` y `interest.cannot_withdraw_assigned` (409) están mapeados al catálogo común.
- Se prueban perfil ausente, propia, radio, moderación rechazada, asignada, retiro con pedido activo, retiro idempotente, cursor/paginación, outbox sin duplicados, timeout/cancelación, circuito, bulkhead y fallo de Discovery (503 sin persistir).

## Límites
El outbox queda durable, pero no hay consumidores para contadores/estadísticas ni notificaciones; corresponden a bloques posteriores y no son condición de T-992. Pedidos y conversaciones reflejan únicamente lo que exista al implementar 08/10. El `.env` contiene las dos claves de Content Safety, pero estas pruebas sustituyen la moderación por un doble controlado; no se hizo llamada a Azure ni se verifica la validez de las credenciales. Revisión humana y aceptación en entorno real siguen pendientes.

## Evidencia local
- `dotnet test tests/Nexo.Seguridad.Tests/Nexo.Seguridad.Tests.csproj -c Release --no-restore -m:1 --filter "FullyQualifiedName~InterestResilienceTests|FullyQualifiedName~RequestEndpointsTests"`: 19/19 aprobadas; incluye Mongo replica set y Redis Testcontainers para HTTP, visibilidad, outbox, pedido activo y paginación; pruebas unitarias cubren timeout, cancelación, circuito y bulkhead.
- La compilación de `Nexo.Host` y `Nexo.Seguridad.Tests` se completó en esa ejecución, sin errores ni advertencias reportadas.
- `python scripts/check_sdd_integrity.py`: `SDD-INTEGRITY-OK`, 26 specs; 491 tareas, 485 requisitos y 338 criterios, grafo local solamente.
- `scripts/check_sdd.ps1` se ejecutó y falla por cientos de referencias a documentos hermanos ausentes en este checkout (`docs_nexo`, `App_NexoLocal`, `API_NexoLocal`); no señala enlaces rotos en los archivos modificados de 07-interés.
- No equivale a regresión .NET completa, llamada real a Azure, despliegue ni aceptación humana.
