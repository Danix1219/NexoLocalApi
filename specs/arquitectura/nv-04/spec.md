# NV-04 — Identidad, sesión y aislamiento de cuenta

Versión: 0.5.0 | Fecha: 2026-11-06 | Rama prevista: `nv-04`

> **Alineación E-001.** Este incremento se implementa en el módulo **Identity** de la API modular por dominios, con datos en **`nexo_identity`**. No es un servicio desplegable independiente: comparte host, se despliega en una app de App Service con **2 instancias**, y el estado compartido vive en **Redis**. Los identificadores `S01`–`S08` se conservan como **mapa de dominios**. Ver [mapa de specs](../README.md) y [constitución §4](../../../.specify/memory/constitution.md).

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§2/5/6/8; D04/D05.

Identidad, sesión y aislamiento de cuenta

Identidad, sesión y aislamiento de cuenta. Servicios involucrados: S01.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 01, 07, 10, 15, 16. Componentes: M02, M03, M24. Hallazgos: H03.

Dependencias: nv-03; subset inicial EP-001/002/003/004/005/007/008. Borrado distribuido cierra en nv-12.

- FR-001: Visitante crea cuenta verificando control real de teléfono y consentimientos.
- FR-002: Usuario inicia, renueva y cierra sesión en cualquiera de dos instancias.
- FR-003: Integrar recuperación/edición/sesiones de este spec y solicitud de borrado de nv-12 con reautenticación; identidad mínima no las entrega por su subset.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- No aceptar OTP demo ni conservar contraseña en claro.
- JWT, refresh rotatorio y revocación Redis coordinada; autorización por sid/userId.
- Borrado bloquea acceso de inmediato, pero no informa completado sin confirmaciones.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Separar identidad mínima de recuperación, listado/revocación de sesiones y arranque de borrado; revocación durable más Redis entre réplicas.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Logout offline limpia acceso local y muestra revocación remota pendiente; recuperar contraseña invalida sesiones según política ratificada; refresh concurrente no crea bucles.

- FR-100: Especificar recuperación, sesiones por dispositivo y logout/revocación inmediata durable compartida, con refresh concurrente/replay y cierre de WS/dispositivo push.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/5/6/8; D04/D05.

### Historias verificables por operación propia

- FR-201: EP-001 — Solicitar verificación o recuperación del teléfono; actor Público limitado; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-202: EP-002 — Comprobar código y obtener prueba de control; actor Público con desafío; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-203: EP-003 — Crear cuenta única y consentimiento; actor Público con prueba OTP; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-204: EP-004 — Iniciar sesión con teléfono y contraseña; actor Público limitado; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-205: EP-005 — Renovar sesión sin login repetido; actor Poseedor de refresh; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-207: EP-007 — Cerrar sesión actual en backend y dispositivos; actor Cuenta autenticada; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-208: EP-008 — Recuperar cuenta, capacidades y preferencias; actor Propietario; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-273: EP-073 — Emitir ticket de conexión WebSocket; actor Cuenta autenticada; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-301: EP-101 — Revalidar visibilidad de autores antes de publicar resultados derivados; actor Identidad S02/S03/S08 autorizada; autoridad S01. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-602: S01 Identidad: Cuenta, OTP, credenciales, JWT, sesiones, consentimiento y borrado. Datos propios: nl_identity + Redis seguridad.
- FR-410: R10 Logout: Atender el requisito académico 10 en su superficie y coordinar la contraparte.

- FR-703: Corregir H03: Aislamiento entre cuentas; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../../App_NexoLocal/specs/004-identidad-sesion/spec.md).

### Asignación primaria vigente 0.4.0

- EP-001 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-002 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-003 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-004 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-005 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-007 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-008 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-073 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-101 → [ficha](../../../docs/contracts/fichas-operaciones.md)

| Operación integrada | Nuevo dueño de incremento |
|---|---|
| EP-006 | [este spec](spec.md) |
| EP-009 | [este spec](spec.md) |
| EP-010 | [este spec](spec.md) |
| EP-011 | [este spec](spec.md) |

Los FR/AC heredados de esta fase comprueban integración con esas capacidades; sus tareas EP y casos de uso se ejecutan únicamente en la spec primaria del inventario. El propietario S01–S08 no cambia.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S01: accounts, otp_challenges, sessions y deletions; teléfono normalizado único, proofToken de un uso, refresh hash/familia, estado durable. App: nunca OTP/contraseña persistidos; no JWT eterno.

## 5. Seguridad

- No aceptar OTP demo ni conservar contraseña en claro.
- JWT, refresh rotatorio y revocación Redis coordinada; autorización por sid/userId.
- Borrado bloquea acceso de inmediato, pero no informa completado sin confirmaciones.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: OTP incorrecto, expirado, reutilizado y límite de intentos se rechazan.
- [ ] AC-002: Ana sale y Carlos entra: Carlos no obtiene caché, borradores, experiencias o sesión de Ana.
- [ ] AC-003: Logout en instancia A hace rechazar el token anterior en B; fallo Redis no autoriza por omisión.
- [ ] AC-004: Reenvío y recuperación no enumeran cuentas; refresh repetido detecta replay.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Logout offline limpia acceso local y muestra revocación remota pendiente; recuperar contraseña invalida sesiones según política ratificada; refresh concurrente no crea bucles.

- [ ] AC-100 → FR-100/NFR-100: Revocar en A impide acceso en B y conexiones abiertas; Redis caído consulta fuente durable o niega; recuperación no enumera y la política documenta qué otras sesiones invalida.

- [ ] AC-201 → FR-201/NFR-001/NFR-003: EP-001: Respuesta neutral; límites por IP/teléfono; no devolver código Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-202 → FR-202/NFR-001/NFR-003: EP-002: Verificar intentos, propósito, expiración y no reutilización Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-203 → FR-203/NFR-001/NFR-003: EP-003: Teléfono único; nombre moderado; secreto protegido Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-204 → FR-204/NFR-001/NFR-003: EP-004: Error neutral; hash adaptativo; nunca aceptar cuentas demo en release Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-205 → FR-205/NFR-001/NFR-003: EP-005: Rotación atómica; replay revoca familia; coordinación de peticiones móviles Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-207 → FR-207/NFR-001/NFR-003: EP-007: Revocación durable y Redis; cierre WebSocket; operación idempotente Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-208 → FR-208/NFR-001/NFR-003: EP-008: El modo de UI no concede privilegios Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-273 → FR-273/NFR-001/NFR-003: EP-073: Guardar hash/TTL en Redis de seguridad; no JWT en URL duradera Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-301 → FR-301/NFR-001/NFR-003: EP-101: Consulta interna de lectura, no mutation. Si hay borrado/suspensión, excluir autor y recursos según política del propietario; no devolver teléfono, secreto o ubicación. No log del cuerpo. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-602 → FR-602/NFR-002/NFR-003: Revocación comprobada en ambas réplicas. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.
- [ ] AC-410 → FR-410/NFR-002/NFR-003: Revocar en backend y limpiar móvil; rechazo entre dos réplicas y WS; offline solo acredita cierre local mientras remoto siga pendiente. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

- [ ] AC-703 → FR-703/NFR-001/NFR-003: Ana publica/guarda y sale; Carlos no recibe como propia autoría, borrador, cola o caché de Ana; IDs del token, nunca Tú/me.

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
| FR-003 | T-001, T-002, T-012, T-003 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100 |
| NFR-001 | T-010, T-011, T-012, T-004, T-201, T-202, T-203, T-204, T-205, T-207, T-208, T-273, T-301, T-703 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-201, AC-202, AC-203, AC-204, AC-205, AC-207, AC-208, AC-273, AC-301, AC-703 |
| NFR-002 | T-001, T-007, T-005, T-602, T-410 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-410, AC-602 |
| NFR-003 | T-002, T-010, T-011, T-012, T-021, T-003, T-005, T-201, T-202, T-203, T-204, T-205, T-207, T-208, T-273, T-301, T-602, T-410, T-703 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-201, AC-202, AC-203, AC-204, AC-205, AC-207, AC-208, AC-273, AC-301, AC-410, AC-602, AC-703 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100 |
| FR-201 | T-201 | AC-201 |
| FR-202 | T-202 | AC-202 |
| FR-203 | T-203 | AC-203 |
| FR-204 | T-204 | AC-204 |
| FR-205 | T-205 | AC-205 |
| FR-207 | T-207 | AC-207 |
| FR-208 | T-208 | AC-208 |
| FR-273 | T-273 | AC-273 |
| FR-301 | T-301 | AC-301 |
| FR-602 | T-602 | AC-602 |
| FR-410 | T-410 | AC-410 |
| FR-703 | T-703 | AC-703 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S01 Identity. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: subset inicial EP-001/002/003/004/005/007/008; reutilizar scaffold de nv-00. Ubicacion objetivo: src/Domains/identity/ y services/identity/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil S01](../../../docs/arquitectura/servicios/s01-identity.md) y [políticas comunes](../../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de S01 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |


## Conciliacion vigente de identidad (2026-11-08)

La decision vigente de correo SMTP y movil declarado esta en [el registro](../../../docs/decisions/registro.md).
El estado implementado esta en [el indice](../../../docs/endpoints.md); las referencias EP de la linea
base no son aliases HTTP. Esta correccion no implementa nuevas operaciones de cuenta.

- FR-970: El envio publico acepta signup y changeEmail; changeEmail requiere sesion con sub UUID valido. Recuperacion se solicita por password/forgot.
- NFR-970: La verificacion por correo no acredita posesion del movil; no publicar una insignia de telefono confirmado ni exigir SMS en las fichas vigentes.
- [ ] AC-970: Signup conserva su flujo; changeEmail sin sesion responde 401 y no encola; con sesion encola el codigo; resetPassword en la ruta de registro responde 400. Las fichas distinguen contacto declarado de correo verificado.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-970 | T-970 | AC-970 |
| NFR-970 | T-970 | AC-970 |


## Cuenta local autorizada — 2026-11-09

El encargo autoriza las ocho operaciones vigentes de [02-cuenta](../../endpoints/02-cuenta/implementacion.md), sobre Identity existente. La saga global conserva aceptación pendiente en nv-12.

- FR-981: Consultar exclusivamente la cuenta propia mediante proyección sin hash y respuesta privada sin caché.
- [ ] AC-981: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Consulta_propia_proyecta_solo_datos_publicables_y_no_acepta_otro_id.
- FR-982: Editar nombre moderado y avatar aprobado propio; distinguir ausente/null y rechazar propiedades no permitidas.
- [ ] AC-982: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Nombre_moderado_se_guarda_y_repetir_no_duplica_revision; Foto_exige_propiedad_aprobacion_y_finalidad_y_null_la_borra; Moderacion_fallida_o_rechazada_no_persiste.
- FR-983: Cambiar modo sin emitir tokens ni conceder perfil; informar si se requiere renovar por /connect/token.
- [ ] AC-983: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Modo_exige_perfil_y_el_refresco_relee_la_cuenta.
- FR-984: Normalizar el móvil como dato declarado sin OTP ni verificación de posesión y exigir correo verificado.
- [ ] AC-984: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Movil_se_normaliza_como_declarado_sin_otp.
- FR-985: Registrar dispositivo por token único; impedir reasignación activa reciente y expirar tras 90 días.
- [ ] AC-985: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Dispositivo_repetido_no_duplica_y_borrado_ajeno_no_revela_existencia; Dispositivo_inactivo_se_reasigna_conservando_un_solo_registro.
- FR-986: Eliminar dispositivo con pertenencia en el mismo filtro; ausente y ajeno devuelven el mismo 404.
- [ ] AC-986: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Dispositivo_repetido_no_duplica_y_borrado_ajeno_no_revela_existencia.
- FR-987: Solicitar baja con contraseña, plazo estable de 30 días, evento durable y revocación de sesiones previas.
- [ ] AC-987: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Solicitud_repetida_no_adelanta_la_baja_y_el_motivo_tiene_limite; Baja_cancelacion_y_refresco_no_reactivan_tokens_anteriores.
- FR-988: Cancelar baja dentro de gracia; no devolver conteos de otros dominios; recuperación restringida hasta renovar.
- [ ] AC-988: La operación cumple su ficha vigente, 401 sin sesión, validación y regla propia; evidencia en CuentaTests.Cancelar_sin_baja_es_409_y_ventana_vencida_es_410; Baja_cancelacion_y_refresco_no_reactivan_tokens_anteriores.
- NFR-981: Guardar cambio y evento de forma atómica, usar revisión optimista, transferir al outbox sin duplicar y aplicar auth_epoch/recovery_only entre réplicas. Sin hash ni secretos en DTO o logs.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-981 | T-981 | AC-981 |
| FR-982 | T-982 | AC-982 |
| FR-983 | T-983 | AC-983 |
| FR-984 | T-984 | AC-984 |
| FR-985 | T-985 | AC-985 |
| FR-986 | T-986 | AC-986 |
| FR-987 | T-987 | AC-987 |
| FR-988 | T-988 | AC-988 |
| NFR-981 | T-981, T-982, T-983, T-984, T-985, T-986, T-987, T-988 | AC-981, AC-982, AC-983, AC-984, AC-985, AC-986, AC-987, AC-988 |


## Incremento 2026-11-10 — correo y ubicación
- FR-990: Enviar códigos de alta/cambio/recuperación en HTML con identidad Nexo y alternativa de texto.
- NFR-990: Sin imágenes remotas ni inyección HTML; caducidad original en hora del centro de México.
- [ ] AC-990: Plantilla y MIME verificados por CodeEmailTemplateTests; sin afirmar entrega Gmail real.

| Requisito | Tarea | Criterio |
|---|---|---|
| FR-990 | T-990 | AC-990 |
| NFR-990 | T-990 | AC-990 |

Estado del incremento: implementación y pruebas registradas en el contrato incremental; aceptación documental/humana pendiente. No supone cerrar el resto del dominio.
