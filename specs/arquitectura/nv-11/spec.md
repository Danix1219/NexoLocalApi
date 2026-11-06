# NV-11 — Conversaciones, mensajes y avisos en vivo

Versión: 0.5.0 | Fecha: 2026-11-06 | Rama prevista: `nv-11`

> **Alineación E-001.** Este incremento se implementa en el módulo **Engagement**, con tiempo real sobre **SignalR + Redis** como backplane entre las 2 instancias de la API modular por dominios, con datos en **`nexo_engagement`**. No es un servicio desplegable independiente: comparte host, se despliega en una app de App Service con **2 instancias**, y el estado compartido vive en **Redis**. Los identificadores `S01`–`S08` se conservan como **mapa de dominios**. Ver [mapa de specs](../README.md) y [constitución §4](../../../.specify/memory/constitution.md).

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§2/4/5/6/8; H05/H11.

Conversaciones, mensajes y avisos en vivo

Conversaciones, mensajes y avisos en vivo. Servicios involucrados: S05, S04, S06, S02.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 04, 07, 09, 15, 16. Componentes: M18, M19. Hallazgos: H05, H11.

Dependencias: nv-10, nv-05, nv-04.

- FR-001: Participantes intercambian texto/foto y recuperan historial al reconectar.
- FR-002: Usuario filtra no leídos y bloquea/desbloquea.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- REST persiste y confirma; WS avisa; Redis Pub/Sub no conserva historial.
- Pareja orientada cliente/prestador por IDs; contexto de pedido/solicitud por mensaje.
- Respuesta rápida usa mismo SendMessage sin sobrescribir con textarea vacío.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir persistencia de mensajes, bloqueos, ticket temporal y avisos WS; historial durable y contexto por mensaje.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; H05/H11: texto rápido/manual utiliza misma intención; reintento mismo ID da una entrega; nuevo pedido cambia cabecera sin reescribir mensajes; logout invalida conexión existente.

- FR-100: Definir ticket temporal de conexión, membresía y revocación durante WS, secuencia durable, bloqueo bilateral y reanudación tras avisos perdidos.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/4/5/6/8; H05/H11.

### Historias verificables por operación propia

- FR-266: EP-066 — Abrir o recuperar conversación por pareja; actor Cliente o prestador habilitado; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-267: EP-067 — Listar bandeja y no leídos por modo; actor Participante; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-268: EP-068 — Recuperar historial o mensajes tras desconexión; actor Miembro; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-269: EP-069 — Enviar texto o foto aprobados; actor Miembro sin bloqueo; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-270: EP-070 — Marcar lectura hasta secuencia; actor Miembro; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-271: EP-071 — Bloquear a contraparte; actor Miembro; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-272: EP-072 — Desbloquear contraparte; actor Autor del bloqueo; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-274: EP-074 — Recibir avisos de mensajes y pedidos; actor Ticket y membresía vigentes; autoridad S05. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-606: S05 Mensajería: Conversaciones, historial, lectura, bloqueo y eventos en vivo. Datos propios: nl_messaging + Redis Pub/Sub.

- FR-705: Corregir H05: Respuesta rápida real; integrar propietarios implicados sin duplicar regla.

- FR-711: Corregir H11: Contexto de chat por mensaje; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../../App_NexoLocal/specs/011-mensajeria/spec.md).

### Asignación primaria vigente 0.4.0

- EP-066 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-067 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-068 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-069 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-070 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-071 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-072 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-074 → [ficha](../../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S05: conversations/messages/blocks; unique conversationId/clientMessageId, sequence durable y contexto requestId/orderId por mensaje; Redis Pub/Sub no historial. WS no lleva JWT en URL persistible.

## 5. Seguridad

- REST persiste y confirma; WS avisa; Redis Pub/Sub no conserva historial.
- Pareja orientada cliente/prestador por IDs; contexto de pedido/solicitud por mensaje.
- Respuesta rápida usa mismo SendMessage sin sobrescribir con textarea vacío.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Mensaje manual y rápido aparecen una vez con el mismo clientMessageId reintentado.
- [ ] AC-002: Bloqueo en cualquier dirección impide nuevos envíos autorizados; conservar historial permitido.
- [ ] AC-003: Nuevo pedido no cambia contexto histórico; cabecera distingue pedido activo.
- [ ] AC-004: Reconexión recupera secuencia faltante aunque se pierda un aviso WS.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: H05/H11: texto rápido/manual utiliza misma intención; reintento mismo ID da una entrega; nuevo pedido cambia cabecera sin reescribir mensajes; logout invalida conexión existente.

- [ ] AC-100 → FR-100/NFR-100: Logout/expiración/revocación corta uso de conexión; no token persistible en URL/log; mismo clientMessageId confirma una vez; aviso perdido se recupera de historial autorizado por cursor.

- [ ] AC-266 → FR-266/NFR-001/NFR-003: EP-066: Prestador inicia solo tras asignación; comprobar bloques y membresía; consultar chatEnabled actual para nueva conversación; acceso por rol/contexto autorizado. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-267 → FR-267/NFR-001/NFR-003: EP-067: No exponer conversaciones de otro modo/miembro Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-268 → FR-268/NFR-001/NFR-003: EP-068: Autorización en cada página; adjuntos privados bajo control Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-269 → FR-269/NFR-001/NFR-003: EP-069: Moderar antes de persistir; retry devuelve mismo mensaje; no respuesta simulada; texto 1..600; respuesta rápida y manual comparten SendMessage sin sobrescribir el texto seleccionado. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-270 → FR-270/NFR-001/NFR-003: EP-070: Monótono; no avanzar más allá de mensaje disponible Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-271 → FR-271/NFR-001/NFR-003: EP-071: Revalidar bloqueo al enviar; no solo silenciar UI Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-272 → FR-272/NFR-001/NFR-003: EP-072: No retirar bloqueo impuesto por la otra persona Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-274 → FR-274/NFR-001/NFR-003: EP-074: Validar membresía, sesión y bloqueos durante conexión; reanudar por REST Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-606 → FR-606/NFR-002/NFR-003: Reintentos sin duplicados y reconexión por cursor. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.

- [ ] AC-705 → FR-705/NFR-001/NFR-003: Respuesta rápida y manual usan la misma intención; texto elegido no se sobrescribe por campo vacío y reintento no duplica.

- [ ] AC-711 → FR-711/NFR-001/NFR-003: Nueva orden no reescribe contexto histórico; mensaje/evento tiene orderId/requestId autorizado y cabecera muestra pedido activo.

## 7. Plan de entrega

Spec→plan→task→tests/gates→Memoria→checklist→revisión→release. No desplegar por escribir una spec. Ver [runbook](../../../docs/runbook-release.md).

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
| NFR-001 | T-010, T-011, T-004, T-266, T-267, T-268, T-269, T-270, T-271, T-272, T-274, T-705, T-711 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-266, AC-267, AC-268, AC-269, AC-270, AC-271, AC-272, AC-274, AC-705, AC-711 |
| NFR-002 | T-001, T-007, T-005, T-606 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-606 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-266, T-267, T-268, T-269, T-270, T-271, T-272, T-274, T-606, T-705, T-711 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-266, AC-267, AC-268, AC-269, AC-270, AC-271, AC-272, AC-274, AC-606, AC-705, AC-711 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100 |
| FR-266 | T-266 | AC-266 |
| FR-267 | T-267 | AC-267 |
| FR-268 | T-268 | AC-268 |
| FR-269 | T-269 | AC-269 |
| FR-270 | T-270 | AC-270 |
| FR-271 | T-271 | AC-271 |
| FR-272 | T-272 | AC-272 |
| FR-274 | T-274 | AC-274 |
| FR-606 | T-606 | AC-606 |
| FR-705 | T-705 | AC-705 |
| FR-711 | T-711 | AC-711 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S05 Messaging. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: mensajes, pertenencia e idempotencia; upgrade WS de Gateway no posee mensajes. Ubicacion objetivo: src/Domains/messaging/ y services/messaging/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil S05](../../../docs/arquitectura/servicios/s05-messaging.md) y [políticas comunes](../../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de S05 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |
