# PLAN — nv-01

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Delimitar G01/S01-S07, propietarios de datos, entornos y configuración; preparar primera vertical sin construir todos los servicios.

Dependencias: nv-00.

## 2. Diseño técnico

API: inventario de bases por servicio y configuración sin secretos. App: zona pública, preferencias, sesión y caché privada separadas; token fuera de SQLite.

Autoridad: [contratos](../../docs/contracts/README.md). [Acuerdo API–app](../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. Documentar arranque, configuración faltante y fallo de dependencia del backend, identidades internas y propietarios sin afirmar proyecto creado; la UI GPS corresponde a app.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Cada comando crítico tiene frontera transaccional local; evento versionado incluye eventId, sourceVersion y correlación mínima, sin PII; reentrega, fuera de orden, cola de error y reconciliación tienen comportamiento definido.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-089 | FR-289 → T-289 → AC-289 | Actor Infraestructura interna; datos `outbox_inbox`; No consultar todas las dependencias en liveness Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-090 | FR-290 → T-290 → AC-290 | Actor Infraestructura interna; datos `outbox_inbox`; Comprobar dependencias indispensables del servicio, no datos de usuario Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| G01 Entrada y composición | FR-601 → T-601 → AC-601 | Delimitar G01 con REST y upgrade WebSocket, dependencias S01-S08 (S08 condicionado) y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |
| I01 Eventos e idempotencia | FR-610 → T-610 → AC-610 | Delimitar I01 con Eventos JSON versionados, dependencias Todos los propietarios y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |
| R03 Cinco componentes del proyecto | FR-403 → T-403 → AC-403 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
