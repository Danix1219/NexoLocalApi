# PLAN — nv-12

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir dispositivos, bandeja y delivery push; saga de eliminación con acuse por servicio y reintentos/reconciliación.

Dependencias: nv-04 para inicio de borrado; cierre global integra nv-05..11; S08 solo si habilitado.

## 2. Diseño técnico

S07: devices/notifications/deliveries; S01 deletions y acuses de cada servicio habilitado. Dedupe por eventId/destinatario; tombstones impiden reaparición. Retención/backups requieren decisión explícita.

Autoridad: [contratos](../../docs/contracts/README.md). [Acuerdo API–app](../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. Push a sesión anterior no entrega datos de nueva cuenta; borrado parcial no informa completado; cuenta bloqueada de inmediato; restaurar backup aplica supresiones antes de servir.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Cuenta bloqueada al inicio; falta de acuse deja trámite pendiente, reentrega no duplica efecto; reaplicar supresiones al restaurar evita resucitar datos; retención se ratifica, no se inventa.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-012 | FR-212 → T-212 → AC-212 | Actor Propietario reautenticado; datos `deletions`; Bloquear cuenta y emitir evento; no afirmar completado al recibir Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-013 | FR-213 → T-213 → AC-213 | Actor Credencial limitada al trámite; datos `deletions`; Receipt solo permite consultar este trámite, nunca reabrir cuenta Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
