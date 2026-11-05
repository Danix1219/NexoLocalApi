# PLAN — nv-19

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Publicar, editar y retirar experiencia declarada o vinculada, recalculando reputación. Dependencias: nv-04/nv-05/nv-06; opinión vinculada espera nv-10.

## 2. Diseño técnico

S03/S02/S04. Autoría por ID, destinatario único y elegibilidad interna; no autoevaluación. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Cambiar mención retira relación antigua; pedido no done/ajeno no permite opinión vinculada; retirar recalcula promedio. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-041 | FR-241 → T-241 → AC-241 | Actor Cuenta; datos `reviews`; Pedido done propio para registeredOrder; no autoevaluación; verificar prestador activo, un solo destinatario y campos obligatorios también en servidor; outbox review.upserted con versión. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-042 | FR-242 → T-242 → AC-242 | Actor Público; datos `reviews`; Distinguir declarado de pedido registrado; no inflar contadores Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-043 | FR-243 → T-243 → AC-243 | Actor Autor; datos `reviews`; Moderar de nuevo y recalcular agregados; no cambiar autor/pedido; en registeredOrder providerId queda ligado al pedido; snapshot reemplaza relaciones de mención/recomendación anteriores. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-044 | FR-244 → T-244 → AC-244 | Actor Autor o moderador; datos `reviews`; Actualizar promedio y recomendaciones; auditar retiro administrativo Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-093 | FR-293 → T-293 → AC-293 | Actor Cuenta autenticada; datos `providers`; Excluir perfil propio/inactivo; sin q sugerir cobertura cercana; con q buscar nombre/oficio sin imponer cercanía, como mresHTML. Sin teléfono ni coordenadas privadas. Búsqueda infija debe preservar resultados mediante índice/adaptador medido; no sustituirla silenciosamente por solo prefijo. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-094 | FR-294 → T-294 → AC-294 | Actor Autor autenticado; datos `reviews`; Filtrar authorId del JWT; incluir prestador y estado visible al autor; sin depender del nombre Tú.; caso de aceptación obligatorio: Ana publica, logout, Carlos entra y no la obtiene como propia. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-095 | FR-295 → T-295 → AC-295 | Actor Público o autor según estado; datos `reviews`; Complemento de navegación: necesario para enlace estable/notificación propuesta, no pantalla individual confirmada en maqueta. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

### Regresión H04

FR-704 → T-704 → AC-704: Quitar/cambiar prestador actualiza relación estructurada, chip y diálogo; registeredOrder no cambia destinatario fuera del pedido. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

## Rutas, propietario y orden verificable 0.4.1

Dueño: S03 con contratos S02/S04. Rutas objetivo: `services/community/src/ y pruebas de contrato con Directory/Orders` (no creadas por este plan). Alcance propio: experiencias/menciones/reputacion sin referencias de proyecto a Domain de otro servicio. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Perfiles de arquitectura aplicables

Consultar [S02](../../../docs/arquitectura/servicios/s02-directory.md); [S03](../../../docs/arquitectura/servicios/s03-community.md) y las [políticas comunes](../../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
