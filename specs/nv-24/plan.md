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

## Rutas, propietario y orden verificable 0.4.1

Dueño: S06 Trust con eventos por dueño. Rutas objetivo: `services/trust/src/ y contratos/pruebas del consumidor` (no creadas por este plan). Alcance propio: reportes/decisiones administrativas; cada dueño aplica cambios en su propia base. Leer [mapa de carpetas](../../docs/estructura-backend.md) y [secuencia SDD](../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Perfiles de arquitectura aplicables

Consultar [S06](../../docs/arquitectura/servicios/s06-trust.md) y las [políticas comunes](../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
