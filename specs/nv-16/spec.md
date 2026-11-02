# NV-16 — Cuenta, recuperación y sesiones

Versión: 0.4.0 | Fecha: 2026-11-02 | Rama prevista: `nv-16`

## 1. Objetivo

Recuperar acceso, editar cuenta y revocar dispositivos.
Constitución: [rectora](../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de capacidad existente, sin implementación.

## 2. Alcance / No-alcance

Dependencias: nv-04; nombre moderado usa nv-05. Propietario: S01.

- FR-001: Recuperar acceso, editar cuenta y revocar dispositivos.
- FR-002: Aplicar la regla: Recovery de un uso; propiedad de sesión; no cambiar roles desde PATCH.
- FR-003: Integrar y recuperar sin cambiar propiedad de datos.
- FR-090: Refinar historias con fuente, actor, precondición, contrato/revisión y exclusiones.
- FR-100: Resolver el escenario: Prueba recovery incorrecta/reutilizada se rechaza; no revocar dispositivo ajeno ni exponer secretos.
- NFR-001: Aislamiento por identidad/recurso/estado y datos mínimos según constitución.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad definidos antes de implementar.
- NFR-090: Sin proveedor/costo/stack/firma supuestos como ratificados.
- NFR-100: Este desglose no añade servicios, endpoints o pantallas por sí mismo.

Fuera: código/proveedores/infra ejecutados ahora, datos de base ajena y nuevas operaciones no justificadas.

### Historias verificables por operación propia

- FR-206: EP-006 — Restablecer acceso y revocar sesiones previas; actor Prueba OTP recovery; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-209: EP-009 — Editar nombre, intereses y preferencias; actor Propietario; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-210: EP-010 — Consultar sesiones de sus dispositivos; actor Propietario; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-211: EP-011 — Revocar otro dispositivo; actor Propietario; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.

## 3. Contratos API

[Inventario](../../docs/contracts/inventario.json) y [contratos](../../docs/contracts/README.md). Operaciones propias se detallan por EP; sin EP el entregable operativo no requiere ruta inventada.
[Acuerdo](../../../docs_nexo/SDD_ALINEACION.md) y [fuentes](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md). Contraparte: [spec](../../../App_NexoLocal/specs/017-cuenta-recuperacion-sesiones/spec.md).

### Asignación primaria vigente 0.4.0

- EP-006 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-009 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-010 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-011 → [ficha](../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

[Datos por propietario](../../docs/modelo-datos.md). S01 mantiene su frontera. Identificar documentos, índices, versión, eventos y retención por historia; no crear base por tener otra spec.

## 5. Seguridad

Recovery de un uso; propiedad de sesión; no cambiar roles desde PATCH. [Checklist](../../docs/asvs-l2-checklist.md). Identidad de servicio y permisos vigentes; sin PII/secretos en evidencia.

## 6. Tests / Gates

- [ ] AC-001: Prueba recovery incorrecta/reutilizada se rechaza; no revocar dispositivo ajeno ni exponer secretos.
- [ ] AC-002: Actor sin permiso no ejecuta ni recibe campos privados; permitido obtiene solo resultado del contrato.
- [ ] AC-003: Fallo/reintento/dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-090: Cada historia identifica entrada/salida, precondición, revisión de contrato y evidencia esperada.
- [ ] AC-100: Recovery de un uso; propiedad de sesión; no cambiar roles desde PATCH. Verificar caso positivo, negativo y recuperación pertinente con evidencia real cuando haya producto.

Solo lint/revisión documental ahora; unit/contrato/NoSQL/Redis/proveedor/recuperación al implementar con rutas reales.

- [ ] AC-206 → FR-206/NFR-001/NFR-003: EP-006: No aceptar prueba de registro ni código ya consumido Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-209 → FR-209/NFR-001/NFR-003: EP-009: Lista blanca; moderar nombre; no permitir cambio libre de roles; fotos propias aprobadas; origen privado nunca se publica. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-210 → FR-210/NFR-001/NFR-003: EP-010: No devolver refreshHash ni direcciones completas Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-211 → FR-211/NFR-001/NFR-003: EP-011: Verificar dueño de sesión antes de revocar Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

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
| NFR-001 | T-010, T-011, T-004, T-206, T-209, T-210, T-211 | AC-001, AC-002, AC-100, AC-206, AC-209, AC-210, AC-211 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-012, T-003, T-005, T-206, T-209, T-210, T-211 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-206, AC-209, AC-210, AC-211 |
| NFR-090 | T-006, T-007 | AC-090 |
| NFR-100 | T-020, T-021, T-004 | AC-002, AC-100 |
| FR-206 | T-206 | AC-206 |
| FR-209 | T-209 | AC-209 |
| FR-210 | T-210 | AC-210 |
| FR-211 | T-211 | AC-211 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.
