# NV-20 — Intereses y candidatos

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-20`

## 1. Objetivo

Expresar/retirar interés y consultar interesados propios; shortlist opcional.
Constitución: [rectora](../../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de capacidad existente, sin implementación.

## 2. Alcance / No-alcance

Dependencias: nv-09/nv-06/nv-04. Propietario: S04.

- FR-001: Expresar/retirar interés y consultar interesados propios; shortlist opcional.
- FR-002: Aplicar la regla: Matching es filtro, no permiso; dueño consulta interesados.
- FR-003: Integrar y recuperar sin cambiar propiedad de datos.
- FR-090: Refinar historias con fuente, actor, precondición, contrato/revisión y exclusiones.
- FR-100: Resolver el escenario: Interés repetido no duplica; cerrado/propio se rechaza; fuera de categoría se admite si visible; shortlist no acepta pedido.
- NFR-001: Aislamiento por identidad/recurso/estado y datos mínimos según constitución.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad definidos antes de implementar.
- NFR-090: Sin proveedor/costo/stack/firma supuestos como ratificados.
- NFR-100: Este desglose no añade servicios, endpoints o pantallas por sí mismo.

Fuera: código/proveedores/infra ejecutados ahora, datos de base ajena y nuevas operaciones no justificadas.

### Historias verificables por operación propia

- FR-253: EP-053 — Expresar interés; actor Prestador; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-254: EP-054 — Retirar interés; actor Prestador interesado; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-255: EP-055 — Consultar interesados y ordenar; actor Dueño de solicitud; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-256: EP-056 — [Ampliación opcional] Preseleccionar candidato; actor Dueño de solicitud; autoridad S04. Opcional; no bloquea alcance obligatorio si no se habilita.
- FR-257: EP-057 — [Ampliación opcional] Quitar preselección; actor Dueño de solicitud; autoridad S04. Opcional; no bloquea alcance obligatorio si no se habilita.

## 3. Contratos API

[Inventario](../../../docs/contracts/inventario.json) y [contratos](../../../docs/contracts/README.md). Operaciones propias se detallan por EP; sin EP el entregable operativo no requiere ruta inventada.
[Acuerdo](../../../../docs_nexo/SDD_ALINEACION.md) y [fuentes](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md). Contraparte: [spec](../../../../App_NexoLocal/specs/021-interesados-candidatos/spec.md).

### Asignación primaria vigente 0.4.0

- EP-053 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-054 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-055 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-056 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-057 → [ficha](../../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

[Datos por propietario](../../../docs/modelo-datos.md). S04 mantiene su frontera. Identificar documentos, índices, versión, eventos y retención por historia; no crear base por tener otra spec.

## 5. Seguridad

Matching es filtro, no permiso; dueño consulta interesados. [Checklist](../../../docs/asvs-l2-checklist.md). Identidad de servicio y permisos vigentes; sin PII/secretos en evidencia.

## 6. Tests / Gates

- [ ] AC-001: Interés repetido no duplica; cerrado/propio se rechaza; fuera de categoría se admite si visible; shortlist no acepta pedido.
- [ ] AC-002: Actor sin permiso no ejecuta ni recibe campos privados; permitido obtiene solo resultado del contrato.
- [ ] AC-003: Fallo/reintento/dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-090: Cada historia identifica entrada/salida, precondición, revisión de contrato y evidencia esperada.
- [ ] AC-100: Matching es filtro, no permiso; dueño consulta interesados. Verificar caso positivo, negativo y recuperación pertinente con evidencia real cuando haya producto.

Solo lint/revisión documental ahora; unit/contrato/NoSQL/Redis/proveedor/recuperación al implementar con rutas reales.

- [ ] AC-253 → FR-253/NFR-001/NFR-003: EP-053: Solicitud abierta ajena y perfil activo; una relación única; no impedir por categoría no coincidente: la maqueta permite esa acción; mantener visibilidad y estado como restricciones. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-254 → FR-254/NFR-001/NFR-003: EP-054: No alterar asignación existente; retirar relación propia Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-255 → FR-255/NFR-001/NFR-003: EP-055: Enriquecer por lote con S02/S03 y mantener cursor estable Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-256 → FR-256/NFR-001/NFR-003: EP-056: Requiere interés existente; no equivale a aceptación Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-257 → FR-257/NFR-001/NFR-003: EP-057: Solo dueño; operación idempotente Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

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
| NFR-001 | T-010, T-011, T-004, T-253, T-254, T-255, T-256, T-257 | AC-001, AC-002, AC-100, AC-253, AC-254, AC-255, AC-256, AC-257 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-012, T-003, T-005, T-253, T-254, T-255, T-256, T-257 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-253, AC-254, AC-255, AC-256, AC-257 |
| NFR-090 | T-006, T-007 | AC-090 |
| NFR-100 | T-020, T-021, T-004 | AC-002, AC-100 |
| FR-253 | T-253 | AC-253 |
| FR-254 | T-254 | AC-254 |
| FR-255 | T-255 | AC-255 |
| FR-256 | T-256 | AC-256 |
| FR-257 | T-257 | AC-257 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S04 Orders. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: intereses/candidatos sobre el propietario de nv-09; sin nuevo servicio por spec. Ubicacion objetivo: services/orders/src/ y services/orders/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
