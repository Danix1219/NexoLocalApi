# NV-21 — Dispositivos y notificaciones push

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-21`

## 1. Objetivo

Asociar token por sesión, deduplicar bandeja/entrega y revalidar destino.
Constitución: [rectora](../../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de capacidad existente, sin implementación.

## 2. Alcance / No-alcance

Dependencias: nv-04 y primer productor disponible; integrar nv-08..11 progresivamente. Propietario: S07.

- FR-001: Asociar token por sesión, deduplicar bandeja/entrega y revalidar destino.
- FR-002: Aplicar la regla: Push mínimo sin PII; logout desvincula; sin permiso permanece bandeja.
- FR-003: Integrar y recuperar sin cambiar propiedad de datos.
- FR-090: Refinar historias con fuente, actor, precondición, contrato/revisión y exclusiones.
- FR-100: Resolver el escenario: Reentrega no duplica; token de cuenta anterior no recibe contenido nuevo; destino retirado revalida autorización.
- NFR-001: Aislamiento por identidad/recurso/estado y datos mínimos según constitución.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad definidos antes de implementar.
- NFR-090: Sin proveedor/costo/stack/firma supuestos como ratificados.
- NFR-100: Este desglose no añade servicios, endpoints o pantallas por sí mismo.

Fuera: código/proveedores/infra ejecutados ahora, datos de base ajena y nuevas operaciones no justificadas.

### Historias verificables por operación propia

- FR-283: EP-083 — Registrar token push y preferencias; actor Propietario; autoridad S07. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-284: EP-084 — Desvincular push del dispositivo; actor Propietario; autoridad S07. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-285: EP-085 — Consultar notificaciones persistentes; actor Propietario; autoridad S07. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-286: EP-086 — Marcar aviso leído; actor Propietario; autoridad S07. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-608: S07 Notificaciones: Dispositivos, bandeja y entregas push. Datos propios: nl_notifications.

## 3. Contratos API

[Inventario](../../../docs/contracts/inventario.json) y [contratos](../../../docs/contracts/README.md). Operaciones propias se detallan por EP; sin EP el entregable operativo no requiere ruta inventada.
[Acuerdo](../../../../docs_nexo/SDD_ALINEACION.md) y [fuentes](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md). Contraparte: [spec](../../../../App_NexoLocal/specs/022-push-dispositivos/spec.md).

### Asignación primaria vigente 0.4.0

- EP-083 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-084 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-085 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-086 → [ficha](../../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

[Datos por propietario](../../../docs/modelo-datos.md). S07 mantiene su frontera. Identificar documentos, índices, versión, eventos y retención por historia; no crear base por tener otra spec.

## 5. Seguridad

Push mínimo sin PII; logout desvincula; sin permiso permanece bandeja. [Checklist](../../../docs/asvs-l2-checklist.md). Identidad de servicio y permisos vigentes; sin PII/secretos en evidencia.

## 6. Tests / Gates

- [ ] AC-001: Reentrega no duplica; token de cuenta anterior no recibe contenido nuevo; destino retirado revalida autorización.
- [ ] AC-002: Actor sin permiso no ejecuta ni recibe campos privados; permitido obtiene solo resultado del contrato.
- [ ] AC-003: Fallo/reintento/dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-090: Cada historia identifica entrada/salida, precondición, revisión de contrato y evidencia esperada.
- [ ] AC-100: Push mínimo sin PII; logout desvincula; sin permiso permanece bandeja. Verificar caso positivo, negativo y recuperación pertinente con evidencia real cuando haya producto.

Solo lint/revisión documental ahora; unit/contrato/NoSQL/Redis/proveedor/recuperación al implementar con rutas reales.

- [ ] AC-283 → FR-283/NFR-001/NFR-003: EP-083: Asociado a sid actual; token único; rotación Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-284 → FR-284/NFR-001/NFR-003: EP-084: Idempotente; no afectar otro usuario Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-285 → FR-285/NFR-001/NFR-003: EP-085: Payload mínimo, acceso al destino se revalida al abrir Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-286 → FR-286/NFR-001/NFR-003: EP-086: Solo aviso propio, idempotente Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-608 → FR-608/NFR-002/NFR-003: Deduplicación, reintento y limpieza de tokens. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.

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
| NFR-001 | T-010, T-011, T-004, T-283, T-284, T-285, T-286 | AC-001, AC-002, AC-100, AC-283, AC-284, AC-285, AC-286 |
| NFR-002 | T-001, T-007, T-005, T-608 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-608 |
| NFR-003 | T-002, T-012, T-003, T-005, T-283, T-284, T-285, T-286, T-608 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-283, AC-284, AC-285, AC-286, AC-608 |
| NFR-090 | T-006, T-007 | AC-090 |
| NFR-100 | T-020, T-021, T-004 | AC-002, AC-100 |
| FR-283 | T-283 | AC-283 |
| FR-284 | T-284 | AC-284 |
| FR-285 | T-285 | AC-285 |
| FR-286 | T-286 | AC-286 |
| FR-608 | T-608 | AC-608 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S07 Notifications. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: dispositivos y entrega push con primer productor; desvinculacion por cuenta/sesion. Ubicacion objetivo: services/notifications/src/ y services/notifications/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil S07](../../../docs/arquitectura/servicios/s07-notifications.md) y [políticas comunes](../../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de S07 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |
