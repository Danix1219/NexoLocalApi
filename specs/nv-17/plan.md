# PLAN — nv-17

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Consultar catálogo activo y administrar categorías con auditoría e invalidación. Dependencias: nv-01/nv-02/nv-03; identidad administrativa nv-04.

## 2. Diseño técnico

S02. Administración separada del consumidor público; no UI admin Flutter implícita. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Categoría inactiva se excluye; actualizar invalida caché entre réplicas sin romper referencias. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-014 | FR-214 → T-214 → AC-214 | Actor Público; datos `categories`; Excluir altas inactivas; caché público 24 h Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-015 | FR-215 → T-215 → AC-215 | Actor Administrador; datos `categories`; Auditar; conservar referencias; invalidar caché Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
