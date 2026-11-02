# PLAN — nv-23

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Coordinar landing, aviso/eliminación, GA4/QR, entrevistas y paquete académico. Dependencias: Preparación nv-00; backend nv-14 y todas las capacidades obligatorias; S08 no bloquea.

## 2. Diseño técnico

Gestión/landing. Landing artefacto separado con dueño web por asignar; no endpoint inventado para entrega. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Play se comprueba con contraparte; aviso/eliminación/GA4/QR y .rar/scripts reales tienen evidencia sin PII ni secretos. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| W01 landing | FR-528 → T-528 → AC-528 | Artefacto web separado, hosting permitido y dueño por asignar; no convertir landing en pantalla Flutter ni en endpoint del backend. |
| R01 Instalación desde Play | FR-401 → T-401 → AC-401 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R17 Landing/Analytics | FR-417 → T-417 → AC-417 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R19 Paquete y evaluación | FR-419 → T-419 → AC-419 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
