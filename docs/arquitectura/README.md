# Arquitectura por dominio

> **Mapa de dominios, no de despliegue.** La **enmienda E-001** sustituyo los ocho microservicios por una API REST modular por dominios: un host, tres modulos (`Identity`, `Discovery`, `Engagement`) mas `Screens`, y una app con **2 instancias**. Los identificadores `S01`-`S08` de las fichas de abajo se conservan como mapa de dominios y su contenido funcional sigue vigente. Estructura real: [estructura-backend.md](../estructura-backend.md).


La arquitectura interna se elige por responsabilidad y complejidad. La [selección revisada, alternativas y criterios de eficiencia](seleccion-arquitecturas.md) define el estado vigente y sustituye la plantilla de cuatro capas para todos.

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

## Estado físico

La solución original `nexolocal.Api.slnx` tiene 28 proyectos y conserva G01 en `nexolocal.Api/`. Los servicios mantienen `services/<propietario>/src/<proyecto>/`; cada uno tiene Api propio y S07 dispone de Worker separado. Los proyectos y referencias varían según el perfil; los diez hosts están vacíos. S08 sigue condicional.

Los proyectos de núcleo no dependen de proveedores o transporte; los adaptadores dependen del núcleo y el host compone. Datos y contratos siguen siendo propios por servicio. No hay referencias de proyectos entre propietarios. La política de referencias se verifica por arquitectura, no por una plantilla común.

## Perfiles y autoridades

- [G01](servicios/g01-gateway.md)
- [S01](servicios/s01-identity.md)
- [S02](servicios/s02-directory.md)
- [S03](servicios/s03-community.md)
- [S04](servicios/s04-orders.md)
- [S05](servicios/s05-messaging.md)
- [S06](servicios/s06-trust.md)
- [S07](servicios/s07-notifications.md)
- [S08](servicios/s08-relations.md)

Leer [políticas comunes](politicas-comunes.md), [estructura backend](../estructura-backend.md), [orden SDD](../orden-desarrollo-backend.md) y [decisiones](../decisions/registro.md). El [registro de perfiles](perfiles.json) conserva cobertura de las 99 operaciones de S01–S08; las dos de salud son transversales. Las 101 operaciones siguen pendientes de contrato formal.

Carpetas y arquitectura no acreditan negocio implementado, rendimiento mejorado o aceptación humana. Las historias concretan contratos, permisos, atomicidad y evidencia antes de Ready.

[Resultados de compilación, referencias y arranque de esta revisión](../inicio-backend/revision-arquitecturas.md).

[Ubicación versionada de la solución y publicación](../inicio-backend/consolidacion-publicacion.md).
