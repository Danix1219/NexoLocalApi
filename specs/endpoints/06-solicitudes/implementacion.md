# Incremento 06-solicitudes — 2026-11-10

## Spec y decisiones previas
Dueño Discovery, arquitectura nv-09. Entrada: cuentas/perfiles y directorio existentes, moderación por IProviderSafety, Mongo replica set y fronteras de contratos. Cuatro fichas: publicar, mine, opportunities y close; habilitar kind=requests en mapa existente porque ya dispone de fuente real. Sin ampliar a expresar interés, pedidos o reseñas.

- Fichas vigentes gobiernan límites: título 5..90, descripción 10..600, siete categorías y urgencia today/week/noRush; chatCall exige correo confirmado y móvil declarado. Moderación real previa. Imágenes siguen bloqueadas hasta ciclo real de subida/moderación/purga: validar propia/aprobada y fallar 503, sin fingir publicación.
- Persistir punto exacto únicamente en location de service_requests, para filtro geoespacial privado. Modelo público y data no tienen location; Mongo lo excluye antes de materializar cualquier lista. Solo publicPoint difuminado una vez 500–800 m en DTO/evento. El nombre público del autor lo aporta Identity; nunca se expone teléfono, correo ni contacto privado.
- Transacción local para solicitud, recibo mínimo, cuota durable 10/día UTC y evento. Misma decisión de trabajos: contador Mongo transaccional sustituye el Redis histórico de cuota para evitar doble consumo/rollback incompleto. Rate limit HTTP/Redis existente sigue activo. Reintento idéntico conserva solicitud/punto; otro payload 409.
- Oportunidades: categorías/cobertura del perfil real, nunca del modo del token; no propias, solo open/aprobadas y cuentas activas. Perfil local radio 0 usa 10 km. Texto AND sin acentos mediante campo normalizado y regex escapadas en Mongo; Atlas Search aún no configurado. Presupuesto 1000 candidatos/5 s; 503 si excede, nunca total truncado. Orden urgencyRank y UUID canónico descendente, cursor <=64 ligado a cuenta, perfil/filtros.
- ReachableProviders: agregado real de perfiles publicados de categoría que sirven el punto y cuentas activas, sin revelar IDs. No intereses automáticos.
- Resumen por contrato de Engagement (lectura de su propia base), nombre/foto públicos por contrato de Identity; Host compone, Discovery no referencia ni consulta bases ajenas. Resumen con timeout 2 s y circuito; degradar a interestCount almacenado y preview vacío, marcar disponibilidad; iAmInterested null si no pudo comprobarse. Identity sigue siendo la autoridad del nombre público.
- Cierre atómico id+clientId+estado. Repetido devuelve 409 request.already_closed con fecha original, como ficha (idempotencia de efecto). pickedProviderId declarado es métrica privada, nunca asigna ni habilita reseña. canReview false hasta evidencia autorizada del futuro flujo de pedidos; no confiar en la declaración del cliente.
- Sin caché compartida/personal ni contadores inventados. Eventos request.created/request.closed durables, no se afirma push, cancelación de pedidos o cierre distribuido de intereses sin consumidores implementados. assigned se lee si llega de un flujo autorizado posterior; este incremento no permite que cliente lo asigne.

## Plan
Domain/ServiceRequest.cs; Application/Requests reglas, DTO/puertos/casos; Infrastructure/Requests/MongoRequestStore; Endpoints/RequestEndpoints y conexión del mapa existente. Identity.Contracts/Infrastructure aporta el nombre público; Engagement.Contracts/Infrastructure aporta resumen limitado de intereses. Host conecta ambos a puertos Discovery, sin referencias entre internals. Pruebas en Discovery.Unit, Integration y Seguridad.Tests, más Architecture.Tests. Sin paquetes/proyectos nuevos; sin móvil, secretos o despliegue.

## Tareas y criterios
- FR-991: Publicar y consultar propias/oportunidades reales, con categoría, contacto, urgencia y radio.
- FR-992: Cerrar por dueño e integrar mapa/resumen de intereses sin inventar pedidos o elegibilidad.
- NFR-991: Privacidad, cuota y reintento atómicos, dependencias aisladas, cursor y fallos explícitos.
- AC-991: RequestRulesTests/RequestEndpointsTests: límites, contacto, moderación, JWT, propiedad y estado.
- AC-992: RequestPersistenceTests: concurrencia/idempotencia/cuota, rollback, geoespacial y proyección sin punto exacto.
- AC-993: Contratos/resúmenes/nombre público, aislamiento, cuentas inactivas, degradación, cursor y mapa con solicitudes cerradas ocultas.
- T-991 → FR-991/FR-992/NFR-991: implementar subset y verificar AC-991/992/993 en capas existentes; conciliar spec/plan/tareas/gates/Memoria sin firma humana automática.

## Evidencia
Verificado el 2026-11-10: solución Release y regresión completa con cobertura, salida 0, 228 pruebas aprobadas: Architecture 7, Common 16, Discovery 61, Identity 4, Integration 44 y Seguridad 96. Engagement.Unit permanece sin casos; el contrato Engagement tiene integración real en RequestContractsTests. Incremento de 31 casos: 14 unitarios, 8 de integración/contratos y 9 HTTP/seguridad.

Comando: `dotnet test src/Nexo.sln -c Release --no-restore -m:1 --collect:"XPlat Code Coverage" --logger "trx;LogFileName=requests-verified.trx"`. Log fuera del proyecto en artifacts/requests-verified.txt; TRX y Cobertura en TestResults de cada capa. Mongo replica set, Redis y JWT reales en Testcontainers; doble explícito solo para moderación y fallos externos. Compilación sin errores/advertencias. La prueba HTTP corrigió el cierre sin cuerpo (JsonElement Undefined), luego regresión completa aprobada.

check_sdd_integrity.py: SDD-INTEGRITY-OK, 26 specs, 489 tareas, 481 requisitos, 334 criterios. git diff --check: salida 0 (avisos CRLF preexistentes). check_sdd.ps1 sigue fallando por enlaces históricos externos ausentes (docs_nexo, API_NexoLocal, App_NexoLocal); no se declara SDD-OK global ni aceptación humana.

API local iniciada en http://127.0.0.1:5188; /health/live 200 y OpenAPI registra las cuatro operaciones. Mine/opportunities/mapa requests anónimos devuelven 401. Redis y emulador de colas existentes se encendieron; Mongo no responde con la configuración actual y ready no está aprobado. No se cambiaron conexiones/secretos ni se crearon cuentas o solicitudes reales. No es validación cloud.

Fuentes técnicas: [MongoDB geoNear](https://www.mongodb.com/docs/manual/reference/operator/aggregation/geonear/) y [transacciones C#](https://www.mongodb.com/docs/drivers/csharp/current/crud/transactions/).


Actualizaci?n 2026-11-10: el incremento de [medios reales](../14-imagenes-cdn/implementacion.md) sustituye el bloqueo hist?rico de im?genes de este documento. Se admiten ?nicamente uploads propios aprobados para el prop?sito correspondiente; sin CDN ni chat privado.
# Incremento 2026-11-10: edición y galería

Se añade [PATCH de solicitud propia](patch-requests-id.md) y hasta seis fotos aprobadas, con `imageUrls` y compatibilidad de `imageUrl`. El frontend muestra nombre/foto de la cuenta actual y carrusel con contador. La edición conserva categoría y ubicación, revalida contenido y fotos, y solo acepta solicitudes abiertas.

