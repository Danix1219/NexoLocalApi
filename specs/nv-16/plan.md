# PLAN — nv-16

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Recuperar acceso, editar cuenta y revocar dispositivos. Dependencias: nv-04; nombre moderado usa nv-05.

## 2. Diseño técnico

S01. Recovery de un uso; propiedad de sesión; no cambiar roles desde PATCH. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Prueba recovery incorrecta/reutilizada se rechaza; no revocar dispositivo ajeno ni exponer secretos. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-006 | FR-206 → T-206 → AC-206 | Actor Prueba OTP recovery; datos `accounts`; No aceptar prueba de registro ni código ya consumido Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-009 | FR-209 → T-209 → AC-209 | Actor Propietario; datos `accounts`; Lista blanca; moderar nombre; no permitir cambio libre de roles; fotos propias aprobadas; origen privado nunca se publica. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-010 | FR-210 → T-210 → AC-210 | Actor Propietario; datos `sessions`; No devolver refreshHash ni direcciones completas Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-011 | FR-211 → T-211 → AC-211 | Actor Propietario; datos `sessions`; Verificar dueño de sesión antes de revocar Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.
