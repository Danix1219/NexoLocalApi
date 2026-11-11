# Incremento 03 — perfiles de prestador

Estado: construcción local autorizada por el usuario; sin aprobación humana ni despliegue.

## Spec y dependencias

Entradas: cinco fichas de esta carpeta, Identity/Cuenta implementados, Mongo y Redis existentes. Propietario Discovery. FR-P01 alta única e idempotente; FR-P02 lectura propia y pública separadas; FR-P03 edición de siete secciones; FR-P04 comparación de dos perfiles; NFR-P01 privacidad/BOLA/baja; NFR-P02 moderación cerrada ante fallo y escrituras atómicas. No adelantar 04–15 ni crear nube.

Conciliaciones antes de construir: correo confirmado no valida celular. El reloj de horarios es real, zona America/Mexico_City (no 9:41 de maqueta). Completitud propia: base 35 + foto 7 + horario 14 + precios 14 + trabajos 14 + condiciones 9 + portada 7, acotada a 100; alta usa esta misma fórmula, evitando dos porcentajes incompatibles. Zona aproximada sin geocodificación: etiqueta genérica y desplazamiento servidor de 500–800 m, estable mientras no cambie el punto.

Primer incremento sin caché de perfiles: no-store y ETag del cuerpo completo; evita datos retirados o coordenadas de otra petición mientras no haya invalidación distribuida. Se conserva rate limit Redis. Contadores de Engagement no implementados: interestsGiven null, sin inventar estadísticas. Proyecciones se consultan mediante contrato de Identity para comprobar baja y correo actuales; Discovery nunca escribe la base de Identity.

## Plan

Domain: modelos, catálogo, horarios/completitud/geografía. Application: validación, puertos y casos de uso. Infrastructure: Mongo keyed, moderación HTTP, subidas aprobadas propias. Endpoints: contratos y traducción de errores. Identity expone contrato mínimo; composición en Host, sin referencias a internos de otros módulos. Alta guarda llave/hash y evento en el mismo documento; el índice único accountId cierra carreras. Cambio parcial por sección y versión evita pérdida concurrente; evento durable en el mismo update. La propagación externa de esos eventos queda pendiente, no se declara entregada.

## Tareas y aceptación

- T-P01 → FR-P01/NFR-P02: alta, validación, moderación, idempotencia. AC-P01: crear/repetir/conflicto/fallo sin persistencia.
- T-P02 → FR-P02/NFR-P01: dueño por sub, proyección pública antes de salir de Mongo. AC-P02: no teléfono/email/accountId/punto home y baja oculta.
- T-P03 → FR-P03/NFR-P02: siete secciones, límites, imágenes propias, concurrencia. AC-P03: rechazar campos ajenos, horarios inválidos y llamada sin móvil; preservar otras secciones.
- T-P04 → FR-P04/NFR-P01: dos UUID distintos, orden, filas objetivas. AC-P04: anónimo/duplicados/inexistentes/orden/privacidad.
- T-P05 → todos: pruebas en Nexo.Discovery.Unit, Nexo.Integration y Nexo.Seguridad.Tests actuales, build y regresión; registrar resultados reales al cierre.

## Checklist

Ejecución local registrada al final de este documento. Moderación real en Azure, almacenamiento/CDN, aceptación humana y despliegue no se acreditan mediante dobles de pruebas.

## Decisiones de persistencia y operación

El documento de perfil contiene `data` (modelo tipado serializado), `locationGeo` (GeoJSON privado), accountId, revisión, llave/hash/respuesta original de alta y eventos. Índices únicos por cuenta y 2dsphere. La llave queda ligada durablemente al alta y devuelve la respuesta original aunque después se edite el perfil. Un evento y su cambio se guardan juntos; no se afirma entrega a Storage Queue. El buffer tiene límite 256 eventos: al llenarse rechaza cambios con 409 en lugar de perder eventos. Su despachador y purgas CDN permanecen pendientes. No se instala validator $jsonSchema de colección en este incremento: validación en aplicación y pruebas de contrato; migración de colecciones requiere fase posterior.

Visitas: incremento agregado atómico en Mongo después de una consulta pública visible; ni consulta propia ni comparación incrementan visitas. `contactClicks` e `interestsGiven` son null mientras no existan sus proyecciones. No se utiliza hora fija del prototipo. Si se solicita distancia, Mongo la calcula sin entregar el punto exacto home; salida redondeada a una décima.

Catálogo inicial de especialidades: slugs controlados en ProviderRules (fugas/calentadores/destapes/tinacos/banos para plomería, y sus equivalentes de las otras cinco categorías; otros sin especialidades). El bloque 04 deberá exponer este catálogo autoritativo antes de habilitar formularios dependientes de él; no se aceptan etiquetas arbitrarias como slugs.

Adaptador de texto contrastado con [Azure REST 2024-09-01](https://learn.microsoft.com/en-us/rest/api/contentsafety/text-operations/analyze-text?view=rest-contentsafety-2024-09-01). Respuesta incompleta, duplicada o con severidad inválida devuelve 503; rechazo de contenido devuelve 403. En pruebas se distingue el doble HTTP del servicio real.

## Verificación ejecutada — 2026-11-09

127 pruebas aprobadas: 7 arquitectura, 16 Common.Unit, 14 Discovery.Unit, 26 Integration y 64 Seguridad.Tests. Son 30 casos nuevos sobre los 97 anteriores; Identity.Unit y Engagement.Unit siguen sin casos y no se cuentan como cobertura.

- `dotnet test src/Nexo.sln -c Release -m:1 --collect "XPlat Code Coverage"`: aprobado. Compilación incluida sin errores; TRX y cobertura Cobertura en `artifacts/profiles-verified/`.
- `python -B scripts/check_sdd_integrity.py`: SDD-INTEGRITY-OK, 26 specs, 484 tareas, 468 requisitos, 323 criterios. Solo integridad documental.
- `scripts/check_sdd.ps1`: falla por enlaces históricos a docs_nexo/App_NexoLocal ausentes; no se declara SDD-OK.
- `git diff --check`: aprobado. Sin commit, push ni despliegue.

AC-P01: alta/repetición/llave reutilizada, respuesta original tras editar y doble alta en HTTP; índice único en integración. AC-P02: aislamiento de dueño, proyección home antes de materializar, baja invisible y ETag. AC-P03: siete secciones, horarios inválidos, límites, teléfono declarado, propiedad/finalidad/aprobación de imágenes y CAS. AC-P04: exactamente dos IDs, orden, errores uniformes, null explícito en best y precisión de centavos.

Matriz de suites: `tests/Nexo.Discovery.Unit/ProviderRulesTests.cs`, `tests/Nexo.Integration/ProviderPersistenceTests.cs`, `tests/Nexo.Integration/ProviderSafetyTests.cs` y `tests/Nexo.Seguridad.Tests/ProviderEndpointsTests.cs`. Proyectos actuales; no se crean capas de pruebas paralelas. Mongo/Redis de Testcontainers reales, moderación HTTP doblada explícitamente, sin acreditar Azure/Atlas/Gmail reales.

T-P01 a T-P05 implementadas para este subset con evidencia local. Aceptación humana, recorrido móvil, caché/invalidación, catálogo público, despacho de eventos, purgas y validator de colección permanecen pendientes según límites anteriores.


Actualizaci?n 2026-11-10: el incremento de [medios reales](../14-imagenes-cdn/implementacion.md) sustituye el bloqueo hist?rico de im?genes de este documento. Se admiten ?nicamente uploads propios aprobados para el prop?sito correspondiente; sin CDN ni chat privado.
