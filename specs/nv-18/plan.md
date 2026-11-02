# PLAN — nv-18

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Gestionar recomendaciones, guardados por cuenta y métricas de actividad minimizadas. Dependencias: nv-04/nv-06/nv-08; solicitudes se integran con nv-09.

## 2. Diseño técnico

S03. Unicidad y privacidad; clic no acredita venta ni servicio; sin identidad de visitantes. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

PUT/DELETE repetido no duplica efecto; recurso retirado se revalida al hidratar; métrica propia no expone visitantes. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-036 | FR-236 → T-236 → AC-236 | Actor Cuenta; datos `saved`; Una reacción por cuenta/objetivo; tipo validado; no contador libre Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-037 | FR-237 → T-237 → AC-237 | Actor Autor de reacción; datos `saved`; Idempotente; recalcular agregados por evento Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-038 | FR-238 → T-238 → AC-238 | Actor Propietario; datos `saved`; Autorización al hidratar cada recurso; mostrar retirado cuando aplique Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-039 | FR-239 → T-239 → AC-239 | Actor Propietario; datos `saved`; Restricción única; verificar visibilidad del objetivo Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-040 | FR-240 → T-240 → AC-240 | Actor Propietario; datos `saved`; Idempotente y sincronizable offline Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-045 | FR-245 → T-245 → AC-245 | Actor Público limitado o cuenta; datos `metrics`; Minimizar datos; idempotente; no equiparar clic con servicio realizado Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-046 | FR-246 → T-246 → AC-246 | Actor Prestador propietario; datos `metrics`; Sin identidad de visitantes ni datos privados Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
