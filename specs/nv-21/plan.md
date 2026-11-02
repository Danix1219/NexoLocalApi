# PLAN — nv-21

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Asociar token por sesión, deduplicar bandeja/entrega y revalidar destino. Dependencias: nv-04 y primer productor disponible; integrar nv-08..11 progresivamente.

## 2. Diseño técnico

S07. Push mínimo sin PII; logout desvincula; sin permiso permanece bandeja. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Reentrega no duplica; token de cuenta anterior no recibe contenido nuevo; destino retirado revalida autorización. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-083 | FR-283 → T-283 → AC-283 | Actor Propietario; datos `devices`; Asociado a sid actual; token único; rotación Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-084 | FR-284 → T-284 → AC-284 | Actor Propietario; datos `devices`; Idempotente; no afectar otro usuario Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-085 | FR-285 → T-285 → AC-285 | Actor Propietario; datos `notifications`; Payload mínimo, acceso al destino se revalida al abrir Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-086 | FR-286 → T-286 → AC-286 | Actor Propietario; datos `notifications`; Solo aviso propio, idempotente Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| S07 Notificaciones | FR-608 → T-608 → AC-608 | Delimitar S07 con REST y consumo de eventos, dependencias FCM, colas y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
