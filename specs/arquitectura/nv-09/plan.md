# PLAN — nv-09

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir necesidad, interés y consulta de interesados por propietario; separar preselección opcional de capacidad visible.

Dependencias: nv-06, nv-05.

## 2. Diseño técnico

S04: requests/interests, único requestId/providerId; estado y propietario, urgencies=Hoy diferente de createdToday; no teléfono/domicilio en lectura pública.

Autoridad: [contratos](../../../docs/contracts/README.md). [Acuerdo API–app](../../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. H12: urgencia Hoy incluye publicación anterior; un prestador puede interesarse fuera de categoría coincidente si visibilidad/estado lo permiten; shortlist no bloquea aceptación.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Solo el dueño lista interesados; propia/cerrada no admite interés; matching es filtro y no permiso; Hoy es urgencia; EP-056/057 siguen opcionales y no bloquean lo visible.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-047 | FR-247 → T-247 → AC-247 | Actor Solicitante; datos `requests`; Texto moderado mínimo diez caracteres; no mostrar teléfono público; máximo 280 caracteres; contactPreference chat/both. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-048 | FR-248 → T-248 → AC-248 | Actor Prestador o visitante público limitado; datos `requests`; ownerOnly requiere sesión; matchMyServices compara categorías del perfil sin confundir propiedad. urgency=Hoy filtra urgencia, no antigüedad. Respuesta fuera de cobertura etiquetada y por ampliación explícita. Nunca domicilio ni teléfono público. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-049 | FR-249 → T-249 → AC-249 | Actor Público o propietario según campos; datos `requests`; Propietario obtiene sus datos privados; otros solo zona Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-050 | FR-250 → T-250 → AC-250 | Actor Propietario; datos `requests`; Solo abierta sin asignación; moderación de texto modificado; nunca ampliar acceso a teléfono de pedido fuera de la autorización del cliente. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-051 | FR-251 → T-251 → AC-251 | Actor Propietario; datos `requests`; Si hay pedido ok, exigir resolver/cancelar antes para no contradecirlo Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-052 | FR-252 → T-252 → AC-252 | Actor Propietario; datos `requests`; No borrar un pedido activo en cascada; ocultar y auditar Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

### Regresión H12

FR-712 → T-712 → AC-712: Tres precios, seis fotos portafolio/cuatro por trabajo se conservan en sus dueños; urgencia Hoy no es fecha de creación y shortlist sigue opcional. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

## Rutas, propietario y orden verificable 0.4.1

Dueño: S04 Orders. Rutas objetivo: `src/Domains/orders/ y services/orders/tests/` (no creadas por este plan). Alcance propio: solicitudes y coordinacion de intereses con este spec. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Perfiles de arquitectura aplicables

Consultar [S04](../../../docs/arquitectura/servicios/s04-orders.md) y las [políticas comunes](../../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
