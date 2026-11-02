# NV-18 — Reacciones, guardados e indicadores

Versión: 0.4.0 | Fecha: 2026-11-02 | Rama prevista: `nv-18`

## 1. Objetivo

Gestionar recomendaciones, guardados por cuenta y métricas de actividad minimizadas.
Constitución: [rectora](../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de capacidad existente, sin implementación.

## 2. Alcance / No-alcance

Dependencias: nv-04/nv-06/nv-08; solicitudes se integran con nv-09. Propietario: S03.

- FR-001: Gestionar recomendaciones, guardados por cuenta y métricas de actividad minimizadas.
- FR-002: Aplicar la regla: Unicidad y privacidad; clic no acredita venta ni servicio; sin identidad de visitantes.
- FR-003: Integrar y recuperar sin cambiar propiedad de datos.
- FR-090: Refinar historias con fuente, actor, precondición, contrato/revisión y exclusiones.
- FR-100: Resolver el escenario: PUT/DELETE repetido no duplica efecto; recurso retirado se revalida al hidratar; métrica propia no expone visitantes.
- NFR-001: Aislamiento por identidad/recurso/estado y datos mínimos según constitución.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad definidos antes de implementar.
- NFR-090: Sin proveedor/costo/stack/firma supuestos como ratificados.
- NFR-100: Este desglose no añade servicios, endpoints o pantallas por sí mismo.

Fuera: código/proveedores/infra ejecutados ahora, datos de base ajena y nuevas operaciones no justificadas.

### Historias verificables por operación propia

- FR-236: EP-036 — Recomendar trabajo o perfil; actor Cuenta; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-237: EP-037 — Retirar recomendación; actor Autor de reacción; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-238: EP-038 — Consultar prestadores, trabajos o solicitudes guardados; actor Propietario; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-239: EP-039 — Guardar objetivo; actor Propietario; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-240: EP-040 — Quitar guardado; actor Propietario; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-245: EP-045 — Registrar vista, compartir o intención de contacto; actor Público limitado o cuenta; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-246: EP-046 — Consultar actividad de su perfil; actor Prestador propietario; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.

## 3. Contratos API

[Inventario](../../docs/contracts/inventario.json) y [contratos](../../docs/contracts/README.md). Operaciones propias se detallan por EP; sin EP el entregable operativo no requiere ruta inventada.
[Acuerdo](../../../docs_nexo/SDD_ALINEACION.md) y [fuentes](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md). Contraparte: [spec](../../../App_NexoLocal/specs/019-guardados-indicadores/spec.md).

### Asignación primaria vigente 0.4.0

- EP-036 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-037 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-038 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-039 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-040 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-045 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-046 → [ficha](../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

[Datos por propietario](../../docs/modelo-datos.md). S03 mantiene su frontera. Identificar documentos, índices, versión, eventos y retención por historia; no crear base por tener otra spec.

## 5. Seguridad

Unicidad y privacidad; clic no acredita venta ni servicio; sin identidad de visitantes. [Checklist](../../docs/asvs-l2-checklist.md). Identidad de servicio y permisos vigentes; sin PII/secretos en evidencia.

## 6. Tests / Gates

- [ ] AC-001: PUT/DELETE repetido no duplica efecto; recurso retirado se revalida al hidratar; métrica propia no expone visitantes.
- [ ] AC-002: Actor sin permiso no ejecuta ni recibe campos privados; permitido obtiene solo resultado del contrato.
- [ ] AC-003: Fallo/reintento/dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-090: Cada historia identifica entrada/salida, precondición, revisión de contrato y evidencia esperada.
- [ ] AC-100: Unicidad y privacidad; clic no acredita venta ni servicio; sin identidad de visitantes. Verificar caso positivo, negativo y recuperación pertinente con evidencia real cuando haya producto.

Solo lint/revisión documental ahora; unit/contrato/NoSQL/Redis/proveedor/recuperación al implementar con rutas reales.

- [ ] AC-236 → FR-236/NFR-001/NFR-003: EP-036: Una reacción por cuenta/objetivo; tipo validado; no contador libre Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-237 → FR-237/NFR-001/NFR-003: EP-037: Idempotente; recalcular agregados por evento Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-238 → FR-238/NFR-001/NFR-003: EP-038: Autorización al hidratar cada recurso; mostrar retirado cuando aplique Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-239 → FR-239/NFR-001/NFR-003: EP-039: Restricción única; verificar visibilidad del objetivo Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-240 → FR-240/NFR-001/NFR-003: EP-040: Idempotente y sincronizable offline Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-245 → FR-245/NFR-001/NFR-003: EP-045: Minimizar datos; idempotente; no equiparar clic con servicio realizado Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-246 → FR-246/NFR-001/NFR-003: EP-046: Sin identidad de visitantes ni datos privados Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

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
| NFR-001 | T-010, T-011, T-004, T-236, T-237, T-238, T-239, T-240, T-245, T-246 | AC-001, AC-002, AC-100, AC-236, AC-237, AC-238, AC-239, AC-240, AC-245, AC-246 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-012, T-003, T-005, T-236, T-237, T-238, T-239, T-240, T-245, T-246 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-236, AC-237, AC-238, AC-239, AC-240, AC-245, AC-246 |
| NFR-090 | T-006, T-007 | AC-090 |
| NFR-100 | T-020, T-021, T-004 | AC-002, AC-100 |
| FR-236 | T-236 | AC-236 |
| FR-237 | T-237 | AC-237 |
| FR-238 | T-238 | AC-238 |
| FR-239 | T-239 | AC-239 |
| FR-240 | T-240 | AC-240 |
| FR-245 | T-245 | AC-245 |
| FR-246 | T-246 | AC-246 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.
