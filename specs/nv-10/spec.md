# NV-10 — Pedidos, privacidad de contacto y resolución

Versión: 0.4.0 | Fecha: 2026-11-02 | Rama prevista: `nv-10`

## 1. Objetivo

Constitución rectora: [fuente única](../../.specify/memory/constitution.md). Aplicación: Constitución §§2/4/5/6/8; H01/H02.

Pedidos, privacidad de contacto y resolución

Pedidos, privacidad de contacto y resolución. Servicios involucrados: S04, S02, S01, S03.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 04, 07, 10, 14, 15, 16. Componentes: M17, M27. Hallazgos: H01, H02.

Dependencias: nv-09, nv-06; entrada perfil/candidato primero; entrada chat integra con nv-11 sin ciclo de implementación.

- FR-001: Cliente envía pedido desde perfil, candidato o chat.
- FR-002: Prestador acepta con precio opcional o rechaza; participantes cancelan; prestador resuelve.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- pend→ok/cancel; ok→done/cancel; done/cancel terminales.
- Aceptar y asignar solicitud son transacción local S04 con version/outbox.
- Teléfono cliente exige participante autorizado, estado ok/done y both vigente.
- Rechazo/cancelación revoca ubicación precisa y rutas; solo devuelve zona.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir transiciones de pedido, concurrencia/asignación y contacto por consentimiento; aceptación y outbox locales a S04.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; H01/H02: pendiente/rechazado no expone teléfono/domicilio; cancelar retira también caché/rutas; dos aceptaciones simultáneas dejan solo una asignación activa.

- FR-100: Precisar matriz de transiciones, consentimiento y exposición por campo, versión esperada e idempotencia de pedido/asignación.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/4/5/6/8; H01/H02.

### Historias verificables por operación propia

- FR-258: EP-058 — Enviar pedido desde perfil, chat o interesado; actor Solicitante; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-259: EP-059 — Consultar pedidos por modo y estado; actor Participante; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-260: EP-060 — Consultar estado y logística autorizada; actor Participante; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-261: EP-061 — Aceptar y opcionalmente fijar precio; actor Prestador destinatario; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-262: EP-062 — Rechazar pedido pendiente; actor Prestador destinatario; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-263: EP-063 — Cancelar servicio activo; actor Cliente o prestador participante; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-264: EP-064 — Declarar servicio resuelto; actor Prestador destinatario; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-265: EP-065 — Cambiar punto y referencias del servicio; actor Cliente del pedido; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-288: EP-088 — Verificar opinión vinculada a pedido; actor Identidad S03; autoridad S04. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-605: S04 Solicitudes y pedidos: Necesidades, interés, preselección, pedido, precio y domicilio. Datos propios: nl_orders.

- FR-702: Corregir H02: No revelar domicilio al rechazar/cancelar; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Endpoints vivos](../../docs/endpoints.md) y [registro de contratos](../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../App_NexoLocal/specs/010-pedidos/spec.md).

### Asignación primaria vigente 0.4.0

- EP-058 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-059 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-060 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-061 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-062 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-063 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-064 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-065 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-088 → [ficha](../../docs/contracts/fichas-operaciones.md)

| Operación integrada | Nuevo dueño de incremento |
|---|---|
| EP-092 | [nv-25](../nv-25/spec.md) |
| EP-097 | [nv-25](../nv-25/spec.md) |
| EP-100 | [nv-25](../nv-25/spec.md) |

Los FR/AC heredados de esta fase comprueban integración con esas capacidades; sus tareas EP y casos de uso se ejecutan únicamente en la spec primaria del inventario. El propietario S01–S08 no cambia.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S04: orders y referencia de asignación en requests; version, participante, precio informativo. pend→ok/cancel; ok→done/cancel. Rechazo se representa con cancel y razón ratificada; done/cancel terminales.

## 5. Seguridad

- pend→ok/cancel; ok→done/cancel; done/cancel terminales.
- Aceptar y asignar solicitud son transacción local S04 con version/outbox.
- Teléfono cliente exige participante autorizado, estado ok/done y both vigente.
- Rechazo/cancelación revoca ubicación precisa y rutas; solo devuelve zona.

Ver [checklist seguridad](../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Dos aceptaciones simultáneas dejan una asignación activa; otra produce conflicto.
- [ ] AC-002: Pedido pendiente no entrega teléfono del cliente ni domicilio exacto.
- [ ] AC-003: Rechazar pendiente no expone dirección; cancelar aceptado retira permisos/cache.
- [ ] AC-004: Solo cliente dueño califica done; acción desde otro usuario se rechaza.

Suites y gate: [roles y gates](../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: H01/H02: pendiente/rechazado no expone teléfono/domicilio; cancelar retira también caché/rutas; dos aceptaciones simultáneas dejan solo una asignación activa.

- [ ] AC-100 → FR-100/NFR-100: Cancel/rechazo no devuelven teléfono, dirección precisa o ruta; ok/done exige participante y consentimiento vigente; carrera de aceptación deja una asignación y evento coherente, nunca dos.

- [ ] AC-258 → FR-258/NFR-001/NFR-003: EP-058: Idempotency-Key; impedir activos duplicados; verificar interés si origin=post; heredar de solicitud; directo chat por defecto y both solo por consentimiento explícito. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-259 → FR-259/NFR-001/NFR-003: EP-059: Filtrar clientId o providerId derivado de sesión Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-260 → FR-260/NFR-001/NFR-003: EP-060: Domicilio solo según estado y actor; no basta ocultarlo en Flutter Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-261 → FR-261/NFR-001/NFR-003: EP-061: Transacción pedido+solicitud+outbox; solo pend; dirección después de commit Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-262 → FR-262/NFR-001/NFR-003: EP-062: Liberar referencia de solicitud y retirar acceso privado Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-263 → FR-263/NFR-001/NFR-003: EP-063: Solo pend/ok; invalidar autorización de domicilio y emitir evento Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-264 → FR-264/NFR-001/NFR-003: EP-064: Solo ok; cerrar solicitud y habilitar calificación del cliente Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-265 → FR-265/NFR-001/NFR-003: EP-065: Solo pend/ok; notificar cambio; no aceptar direcciones ajenas en evento público Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-288 → FR-288/NFR-001/NFR-003: EP-088: Devuelve solo pertenencia/done/providerId, nunca domicilio Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-605 → FR-605/NFR-002/NFR-003: Una asignación activa por solicitud y transiciones condicionales. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.

- [ ] AC-702 → FR-702/NFR-001/NFR-003: Cancel/rechazo retira dirección precisa, caché y ruta; solo zona pública permanece.

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
| FR-001 | T-001, T-002, T-010, T-003 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100 |
| FR-002 | T-001, T-002, T-011, T-003 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100 |
| NFR-001 | T-010, T-011, T-004, T-258, T-259, T-260, T-261, T-262, T-263, T-264, T-265, T-288, T-702 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-258, AC-259, AC-260, AC-261, AC-262, AC-263, AC-264, AC-265, AC-288, AC-702 |
| NFR-002 | T-001, T-007, T-005, T-605 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-605 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-258, T-259, T-260, T-261, T-262, T-263, T-264, T-265, T-288, T-605, T-702 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-258, AC-259, AC-260, AC-261, AC-262, AC-263, AC-264, AC-265, AC-288, AC-605, AC-702 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100 |
| FR-258 | T-258 | AC-258 |
| FR-259 | T-259 | AC-259 |
| FR-260 | T-260 | AC-260 |
| FR-261 | T-261 | AC-261 |
| FR-262 | T-262 | AC-262 |
| FR-263 | T-263 | AC-263 |
| FR-264 | T-264 | AC-264 |
| FR-265 | T-265 | AC-265 |
| FR-288 | T-288 | AC-288 |
| FR-605 | T-605 | AC-605 |
| FR-702 | T-702 | AC-702 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.
