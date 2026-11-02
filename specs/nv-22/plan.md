# PLAN — nv-22

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Preparar creación idempotente, validadores/índices, migración compatible, respaldo/restauración y supresión. Dependencias: nv-01/nv-02/nv-03; cada propietario habilitado y saga nv-12.

## 2. Diseño técnico

Transversal por propietario. Cada servicio escribe solo su base; no scripts ejecutables ahora; ACL/TTL/transacciones dependen del plan real. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Crear dos veces no duplica; restaurar respeta versión y reaplica supresiones antes de servir; rollback conserva consistencia. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| I02 Almacenamiento y caché | FR-611 → T-611 → AC-611 | Delimitar I02 con SDK privado, dependencias Servicios de negocio y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |
| R14 NoSQL | FR-414 → T-414 → AC-414 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
