# PLAN — nv-20

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Expresar/retirar interés y consultar interesados propios; shortlist opcional. Dependencias: nv-09/nv-06/nv-04.

## 2. Diseño técnico

S04. Matching es filtro, no permiso; dueño consulta interesados. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Interés repetido no duplica; cerrado/propio se rechaza; fuera de categoría se admite si visible; shortlist no acepta pedido. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-053 | FR-253 → T-253 → AC-253 | Actor Prestador; datos `interests`; Solicitud abierta ajena y perfil activo; una relación única; no impedir por categoría no coincidente: la maqueta permite esa acción; mantener visibilidad y estado como restricciones. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-054 | FR-254 → T-254 → AC-254 | Actor Prestador interesado; datos `interests`; No alterar asignación existente; retirar relación propia Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-055 | FR-255 → T-255 → AC-255 | Actor Dueño de solicitud; datos `interests`; Enriquecer por lote con S02/S03 y mantener cursor estable Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-056 | FR-256 → T-256 → AC-256 | Actor Dueño de solicitud; datos `interests`; Requiere interés existente; no equivale a aceptación Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-057 | FR-257 → T-257 → AC-257 | Actor Dueño de solicitud; datos `interests`; Solo dueño; operación idempotente Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Rutas, propietario y orden verificable 0.4.1

Dueño: S04 Orders. Rutas objetivo: `services/orders/src/ y services/orders/tests/` (no creadas por este plan). Alcance propio: intereses/candidatos sobre el propietario de nv-09; sin nuevo servicio por spec. Leer [mapa de carpetas](../../docs/estructura-backend.md) y [secuencia SDD](../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Perfiles de arquitectura aplicables

Consultar [S04](../../docs/arquitectura/servicios/s04-orders.md) y las [políticas comunes](../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
