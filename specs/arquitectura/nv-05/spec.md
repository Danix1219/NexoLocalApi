# NV-05 — Moderación y medios antes de persistir

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-05`

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§2/4/6/8; D04/D05.

Moderación y medios antes de persistir

Moderación y medios antes de persistir. Servicios involucrados: S06.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 07, 15, 16. Componentes: M23, M26. Hallazgos: Sin hallazgo específico asociado.

Dependencias: nv-04; contratos internos mínimos sin esperar todo el directorio.

- FR-001: Usuario envía texto o imagen y conoce aprobación/rechazo antes de publicar.
- FR-002: Reportante envía/consulta reportes; integrar revisión administrativa de este spec sin duplicar cola/decisión de moderación.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Vincular aprobación a contenido exacto, usuario y propósito; cambios exigen nueva validación.
- Medio de chat privado no recibe URL pública permanente.
- No persistir contenido rechazado ni cuerpos privados en logs.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Diseñar moderación previa y medios privados/públicos, reportes y decisiones; una red interna no sustituye autenticación.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Contenido editado invalida aprobación; proveedor caído impide persistir; un usuario no reutiliza aprobación ajena; reporte aceptado confirma persistencia real.

- FR-100: Definir trazabilidad y ciclo de vida de medios, decisiones de moderación y reportes sin persistir bytes/cuerpos rechazados en producto, proxy, temporales o logs.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/4/6/8; D04/D05.

### Historias verificables por operación propia

- FR-275: EP-075 — Moderar y registrar imagen; actor Cuenta; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-276: EP-076 — Resolver acceso autorizado a medio; actor Público o miembro según recurso; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-277: EP-077 — Retirar archivo autorizado; actor Propietario o proceso de eliminación; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-278: EP-078 — Reportar perfil, trabajo, experiencia o chat; actor Cuenta; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-279: EP-079 — Consultar estado de sus reportes; actor Reportante; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-287: EP-087 — Moderar texto de una operación de negocio; actor Identidad de servicio autorizada; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-299: EP-099 — Obtener motivos y política vigente de reporte; actor Cuenta autenticada; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-607: S06 Confianza y contenido: Moderación, medios, reportes, decisión y borrador IA. Datos propios: nl_trust + Blob.
- FR-416: R16 Móvil completo: Atender el requisito académico 16 en su superficie y coordinar la contraparte.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../../App_NexoLocal/specs/005-contenido-seguro/spec.md).

### Asignación primaria vigente 0.4.0

- EP-075 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-076 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-077 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-078 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-079 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-087 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-099 → [ficha](../../../docs/contracts/fichas-operaciones.md)

| Operación integrada | Nuevo dueño de incremento |
|---|---|
| EP-080 | [este spec](../este spec/spec.md) |
| EP-081 | [este spec](../este spec/spec.md) |

Los FR/AC heredados de esta fase comprueban integración con esas capacidades; sus tareas EP y casos de uso se ejecutan únicamente en la spec primaria del inventario. El propietario S01–S08 no cambia.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S06: media, reports y decisiones. Aprobación por hash/autor/propósito/política; no bytes rechazados en Blob, CDN, logs ni temporales. App: borrador privado local y eliminación al salir.

## 5. Seguridad

- Vincular aprobación a contenido exacto, usuario y propósito; cambios exigen nueva validación.
- Medio de chat privado no recibe URL pública permanente.
- No persistir contenido rechazado ni cuerpos privados en logs.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Moderación caída impide publicación nueva y permite lectura previa.
- [ ] AC-002: Hash/propósito ajeno no sirve como aprobación; MIME/tamaño inválidos se rechazan.
- [ ] AC-003: Reporte sin motivo no se guarda; moderador no modifica directamente base ajena.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Contenido editado invalida aprobación; proveedor caído impide persistir; un usuario no reutiliza aprobación ajena; reporte aceptado confirma persistencia real.

- [ ] AC-100 → FR-100/NFR-100: Rechazado o proveedor caído no persiste ni produce URL pública; hash/autor/finalidad ajenos no autorizan; decisión administrativa auditada se comunica al dueño por interfaz autorizada.

- [ ] AC-275 → FR-275/NFR-001/NFR-003: EP-075: Sin almacenamiento persistente previo; máximo 3 MB; validar contenido real Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-276 → FR-276/NFR-001/NFR-003: EP-076: Revalidar boundResource; no entregar URL privada a visitante Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-277 → FR-277/NFR-001/NFR-003: EP-077: Comprobar referencias y purgar CDN; no borrar medio ajeno Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-278 → FR-278/NFR-001/NFR-003: EP-078: Validar acceso al objetivo; no copiar conversación completa a logs Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-279 → FR-279/NFR-001/NFR-003: EP-079: No exponer identidad del moderador ni datos de terceros innecesarios Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-287 → FR-287/NFR-001/NFR-003: EP-087: API interna, sin persistir contenido rechazado ni log de cuerpo Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-299 → FR-299/NFR-001/NFR-003: EP-099: Complemento de contrato: frontend y backend comparten catálogo versionado de motivos; no recopilar datos privados en etiquetas. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-607 → FR-607/NFR-002/NFR-003: Rechazados no se persisten; medios privados no son públicos. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.
- [ ] AC-416 → FR-416/NFR-002/NFR-003: Demostrar moderación antes de persistir, push, aviso/datos/borrado, permisos, continuidad de sesión, SQLite/sincronización aplicable y funciones ML y GenAI reales distintas. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

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
| NFR-001 | T-010, T-011, T-004, T-275, T-276, T-277, T-278, T-279, T-287, T-299 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-275, AC-276, AC-277, AC-278, AC-279, AC-287, AC-299 |
| NFR-002 | T-001, T-007, T-005, T-607, T-416 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-416, AC-607 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-275, T-276, T-277, T-278, T-279, T-287, T-299, T-607, T-416 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-275, AC-276, AC-277, AC-278, AC-279, AC-287, AC-299, AC-416, AC-607 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-275 | T-275 | AC-275 |
| FR-276 | T-276 | AC-276 |
| FR-277 | T-277 | AC-277 |
| FR-278 | T-278 | AC-278 |
| FR-279 | T-279 | AC-279 |
| FR-287 | T-287 | AC-287 |
| FR-299 | T-299 | AC-299 |
| FR-607 | T-607 | AC-607 |
| FR-416 | T-416 | AC-416 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S06 Trust. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: moderacion y medios antes de persistir, con adaptadores y datos de S06. Ubicacion objetivo: services/trust/src/ y services/trust/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil S06](../../../docs/arquitectura/servicios/s06-trust.md) y [políticas comunes](../../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de S06 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |
