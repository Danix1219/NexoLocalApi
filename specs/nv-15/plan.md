# PLAN — nv-15

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir S08 como proyección pública opcional de dos saltos, reconstrucción e invalidación; GraphQL lectura acotada si se adopta.

Dependencias: nv-08, nv-12; habilitación condicionada a consultas/costo y aprobación.

## 2. Diseño técnico

Neo4j derivado, nunca autoridad de permisos/PII. eventId/sourceVersion/snapshot y tombstones; retiro de aristas al editar/borrar. Operaciones GraphQL registradas, profundidad/costo/paginación acotados.

Autoridad: [contratos](../../docs/contracts/README.md). [Acuerdo API–app](../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. Grafo caído no bloquea funciones obligatorias; replay antiguo no revive autor borrado; cambiar destinatario elimina relación anterior; consulta excedida se rechaza.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Eliminar autor y reejecutar evento antiguo no revive relación; indisponibilidad no bloquea directorio/pedidos; GraphQL rechaza operación no registrada o excedida sin dar permisos privados.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-098 | FR-298 → T-298 → AC-298 | Actor Cuenta autenticada; consultas acotadas; datos `social_graph`; Extensión opcional de lectura, sin mutations ni Cypher arbitrario. Authorization por recurso/campo, dos saltos y profundidad propuesta 4; máximo 100 objetos, una operación y timeout 2 s. Coste y rate limit por usuario. No caché compartida de datos personalizados. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| S08 Relaciones y descubrimiento | FR-609 → T-609 → AC-609 | Delimitar S08 con Eventos; GraphQL de lectura opcional, dependencias S02/S03/S01; colas y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
