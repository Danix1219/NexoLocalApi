# NV-12 — Notificaciones y eliminación distribuida completa

Versión: 0.5.0 | Fecha: 2026-11-06 | Rama prevista: `nv-12`

> **Alineación E-001.** Este incremento se implementa en **Engagement** (notificaciones) e **Identity** (borrado de cuenta), con datos en **`nexo_engagement`** y **`nexo_identity`**. El borrado se propaga por **eventos de outbox** a los tres módulos; al compartir host no requiere coordinación distribuida entre servicios. No es un servicio desplegable independiente: comparte host, se despliega en una app de App Service con **2 instancias**, y el estado compartido vive en **Redis**. Los identificadores `S01`–`S08` se conservan como **mapa de dominios**. Ver [mapa de specs](../README.md) y [constitución §4](../../../.specify/memory/constitution.md).

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§2/4/6/9; D05.

Notificaciones y eliminación distribuida completa

Notificaciones y eliminación distribuida completa. Servicios involucrados: S07, S01, S02, S03, S04, S05, S06.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 07, 10, 15, 16. Componentes: M22, M24. Hallazgos: H01, H02, H03.

Dependencias: nv-04 para inicio de borrado; cierre global integra nv-05..11; S08 solo si habilitado.

- FR-001: Integrar notificaciones/dispositivos de este spec con logout y borrado; la bandeja/push tiene implementación primaria en este spec.
- FR-002: Cuenta solicita borrado y consulta progreso hasta confirmación de todos.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Push es aviso, bandeja durable deduplica; logout desvincula token de dispositivo.
- Evento mínimo; destino revalida permisos al abrir.
- Saga no declara completado al recibir solicitud. S08 confirma si está habilitado.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir dispositivos, bandeja y delivery push; saga de eliminación con acuse por servicio y reintentos/reconciliación.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Push a sesión anterior no entrega datos de nueva cuenta; borrado parcial no informa completado; cuenta bloqueada de inmediato; restaurar backup aplica supresiones antes de servir.

- FR-100: Precisar reintentos/dedupe de notificación y saga de borrado por todos los propietarios habilitados, incluidos objetos/caché/colas/proyecciones y restauración de backups.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/4/6/9; D05.

### Historias verificables por operación propia

- FR-212: EP-012 — Solicitar eliminación integral; actor Propietario reautenticado; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-213: EP-013 — Consultar progreso del borrado; actor Credencial limitada al trámite; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../../App_NexoLocal/specs/012-notificaciones-borrado/spec.md).

### Asignación primaria vigente 0.4.0

- EP-012 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-013 → [ficha](../../../docs/contracts/fichas-operaciones.md)

| Operación integrada | Nuevo dueño de incremento |
|---|---|
| EP-083 | [este spec](spec.md) |
| EP-084 | [este spec](spec.md) |
| EP-085 | [este spec](spec.md) |
| EP-086 | [este spec](spec.md) |

Los FR/AC heredados de esta fase comprueban integración con esas capacidades; sus tareas EP y casos de uso se ejecutan únicamente en la spec primaria del inventario. El propietario S01–S08 no cambia.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S07: devices/notifications/deliveries; S01 deletions y acuses de cada servicio habilitado. Dedupe por eventId/destinatario; tombstones impiden reaparición. Retención/backups requieren decisión explícita.

## 5. Seguridad

- Push es aviso, bandeja durable deduplica; logout desvincula token de dispositivo.
- Evento mínimo; destino revalida permisos al abrir.
- Saga no declara completado al recibir solicitud. S08 confirma si está habilitado.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Sin permiso push se conserva bandeja.
- [ ] AC-002: Reentrega de evento no duplica notificación ni efecto de borrado.
- [ ] AC-003: Borrado parcial muestra pendiente; cuenta queda bloqueada; backups/retención documentados.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Push a sesión anterior no entrega datos de nueva cuenta; borrado parcial no informa completado; cuenta bloqueada de inmediato; restaurar backup aplica supresiones antes de servir.

- [ ] AC-100 → FR-100/NFR-100: Cuenta bloqueada al inicio; falta de acuse deja trámite pendiente, reentrega no duplica efecto; reaplicar supresiones al restaurar evita resucitar datos; retención se ratifica, no se inventa.

- [ ] AC-212 → FR-212/NFR-001/NFR-003: EP-012: Bloquear cuenta y emitir evento; no afirmar completado al recibir Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-213 → FR-213/NFR-001/NFR-003: EP-013: Receipt solo permite consultar este trámite, nunca reabrir cuenta Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

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
| FR-001 | T-001, T-002, T-010, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-002 | T-001, T-002, T-011, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-001 | T-010, T-011, T-004, T-212, T-213 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-212, AC-213 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-212, T-213 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-212, AC-213 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-212 | T-212 | AC-212 |
| FR-213 | T-213 | AC-213 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S01/S07 y cada propietario activo. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: borrado inicia en identidad; cierre distribuido integra dueños sin acceder a sus bases. Ubicacion objetivo: adaptadores/outbox/inbox de cada servicio y tests/EndToEnd/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Incremento 2026-11-26 · baja de cuenta completa entre dominios

Estado: implementado y probado de forma local; revisión humana y aceptación pendientes.

- FR-DEL-001: Cada dominio implementa `IAccountDataLifecycle` (ocultar, restaurar, borrar) sobre su propia base. Las tres operaciones son idempotentes.
- FR-DEL-002: Al pedir la baja se ocultan perfil y trabajos, se cierran las solicitudes abiertas, se cancelan los pedidos `pending` y las conversaciones no admiten mensajes nuevos. El ciclo corre cada 10 s.
- FR-DEL-003: Al cancelar dentro del plazo vuelven perfil, trabajos y conversaciones. Solicitudes cerradas y pedidos cancelados no vuelven.
- FR-DEL-004: A los 30 días se borra en todos los dominios. Los pedidos `accepted` se cancelan. Solo sobreviven pedidos resueltos (sin texto ni domicilio) y opiniones. El perfil queda como cascarón sin datos porque lo referencian.
- FR-DEL-005: Cada dominio confirma al terminar (`erasedDomains`). La cuenta se anonimiza al final y el correo queda libre. Un borrado parcial sigue como pendiente y se reintenta.
- FR-DEL-006: Un lease por cuenta permite varias réplicas. `Accounts:Deletion:DisableWorker=true` detiene el ciclo sin tocar datos.
- NFR-DEL-001: Los registros no incluyen datos personales; se borran los mensajes de outbox de Identity que contienen el correo o la cuenta.
- FR-DEL-007: `chat_attachments` anota quién sube cada adjunto cifrado antes de guardarlo; otra cuenta no puede usar ese identificador (409). A los 30 días se borran los objetos de la cuenta; los de la otra persona se conservan. Los subidos antes de este registro no tienen autor conocido.
- FR-DEL-008: Al completar la baja se envía un último correo. La dirección vive solo en ese mensaje y se sustituye por `{}` al enviarlo o a los 7 días si no se pudo enviar.

Pendiente y decisiones por ratificar (D05):
- Respaldos (AC-100): hay un [procedimiento propuesto](../../../docs/runbook-release.md) para no resucitar cuentas al restaurar, sin ensayar. Faltan decidir la retención y dónde guardar la lista de bajas fuera de la base. La retención de borrado reversible del almacenamiento de archivos tampoco está definida.
- Los reportes se conservan sin el texto libre; los contadores de cuota no se purgan.
- Los pedidos no resueltos se eliminan, así que la contraparte deja de verlos en su historial.
- EP-013 (consultar el progreso) no está implementado: su contrato sigue en diseño y ninguna pantalla lo consume.
- Si el correo falla, el error del envío queda en `lastError` del mensaje hasta que se envía o vence.
