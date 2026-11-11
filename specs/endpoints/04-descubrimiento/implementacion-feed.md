# Implementación local del feed — 2026-11-11

Estado: `GET /v1/discovery/feed` está conectado en la API modular y consume trabajos aprobados y perfiles publicados existentes. Esta entrada actualiza el estado “feed pendiente” de `docs/estado-desarrollo-endpoints.md` para esta ruta únicamente; no acredita despliegue ni aceptación humana.

## Comportamiento

- Lee `lat`, `lng`, `cursor` y `limit` (1–50, predeterminado 10). El cursor de 32 bytes queda firmado por ámbito mediante un hash de las coordenadas y no puede reutilizarse para otra ubicación.
- Combina publicaciones aprobadas de prestadores cuyo perfil activo atiende el punto con trabajos cuyo `publicGeo` difuminado está a 3 km o menos.
- Omite perfiles no publicados y cuentas inactivas, ordena por UUID v7 descendente y devuelve `nextCursor` y `hasMore`.
- Proyecta el resumen público del prestador, imágenes aprobadas, zona, distancia redondeada, recomendaciones y texto recortado a 300 caracteres. Nunca materializa ni serializa el punto exacto de domicilio, datos de Identity ni datos del cliente.
- Para visitantes `guest=true`, `canContact=false` y no devuelve `saved`/`recommended`. Para sesiones válidas obtiene esas marcas con dos consultas agrupadas por página.
- Limita la consulta geográfica a 3 km y la candidata de perfiles a 1,000; una caída/timeout de Mongo o de Identity falla con 503 explícito. `Cache-Control` separa la respuesta pública de la personalizada; Redis/L2 no está incorporado en este cambio.

## Archivos

- `src/Domains/Discovery/Nexo.Discovery.Endpoints/DiscoveryEndpointMapping.cs`
- `src/Domains/Discovery/Nexo.Discovery.Application/Works/FeedService.cs`
- `src/Domains/Discovery/Nexo.Discovery.Infrastructure/Works/MongoFeedStore.cs`
- Registro de `IFeedStore` y `FeedService` en `DiscoveryInfrastructureRegistration.cs`.

## Verificación

- Build de la solución Release: 0 errores y 0 advertencias.
- `FeedTests`: 4/4 pasan (cursor ligado a punto, límites, filtro de cuentas activas y privacidad del DTO).
- Se agregó una prueba Testcontainers para Mongo con paginación y marcas de recomendación/guardado. No pudo ejecutarse en este entorno porque Docker no está disponible (`npipe://./pipe/docker_engine`, acceso denegado). Su proyecto sí compiló durante `dotnet test`.
- No se probó el endpoint contra Atlas/Azure ni se desplegó la API.

## Dependencias de acciones de tarjeta

- Recomendar/quitar recomendación ya usa `POST/DELETE /v1/discovery/works/{id}/recommend` del grupo 05.
- Guardar/quitar y consultar guardados siguen pendientes en grupo 11: `POST/DELETE /v1/discovery/saved/{kind}/{id}` y `GET /v1/discovery/saved`.
- Contactar por teléfono y reportar siguen pendientes en grupo 12: `POST /v1/engagement/contacts/phone` y `POST /v1/engagement/reports`.
- Compartir y comparar ya usan navegación/enlace local y `GET /v1/discovery/providers/compare`; no son operaciones del feed.
