# NV-25 — Consentimiento y políticas de contacto

Versión: 0.4.0 | Fecha: 2026-11-02 | Rama prevista: `nv-25`

## 1. Objetivo

Coordinar política del prestador, permiso por pedido y consulta vigente antes de divulgar.
Constitución: [rectora](../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de capacidad existente, sin implementación.

## 2. Alcance / No-alcance

Dependencias: nv-06/nv-10; nuevas conversaciones integran nv-11. Propietario: S02/S04/S05.

- FR-001: Coordinar política del prestador, permiso por pedido y consulta vigente antes de divulgar.
- FR-002: Aplicar la regla: ShowPhone/chatEnabled y both vigentes; contacto no cacheable; no nuevo microservicio.
- FR-003: Integrar y recuperar sin cambiar propiedad de datos.
- FR-090: Refinar historias con fuente, actor, precondición, contrato/revisión y exclusiones.
- FR-100: Resolver el escenario: Retirar consentimiento bloquea acceso; pendiente/cancelado no revela teléfono cliente; dependencia caída niega divulgación.
- NFR-001: Aislamiento por identidad/recurso/estado y datos mínimos según constitución.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad definidos antes de implementar.
- NFR-090: Sin proveedor/costo/stack/firma supuestos como ratificados.
- NFR-100: Este desglose no añade servicios, endpoints o pantallas por sí mismo.

Fuera: código/proveedores/infra ejecutados ahora, datos de base ajena y nuevas operaciones no justificadas.

### Historias verificables por operación propia

- FR-291: EP-091 — Obtener número del prestador para copiar; actor Cuenta autenticada; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-292: EP-092 — Obtener contacto privado de la contraparte; actor Participante habilitado; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-296: EP-096 — Configurar chat y visibilidad del número; actor Prestador propietario; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-297: EP-097 — Dar o retirar permiso para compartir su teléfono; actor Cliente propietario del pedido; autoridad S04. Complemento de diseño; condición/UX y alcance se ratifican antes de Ready, sin pantalla inventada.
- FR-300: EP-100 — Comprobar política actual de contacto del prestador; actor Identidad S04/S05 autorizada; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.

- FR-701: Corregir H01: No revelar teléfono de cliente pendiente; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Inventario](../../docs/contracts/inventario.json) y [contratos](../../docs/contracts/README.md). Operaciones propias se detallan por EP; sin EP el entregable operativo no requiere ruta inventada.
[Acuerdo](../../../docs_nexo/SDD_ALINEACION.md) y [fuentes](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md). Contraparte: [spec](../../../App_NexoLocal/specs/025-contacto-consentimiento/spec.md).

### Asignación primaria vigente 0.4.0

- EP-091 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-092 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-096 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-097 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-100 → [ficha](../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

[Datos por propietario](../../docs/modelo-datos.md). S02/S04/S05 mantiene su frontera. Identificar documentos, índices, versión, eventos y retención por historia; no crear base por tener otra spec.

## 5. Seguridad

ShowPhone/chatEnabled y both vigentes; contacto no cacheable; no nuevo microservicio. [Checklist](../../docs/asvs-l2-checklist.md). Identidad de servicio y permisos vigentes; sin PII/secretos en evidencia.

## 6. Tests / Gates

- [ ] AC-001: Retirar consentimiento bloquea acceso; pendiente/cancelado no revela teléfono cliente; dependencia caída niega divulgación.
- [ ] AC-002: Actor sin permiso no ejecuta ni recibe campos privados; permitido obtiene solo resultado del contrato.
- [ ] AC-003: Fallo/reintento/dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-090: Cada historia identifica entrada/salida, precondición, revisión de contrato y evidencia esperada.
- [ ] AC-100: ShowPhone/chatEnabled y both vigentes; contacto no cacheable; no nuevo microservicio. Verificar caso positivo, negativo y recuperación pertinente con evidencia real cuando haya producto.

Solo lint/revisión documental ahora; unit/contrato/NoSQL/Redis/proveedor/recuperación al implementar con rutas reales.

- [ ] AC-291 → FR-291/NFR-001/NFR-003: EP-091: Respuesta privada sin caché compartida; número validado por identidad; no mezclar en DTO público cacheable; exigir showPhone=true vigente; no confiar solo en canal visible en Flutter. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-292 → FR-292/NFR-001/NFR-003: EP-092: Cliente: teléfono del prestador solo con showPhone vigente. Prestador: teléfono del cliente solo asignado en ok/done y clientContactPreference=both vigente. Cancelado o revocado: 403/404. No caché de contacto; comprobación autoritativa. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-296 → FR-296/NFR-001/NFR-003: EP-096: showPhone=false revoca lectura de teléfono inmediatamente. chatEnabled=false impide nuevas conversaciones; propuesta: historial y pedidos activos conservan chat. S05 consulta regla vigente. Alias local wa significa chat interno, no WhatsApp. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-297 → FR-297/NFR-001/NFR-003: EP-097: Ampliación de privacidad: en pend/ok/done permite revocar; cancel no admite conceder. Cambiar a both no revela teléfono antes de ok. S04 es autoridad y retira cachés de contacto. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-300 → FR-300/NFR-001/NFR-003: EP-100: Solo servicios permitidos; S05 verifica antes de nueva conversación y S04 antes de contacto. No conceder por caché obsoleta; dependencia caída niega divulgación. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-701 → FR-701/NFR-001/NFR-003: Participante prestador obtiene teléfono cliente solo en ok/done y both vigente; dato no existe en DTO ni copia no autorizados.

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
| NFR-001 | T-010, T-011, T-004, T-291, T-292, T-296, T-297, T-300, T-701 | AC-001, AC-002, AC-100, AC-291, AC-292, AC-296, AC-297, AC-300, AC-701 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-012, T-003, T-005, T-291, T-292, T-296, T-297, T-300, T-701 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-291, AC-292, AC-296, AC-297, AC-300, AC-701 |
| NFR-090 | T-006, T-007 | AC-090 |
| NFR-100 | T-020, T-021, T-004 | AC-002, AC-100 |
| FR-291 | T-291 | AC-291 |
| FR-292 | T-292 | AC-292 |
| FR-296 | T-296 | AC-296 |
| FR-297 | T-297 | AC-297 |
| FR-300 | T-300 | AC-300 |
| FR-701 | T-701 | AC-701 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.
