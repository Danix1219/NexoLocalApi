# NV-19 — Experiencias, menciones y reputación

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-19`

## 1. Objetivo

Publicar, editar y retirar experiencia declarada o vinculada, recalculando reputación.
Constitución: [rectora](../../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de capacidad existente, sin implementación.

## 2. Alcance / No-alcance

Dependencias: nv-04/nv-05/nv-06; opinión vinculada espera nv-10. Propietario: S03/S02/S04.

- FR-001: Publicar, editar y retirar experiencia declarada o vinculada, recalculando reputación.
- FR-002: Aplicar la regla: Autoría por ID, destinatario único y elegibilidad interna; no autoevaluación.
- FR-003: Integrar y recuperar sin cambiar propiedad de datos.
- FR-090: Refinar historias con fuente, actor, precondición, contrato/revisión y exclusiones.
- FR-100: Resolver el escenario: Cambiar mención retira relación antigua; pedido no done/ajeno no permite opinión vinculada; retirar recalcula promedio.
- NFR-001: Aislamiento por identidad/recurso/estado y datos mínimos según constitución.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad definidos antes de implementar.
- NFR-090: Sin proveedor/costo/stack/firma supuestos como ratificados.
- NFR-100: Este desglose no añade servicios, endpoints o pantallas por sí mismo.

Fuera: código/proveedores/infra ejecutados ahora, datos de base ajena y nuevas operaciones no justificadas.

### Historias verificables por operación propia

- FR-241: EP-041 — Publicar experiencia o calificación; actor Cuenta; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-242: EP-042 — Consultar opiniones y resumen; actor Público; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-243: EP-043 — Corregir experiencia; actor Autor; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-244: EP-044 — Retirar experiencia; actor Autor o moderador; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-293: EP-093 — Buscar prestador para mencionar en una experiencia; actor Cuenta autenticada; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-294: EP-094 — Listar experiencias propias; actor Autor autenticado; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-295: EP-095 — Abrir experiencia individual y su prestador; actor Público o autor según estado; autoridad S03. Complemento de diseño; condición/UX y alcance se ratifican antes de Ready, sin pantalla inventada.

- FR-704: Corregir H04: Mención reactiva; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Inventario](../../../docs/contracts/inventario.json) y [contratos](../../../docs/contracts/README.md). Operaciones propias se detallan por EP; sin EP el entregable operativo no requiere ruta inventada.
[Acuerdo](../../../../docs_nexo/SDD_ALINEACION.md) y [fuentes](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md). Contraparte: [spec](../../../../App_NexoLocal/specs/020-experiencias-reputacion/spec.md).

### Asignación primaria vigente 0.4.0

- EP-041 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-042 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-043 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-044 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-093 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-094 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-095 → [ficha](../../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

[Datos por propietario](../../../docs/modelo-datos.md). S03/S02/S04 mantiene su frontera. Identificar documentos, índices, versión, eventos y retención por historia; no crear base por tener otra spec.

## 5. Seguridad

Autoría por ID, destinatario único y elegibilidad interna; no autoevaluación. [Checklist](../../../docs/asvs-l2-checklist.md). Identidad de servicio y permisos vigentes; sin PII/secretos en evidencia.

## 6. Tests / Gates

- [ ] AC-001: Cambiar mención retira relación antigua; pedido no done/ajeno no permite opinión vinculada; retirar recalcula promedio.
- [ ] AC-002: Actor sin permiso no ejecuta ni recibe campos privados; permitido obtiene solo resultado del contrato.
- [ ] AC-003: Fallo/reintento/dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-090: Cada historia identifica entrada/salida, precondición, revisión de contrato y evidencia esperada.
- [ ] AC-100: Autoría por ID, destinatario único y elegibilidad interna; no autoevaluación. Verificar caso positivo, negativo y recuperación pertinente con evidencia real cuando haya producto.

Solo lint/revisión documental ahora; unit/contrato/NoSQL/Redis/proveedor/recuperación al implementar con rutas reales.

- [ ] AC-241 → FR-241/NFR-001/NFR-003: EP-041: Pedido done propio para registeredOrder; no autoevaluación; verificar prestador activo, un solo destinatario y campos obligatorios también en servidor; outbox review.upserted con versión. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-242 → FR-242/NFR-001/NFR-003: EP-042: Distinguir declarado de pedido registrado; no inflar contadores Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-243 → FR-243/NFR-001/NFR-003: EP-043: Moderar de nuevo y recalcular agregados; no cambiar autor/pedido; en registeredOrder providerId queda ligado al pedido; snapshot reemplaza relaciones de mención/recomendación anteriores. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-244 → FR-244/NFR-001/NFR-003: EP-044: Actualizar promedio y recomendaciones; auditar retiro administrativo Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-293 → FR-293/NFR-001/NFR-003: EP-093: Excluir perfil propio/inactivo; sin q sugerir cobertura cercana; con q buscar nombre/oficio sin imponer cercanía, como mresHTML. Sin teléfono ni coordenadas privadas. Búsqueda infija debe preservar resultados mediante índice/adaptador medido; no sustituirla silenciosamente por solo prefijo. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-294 → FR-294/NFR-001/NFR-003: EP-094: Filtrar authorId del JWT; incluir prestador y estado visible al autor; sin depender del nombre Tú.; caso de aceptación obligatorio: Ana publica, logout, Carlos entra y no la obtiene como propia. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-295 → FR-295/NFR-001/NFR-003: EP-095: Complemento de navegación: necesario para enlace estable/notificación propuesta, no pantalla individual confirmada en maqueta. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-704 → FR-704/NFR-001/NFR-003: Quitar/cambiar prestador actualiza relación estructurada, chip y diálogo; registeredOrder no cambia destinatario fuera del pedido.

## 7. Plan de entrega

[Plan](plan.md), [tareas](task.md), [gates](checklists/gates.md). Integrar con fase origen sin implementar dos veces el mismo EP.

## Aprobación

Estado: Borrador documental; sin implementación ni evidencia de producto.
- [ ] Revisor humano: nombre/fecha pendientes.
- [ ] Decisiones pertinentes resueltas antes de Ready; no firma automática.

La traza acredita asignación documental, no pruebas ejecutadas. Las operaciones internas/admin y opcionales mantienen límites expresos.

## Integración y límites del desglose

Las historias de EP tienen un único dueño primario en el inventario y mapa bilateral; los requisitos agregados previos de fases origen verifican integración con sus subcapacidades. No son una segunda implementación. Una dependencia de cierre de experiencia integrada no impide construir el subset anterior que sí tiene contratos/entradas. Las condiciones bloqueantes específicas se resuelven antes de Ready de cada historia.

## Trazabilidad del incremento

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-001 | T-001, T-002, T-010 | AC-001, AC-002, AC-003, AC-090 |
| FR-002 | T-001, T-002, T-011 | AC-001, AC-002, AC-003, AC-090 |
| FR-003 | T-001, T-002, T-012 | AC-001, AC-002, AC-003, AC-090 |
| FR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-001 | T-010, T-011, T-004, T-241, T-242, T-243, T-244, T-293, T-294, T-295, T-704 | AC-001, AC-002, AC-100, AC-241, AC-242, AC-243, AC-244, AC-293, AC-294, AC-295, AC-704 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-012, T-003, T-005, T-241, T-242, T-243, T-244, T-293, T-294, T-295, T-704 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-241, AC-242, AC-243, AC-244, AC-293, AC-294, AC-295, AC-704 |
| NFR-090 | T-006, T-007 | AC-090 |
| NFR-100 | T-020, T-021, T-004 | AC-002, AC-100 |
| FR-241 | T-241 | AC-241 |
| FR-242 | T-242 | AC-242 |
| FR-243 | T-243 | AC-243 |
| FR-244 | T-244 | AC-244 |
| FR-293 | T-293 | AC-293 |
| FR-294 | T-294 | AC-294 |
| FR-295 | T-295 | AC-295 |
| FR-704 | T-704 | AC-704 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S03 con contratos S02/S04. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: experiencias/menciones/reputacion sin referencias de proyecto a Domain de otro servicio. Ubicacion objetivo: services/community/src/ y pruebas de contrato con Directory/Orders.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
