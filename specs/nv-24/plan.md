# PLAN — nv-24

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Proteger cola de reportes y decisiones administrativas con aplicación por propietario. Dependencias: nv-04/nv-05; cada propietario aplica evento.

## 2. Diseño técnico

S06. Moderador separado del reportante; auditoría mínima; no escritura cruzada de bases. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Cuenta normal no lista ni decide; reentrega no duplica efecto; ocultar/restaurar lo aplica dueño sin logs privados. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-080 | FR-280 → T-280 → AC-280 | Actor Moderador; datos `reports`; Acceso administrativo separado; auditoría de consulta sensible Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-081 | FR-281 → T-281 → AC-281 | Actor Moderador; datos `reports`; No escribir base ajena; propietario aplica evento idempotente Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
