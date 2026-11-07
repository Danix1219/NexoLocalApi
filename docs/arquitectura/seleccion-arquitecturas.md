# Arquitectura interna por caso de uso

> **Documento historico.** Describe la estructura de ocho microservicios que la **enmienda E-001** retiro el 2026-11-06. Ya no refleja lo que hay en disco: no existen `nexolocal.Api.slnx`, `services/` ni los hosts por servicio. Se conserva como evidencia del trabajo ejecutado y de por que se cambio. **Estado vigente:** [estructura-backend.md](../estructura-backend.md) y [mapa de specs](../../specs/arquitectura/README.md).


Revisión aplicada al scaffold el 2026-11-03, por encargo del usuario. Elección razonada según las operaciones e invariantes del inventario, sin afirmar que exista una arquitectura universalmente mejor. Proveedores/contratos y aceptación funcional siguen pendientes. Se mantienen los ocho límites de servicio y G01, la solución original y los diez procesos previstos.

| Propietario | Arquitectura interna | Proyectos | Patrones asociados |
|---|---|---|---|
| G01 | Gateway de borde | nexolocal.Api | Enrutamiento, composición acotada, aislamiento por dependencia |
| S01 Identity | hexagonal | Domain, Application, Infrastructure, Api | Rotación atómica, revocación durable, outbox y recuperación hacia adelante. |
| S02 Directory | vertical-slices | Features, Adapters, Api | Separación lógica de consulta/comando; cache-aside solo público, índices y proyecciones por contrato. |
| S03 Community | feature-modules | Modules, Adapters, Api | Módulos locales, moderación previa, unicidad idempotente y proyecciones de métricas. |
| S04 Orders | ddd-hexagonal | Domain, Application, Infrastructure, Api | Agregados y máquina de estados, versión optimista, idempotencia y transactional outbox. |
| S05 Messaging | ports-adapters-realtime | Core, Adapters, Api | Comandos/consultas, secuencia por conversación, deduplicación y fan-out posterior al commit. |
| S06 Trust | evaluation-pipeline | Evaluation, Adapters, Api | Pipeline de evaluación con Policy/Strategy; decisión vinculada al contenido y adaptadores reemplazables. |
| S07 Notifications | event-driven-delivery | Delivery, Adapters, Api, Worker | API y Worker separados, inbox durable, planificación, reintentos acotados y cola de errores. |
| S08 Relations | read-projections | Projections, Adapters, Api | Vista materializada, consumidores idempotentes y control de versiones/tombstones. |

## Qué cambia en la estructura

S01 conserva puertos/adaptadores con Domain, Application, Infrastructure y Api: sesiones, verificación y borrado requieren reglas que puedan probarse aisladas. S04 conserva esa dirección de dependencias con DDD táctico para pedido/asignación y sus transiciones. Compartir esa forma en ambos es deliberado; no obliga a todos los servicios a usarla.

S02 agrupa cada funcionalidad en Features. Un slice contiene sus entradas/salidas, validación, autorización y modelo local cuando se implemente; los puertos externos siguen aislados. Catálogo y búsqueda no requieren la misma maquinaria transaccional de pedidos. ContactPolicy permanece explícito porque el directorio también maneja visibilidad y acceso a teléfono.

S03 organiza módulos Posts/Reviews/Reactions/SavedItems/Feed/Metrics dentro de Modules. Cada módulo conserva reglas propias y contratos internos; las composiciones cruzadas usan capacidades explícitas, sin acceso libre a colecciones ajenas. Un solo ensamblado reduce separación física innecesaria; los límites internos entre módulos requieren revisión y pruebas de código cuando exista implementación. El verificador de ProjectReference no los demuestra.

S05 separa Core durable (conversaciones, membresía, comandos y consultas) del fan-out de Adapters y el transporte Api/Http/WebSocket. El socket es efímero; el historial y permisos no dependen de una conexión abierta. No se añade un actor framework, otro worker o almacén por diferenciarse.

S06 concentra en Evaluation el pipeline y las políticas; Adapters implementa moderación, medios y asistencia. El pipeline evalúa el conjunto de controles requerido por tipo/finalidad; una estrategia elige proveedor según configuración aprobada. No es una cadena que apruebe al obtener el primer resultado favorable. Informes/admin y borradores son capacidades explícitas del mismo dueño, sin publicar borradores automáticamente.

S07 comparte Delivery entre Api y Worker; contiene planificación, intentos y puertos. El worker ejecutará procesamiento durable después de implementar su contrato. Separar procesos permite controlar latencia y fallos de entrega sin bloquear registro/consulta; no prueba escalado por sí mismo.

S08 usa Projections para modelos derivados, consulta, actualización y reconstrucción; Adapters contiene el almacén y la entrada de eventos. No crea un Domain transaccional ficticio. Sigue condicional: no se habilita ni aprueba Neo4j por compilar.

## Alternativas y criterios para volver a decidir

| Caso | Alternativa descartada en esta fase | Coste asumido y señal para revisar |
|---|---|---|
| S01/S04 | CRUD sin núcleo de reglas | Más proyectos, a cambio de probar invariantes y concurrencia; revisar si las reglas cambian de dueño |
| S02 | Cuatro capas obligatorias o consultas desde endpoints | Slices con puertos: vigilar duplicación; extraer regla compartida solo dentro del propietario cuando exista repetición real |
| S03 | Un microservicio por reacción/reseña | Módulos en un ensamblado requieren disciplina; separar despliegue solo con autonomía, carga y frontera de datos demostrables |
| S05 | Persistir y autorizar dentro del WebSocket | Core añade mapeo, pero permite probar reconexión y permisos sin transporte; medir fan-out, lag y latencia de commit |
| S06 | SDK de proveedor repartido por endpoints | Pipeline/puertos añaden coordinación; medir latencia por etapa y mantener rechazo seguro ante fallo |
| S07 | Enviar push en la transacción HTTP del productor | Consistencia eventual y operación de reintentos; medir edad de cola, tasa de entrega y errores permanentes |
| S08 | Consultas de grafo como autoridad de permisos | Proyección puede estar atrasada; medir lag y contrastar permisos con el dueño antes de devolver datos sensibles |

No se exige CQRS físico, event sourcing, MediatR, framework de actores ni un broker nuevo. Comandos y consultas se separan lógicamente donde aportan claridad. Persistencia, atomicidad local, idempotencia, moderación previa y privacidad mantienen las políticas comunes.

## Eficiencia verificable

Antes de elegir índices, cachés, paralelismo o escalado: fijar escenario, volumen y objetivo acordado; registrar latencia p50/p95/p99, throughput, errores y recursos. Medir hit ratio y frescura en S02, tiempo de confirmación y conflictos en S04, persistencia/fan-out/reconexión en S05, tiempos por etapa en S06, lag/reintentos en S07 y lag/reconstrucción en S08. No inventar umbrales ni llamar más rápida a la nueva estructura sin comparación reproducible.

La revisión reduce de 33 a 28 proyectos eliminando capas artificiales; no representa una mejora de tiempo de respuesta medida. No cambia las 101 operaciones ni sus dueños. Los hosts permanecen vacíos; la seguridad funcional e integración requieren implementación real.

## Fuentes y alcance de la recomendación

La selección concreta es una decisión de diseño de Nexo Local basada en su inventario. Como fundamento, Microsoft distingue [microservicios con reglas complejas y diseños sencillos](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice), explica [CQRS y sus costes](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) y las [vistas materializadas](https://learn.microsoft.com/en-us/azure/architecture/patterns/materialized-view). No se transfieren SQL/EF, Azure o tecnologías de sus ejemplos como requisitos del proyecto.
