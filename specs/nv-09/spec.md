# NV-09 — Solicitudes, intereses y candidatos

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-09`

## 1. Objetivo

Constitución rectora: [fuente única](../../.specify/memory/constitution.md). Aplicación: Constitución §§2/5/6; H12.

Solicitudes, intereses y candidatos

Solicitudes, intereses y candidatos. Servicios involucrados: S04, S02, S03.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 04, 07, 14, 16. Componentes: M09, M13, M16. Hallazgos: H12.

Dependencias: nv-06, nv-05.

- FR-001: Cliente publica/edita/cierra/retira necesidad; interesados se consultan mediante nv-20.
- FR-002: Integrar expresar/retirar interés y candidatos con nv-20, preservando permisos/estado de la solicitud.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Solicitud 10..280, urgencia Hoy separada de fecha de creación y ownerOnly distinto de matchMyServices.
- Coincidencia de categoría es filtro; visibilidad/estado restringen interés.
- Preselección explícita es extensión opcional, sin bloquear el alcance visible.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir necesidad, interés y consulta de interesados por propietario; separar preselección opcional de capacidad visible.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; H12: urgencia Hoy incluye publicación anterior; un prestador puede interesarse fuera de categoría coincidente si visibilidad/estado lo permiten; shortlist no bloquea aceptación.

- FR-100: Precisar permisos de listado/detalle e interesados, cierre de solicitud y consistencia de interés al cambiar visibilidad o estado.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/5/6; H12.

### Historias verificables por operación propia

- FR-247: EP-047 — Publicar necesidad de servicio; actor Solicitante; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-248: EP-048 — Consultar solicitudes de zona y capacidad; actor Prestador o visitante público limitado; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-249: EP-049 — Abrir solicitud; actor Público o propietario según campos; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-250: EP-050 — Corregir necesidad abierta; actor Propietario; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-251: EP-051 — Cerrar necesidad ya resuelta; actor Propietario; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-252: EP-052 — Retirar solicitud; actor Propietario; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.

- FR-712: Corregir H12: Límites y filtros reales; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Endpoints vivos](../../docs/endpoints.md) y [registro de contratos](../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../App_NexoLocal/specs/009-solicitudes/spec.md).

### Asignación primaria vigente 0.4.0

- EP-047 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-048 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-049 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-050 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-051 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-052 → [ficha](../../docs/contracts/fichas-operaciones.md)

| Operación integrada | Nuevo dueño de incremento |
|---|---|
| EP-053 | [nv-20](../nv-20/spec.md) |
| EP-054 | [nv-20](../nv-20/spec.md) |
| EP-055 | [nv-20](../nv-20/spec.md) |
| EP-056 | [nv-20](../nv-20/spec.md) |
| EP-057 | [nv-20](../nv-20/spec.md) |

Los FR/AC heredados de esta fase comprueban integración con esas capacidades; sus tareas EP y casos de uso se ejecutan únicamente en la spec primaria del inventario. El propietario S01–S08 no cambia.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S04: requests/interests, único requestId/providerId; estado y propietario, urgencies=Hoy diferente de createdToday; no teléfono/domicilio en lectura pública.

## 5. Seguridad

- Solicitud 10..280, urgencia Hoy separada de fecha de creación y ownerOnly distinto de matchMyServices.
- Coincidencia de categoría es filtro; visibilidad/estado restringen interés.
- Preselección explícita es extensión opcional, sin bloquear el alcance visible.

Ver [checklist seguridad](../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Propia/cerrada no admite interés; duplicado es idempotente.
- [ ] AC-002: Urgencia Hoy devuelve urgentes aunque se hayan publicado ayer.
- [ ] AC-003: No devolver teléfono ni domicilio en lista/detalle público.

Suites y gate: [roles y gates](../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: H12: urgencia Hoy incluye publicación anterior; un prestador puede interesarse fuera de categoría coincidente si visibilidad/estado lo permiten; shortlist no bloquea aceptación.

- [ ] AC-100 → FR-100/NFR-100: Solo el dueño lista interesados; propia/cerrada no admite interés; matching es filtro y no permiso; Hoy es urgencia; EP-056/057 siguen opcionales y no bloquean lo visible.

- [ ] AC-247 → FR-247/NFR-001/NFR-003: EP-047: Texto moderado mínimo diez caracteres; no mostrar teléfono público; máximo 280 caracteres; contactPreference chat/both. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-248 → FR-248/NFR-001/NFR-003: EP-048: ownerOnly requiere sesión; matchMyServices compara categorías del perfil sin confundir propiedad. urgency=Hoy filtra urgencia, no antigüedad. Respuesta fuera de cobertura etiquetada y por ampliación explícita. Nunca domicilio ni teléfono público. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-249 → FR-249/NFR-001/NFR-003: EP-049: Propietario obtiene sus datos privados; otros solo zona Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-250 → FR-250/NFR-001/NFR-003: EP-050: Solo abierta sin asignación; moderación de texto modificado; nunca ampliar acceso a teléfono de pedido fuera de la autorización del cliente. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-251 → FR-251/NFR-001/NFR-003: EP-051: Si hay pedido ok, exigir resolver/cancelar antes para no contradecirlo Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-252 → FR-252/NFR-001/NFR-003: EP-052: No borrar un pedido activo en cascada; ocultar y auditar Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-712 → FR-712/NFR-001/NFR-003: Tres precios, seis fotos portafolio/cuatro por trabajo se conservan en sus dueños; urgencia Hoy no es fecha de creación y shortlist sigue opcional.

## 7. Plan de entrega

Spec→plan→task→tests/gates→Memoria→checklist→revisión→release. No desplegar por escribir una spec. Ver [runbook](../../docs/runbook-release.md).

## Aprobación

Estado: Borrador documental; sin implementación ni evidencia de producto.
- [ ] Revisor humano identificado — nombre y fecha pendientes.
- [ ] CHECKLIST_PR.md completado con evidencia propia.
- [ ] Sin conflicto con constitución y requisitos del proyecto.

No firmar ni marcar aprobada sin revisión real.

La tabla define cobertura de diseño, no pruebas ejecutadas. El plan concreta método/entorno; el checklist registra evidencia y revisión humana. Los escenarios compartidos no trasladan la propiedad del backend a Flutter.

La traza acredita asignación documental, no pruebas ejecutadas. Las operaciones internas/admin y opcionales mantienen límites expresos.

## Integración y límites del desglose

Las historias de EP tienen un único dueño primario en el inventario y mapa bilateral; los requisitos agregados previos de fases origen verifican integración con sus subcapacidades. No son una segunda implementación. Una dependencia de cierre de experiencia integrada no impide construir el subset anterior que sí tiene contratos/entradas. Las condiciones bloqueantes específicas se resuelven antes de Ready de cada historia.

## Trazabilidad del incremento

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-001 | T-001, T-002, T-010, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-002 | T-001, T-002, T-011, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-001 | T-010, T-011, T-004, T-247, T-248, T-249, T-250, T-251, T-252, T-712 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-247, AC-248, AC-249, AC-250, AC-251, AC-252, AC-712 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-247, T-248, T-249, T-250, T-251, T-252, T-712 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-247, AC-248, AC-249, AC-250, AC-251, AC-252, AC-712 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-247 | T-247 | AC-247 |
| FR-248 | T-248 | AC-248 |
| FR-249 | T-249 | AC-249 |
| FR-250 | T-250 | AC-250 |
| FR-251 | T-251 | AC-251 |
| FR-252 | T-252 | AC-252 |
| FR-712 | T-712 | AC-712 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S04 Orders. [Mapa backend](../../docs/estructura-backend.md) y [orden SDD](../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: solicitudes y coordinacion de intereses con nv-20. Ubicacion objetivo: services/orders/src/ y services/orders/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
