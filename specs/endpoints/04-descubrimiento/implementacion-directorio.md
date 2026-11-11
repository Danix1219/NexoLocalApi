# Incremento 04 — directorio público (2026-11-10)

## Spec y decisiones antes de construir
Dueño: Discovery, arquitectura nv-06. Entradas: perfiles publicados del grupo 03, contrato mínimo de estado de Identity y ubicación del grupo 04. El encargo autoriza implementación local; revisión humana y publicación no inferidas.

Rutas: GET /v1/discovery/categories, /search/suggest, /search/providers y /map/nearby (kind=providers). Feed y solicitudes no se inventan. kind=requests exige sesión/perfil activo y devuelve 503 hasta existir la fuente del grupo 06.

Refinamiento vinculante para este incremento sobre las cuatro fichas:
- Catálogo versionado en código y sembrado idempotentemente en Mongo (categories). Mantener slugs aceptados por ProviderRules: seis grupos de cinco especialidades; otros sin especialidades predefinidas. Colores/etiquetas de la maqueta vigente, no los ejemplos antiguos. Sin keywords públicas.
- Búsqueda AND sin acentos y por fragmentos sobre nombre/bio/activity/trades/categorías/especialidades y sus etiquetas. Adaptador Mongo 2dsphere para candidatos; reglas y estrategias en Application. Atlas Search/Redis de resultados quedan pendientes: no depender de índices cloud inexistentes ni afirmar caché distribuida activa.
- Lectura acotada a 1000 candidatos y cinco segundos en Mongo: si supera el presupuesto, 503 explícito, nunca total truncado o éxito vacío. Con punto, candidatos normales hasta 20 km; expanded consulta fuera solo si no hay coincidencias en zona. Pendiente sustituir por proyección/índice especializado antes de escalar el directorio.
- Normal: distancia <= radio elegido Y sirve el punto (radio prestador o local hasta 10 km). expanded solo si no hay ninguno en zona, orden cercano y aviso explícito. Sin punto no filtrar zona ni fingir distancia; near/shop requieren punto.
- Horarios reales America/Mexico_City, no 9:41 fijo. Cuentas inactivas excluidas mediante contrato Identity por lote (hasta 1000), sin consulta por prestador ni acceso desde Discovery a su base. Sin caché compartida para evitar bajas obsoletas y consultas de ubicación compartidas: private, no-store en búsqueda/mapa/sugerencias. Catálogo público con ETag/304.
- Cursor compacto <=64: ancla providerId + hash SHA256/128 de filtros normalizados, punto exacto, radio, orden y expansión. No offset. Si desaparece el ancla, 400 cursor_invalid; no snapshot de cambios concurrentes.
- Mongo elimina locationGeo, location de home, creación/eventos y datos privados antes de materializar candidatos. Mapa solo publicZone.point persistido para home y local publicado para shop/both. Distancias públicas en pasos de 0.5 km; no publicar distancia precisa.

## Plan
Application/Directory: catálogo, consulta/validación, normalización, cursor, estrategias, servicio y puerto de candidatos. Infrastructure/Directory: semilla Mongo; reutilizar MongoProviderStore con interfaz adicional de consulta pública. Endpoints/DirectoryEndpoints: cuatro rutas anónimas con rate limit public y errores de contrato. Registrar en infraestructura y mapping actual sin paquetes ni proyectos nuevos. Identity.Contracts y su adaptador agregan ReadManyAsync; Host/ProviderAccountsBridge compone estados mínimos y traduce indisponibilidad sin exponer contactos.

## Tareas y aceptación
- FR-991: catálogo y sugerencias públicas reales; AC-991: siete categorías, slugs compatibles, 304, orden especialidad/categoría/prestador, consultas cortas sin directorio.
- FR-992: búsqueda y mapa de perfiles publicados; AC-992: AND/acento, filtros, servicio/radio, expansión, tres órdenes, cursor y conteo/puntos limitados.
- NFR-991: privacidad/aislamiento/fallos acotados; AC-993: bajas ocultas, home sin coordenadas privadas, cursor ajeno inválido, límites/503 y permisos de requests.
- T-991 → FR-991/FR-992/NFR-991: preparar, implementar y verificar AC-991/992/993 en capas existentes Discovery.Unit, Integration, Seguridad.Tests y Architecture.Tests. No sustituir integración Mongo por dobles ni afirmar moderación externa probada.

## Verificación prevista
dotnet build src/Nexo.sln -c Release -m:1; dotnet test de las cuatro suites existentes; scripts/check_sdd.ps1 y python -B scripts/check_sdd_integrity.py. Registrar resultados reales al cierre. Docker real para integración/seguridad; dobles solo en puertos externos/estados para casos unitarios. Sin móvil, secretos, despliegue, firma humana o cloud.

## Evidencia
Ejecución de suites con cobertura: `dotnet test src/Nexo.sln -c Release --no-restore -m:1 --collect:"XPlat Code Coverage" --logger "trx;LogFileName=directory-verified.trx"`, seguida de regresión completa de `tests/Nexo.Seguridad.Tests/Nexo.Seguridad.Tests.csproj` con las mismas opciones.

170 casos aprobados en el resultado final por capas: Architecture 7, Common.Unit 16, Discovery.Unit 34, Identity.Unit 4, Integration 30 y Seguridad.Tests 79. Incremento de 31 casos frente a los 139 previos: 15 unitarios, 3 de persistencia Mongo y 13 HTTP/seguridad. Engagement.Unit conserva cero casos y no se cuenta como cobertura. TRX y cobertura en TestResults de las suites existentes; logs `C:/Users/kevin/nexolocal_api/artifacts/directory-verified.txt` y `directory-security-verified.txt`.

La primera ejecución sin cobertura aprobó los 170 casos. En la ejecución con cobertura hubo 12 fallos de la clase de seguridad previa por agotar entre hosts el bucket Redis de la IP compartida. Se reutilizó TestClientAddress en HostDeSeguridad para aislar clientes, sin desactivar ni elevar límites reales. La regresión posterior de seguridad valida los 79 casos y sustituye ese resultado inicial; no se presenta el log con fallos como ejecución verde.

AC-991: catálogo compatible/ETag/304, prefijos y consulta corta. AC-992: filtros AND/acento, radio/serves/expansión, orden estable/paginación, límite de puntos separado del total. AC-993: bajas reales por contrato Identity, proyección Mongo sin home/owner/contacto/creación, 400/401/403/503 y rechazo por más de 1000 candidatos. Moderación es ajena a estas lecturas; las pruebas de datos no certifican Content Safety, Gmail ni Atlas Search.

Integridad documental: `SDD-INTEGRITY-OK`, 26 specs, 487 tareas, 475 requisitos, 328 criterios. `check_sdd.ps1` conserva enlaces históricos rotos a docs_nexo/App_NexoLocal ausentes; no se declara SDD-OK global. Revisión humana pendiente.

Fuente técnica consultada: [MongoDB $geoNear](https://www.mongodb.com/docs/manual/reference/operator/aggregation/geonear/) exige primera etapa e índice geoespacial. Adaptador usa locationGeo ya existente y proyecta antes de materializar; Testcontainers comprueba ejecución, no Atlas Search.

## Comprobación local final
`dotnet build src/Nexo.sln -c Release --no-restore -m:1`: cero errores y advertencias. `git -c core.safecrlf=false diff --check`: 0. API reiniciada con perfil Api en http://127.0.0.1:5188. Respuestas HTTP 200 verificadas en health/ready y las cuatro rutas nuevas, usando la configuración operativa existente. No se creó una cuenta o perfil real para estas consultas; GET categories inicializa únicamente su semilla versionada de referencia. No se afirma despliegue ni validación de Atlas Search.

## Actualización del 2026-11-10
El [incremento 06-solicitudes](../06-solicitudes/implementacion.md) aporta la fuente real de kind=requests. Sustituye únicamente el 503 temporal documentado arriba; feed y demás consumidores posteriores conservan sus pendientes.
