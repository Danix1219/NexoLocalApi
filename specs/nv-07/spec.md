# NV-07 — Búsqueda, mapa, comparación y feed público

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-07`

## 1. Objetivo

Constitución rectora: [fuente única](../../.specify/memory/constitution.md). Aplicación: Constitución §§4/5/6/9; D01/D03.

Búsqueda, mapa, comparación y feed público

Búsqueda, mapa, comparación y feed público. Servicios involucrados: S02, S03, G01.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 04, 09, 15, 16. Componentes: M01, M06, M07, M08, M10, M12. Hallazgos: H08.

Dependencias: nv-06; feed de comunidad espera nv-08, sin bloquear búsqueda de directorio.

- FR-001: Visitante busca por zona y categoría; registro al intentar acciones protegidas.
- FR-002: Cuenta compara dos prestadores con condiciones comparables.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Paginación estable, cobertura por modalidad y ampliación fuera de radio explícita.
- No etiquetar mejor precio cuando servicio/unidad/moneda/materiales no coinciden.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir búsqueda, mapas, sugerencias, comparación y composición gradual del feed; claves/TTL e invalidación DB/Redis.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; H08/H10: comparar solo serviceKey/unidad/moneda/materiales; local con radio cero aparece; denegar GPS conserva entrada manual; edición invalida caché entre réplicas.

- FR-100: Definir caché en base de datos y Redis por consulta pública, invalidación por versión, caducidad comprobada por lector e índices/paginación estable de consulta.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§4/5/6/9; D01/D03.

### Historias verificables por operación propia

- FR-225: EP-025 — Buscar y filtrar prestadores por necesidad y zona; actor Público; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-226: EP-026 — Autocompletar servicios y términos reconocidos; actor Público; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-227: EP-027 — Comparar exactamente dos prestadores; actor Cuenta autenticada; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-228: EP-028 — Buscar lugar para seleccionar ubicación; actor Público limitado; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-229: EP-029 — Obtener etiqueta de zona desde punto; actor Público limitado; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-230: EP-030 — Componer inicio por zona y modo; actor Público o cuenta según filtro; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-409: R09 Caché en tres niveles: Atender el requisito académico 9 en su superficie y coordinar la contraparte.

- FR-708: Corregir H08: Comparación equivalente; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Endpoints vivos](../../docs/endpoints.md) y [registro de contratos](../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../App_NexoLocal/specs/007-descubrimiento/spec.md).

### Asignación primaria vigente 0.4.0

- EP-025 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-026 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-027 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-028 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-029 → [ficha](../../docs/contracts/fichas-operaciones.md)
- EP-030 → [ficha](../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S02/G01: filtros normalizados, read_cache con expiresAt comprobado por lector, cursor estable y DTO público. App: recientes y resúmenes públicos; no origen privado ni contactos cacheados.

## 5. Seguridad

- Paginación estable, cobertura por modalidad y ampliación fuera de radio explícita.
- No etiquetar mejor precio cuando servicio/unidad/moneda/materiales no coinciden.

Ver [checklist seguridad](../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Tercer prestador en comparación se rechaza.
- [ ] AC-002: Contrato acepta zona manual sin exigir GPS y devuelve vacío distinguible de error; la contraparte móvil explica ampliar/cambiar zona.
- [ ] AC-003: Precio de electricidad no gana frente a plomería por ser numéricamente menor.

Suites y gate: [roles y gates](../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: H08/H10: comparar solo serviceKey/unidad/moneda/materiales; local con radio cero aparece; denegar GPS conserva entrada manual; edición invalida caché entre réplicas.

- [ ] AC-100 → FR-100/NFR-100: Una edición invalida búsqueda/perfil entre réplicas; TTL retrasado no sirve datos caducados; fallback público ante Redis caído respeta límites y nunca entrega PII ni decide autorización con caché.

- [ ] AC-225 → FR-225/NFR-001/NFR-003: EP-025: Cobertura real por prestador; orden estable; ubicación exacta no sale Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-226 → FR-226/NFR-001/NFR-003: EP-026: Tokens/prefijos indexados; debounce móvil; excluir búsqueda privada histórica Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-227 → FR-227/NFR-001/NFR-003: EP-027: Evitar inventar precio u horario faltante; route estática antes de {id}; destacar precio menor solo si servicio, moneda, unidad y materiales/alcance coinciden; de lo contrario no comparable y sin MEJOR. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-228 → FR-228/NFR-001/NFR-003: EP-028: Adaptador Azure Maps; comprobar licencia/cupos; limitar precisión pública Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-229 → FR-229/NFR-001/NFR-003: EP-029: No guardar consultas precisas en logs o caché pública sin anonimizar Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-230 → FR-230/NFR-001/NFR-003: EP-030: Gateway agrega DTO públicos; para mine exige usuario; fallos parciales explícitos; conservar trabajo cercano por cobertura de prestador o distancia del trabajo <=3 km según postNear, usando ubicación privada solo dentro del servidor. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-409 → FR-409/NFR-002/NFR-003: Demostrar caché DB/backend/frontend con hit/miss, caducidad, invalidación y aislamiento; no llamar caché solo al buffer del motor. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

- [ ] AC-708 → FR-708/NFR-001/NFR-003: Precio menor solo se destaca con servicio, unidad, moneda, alcance/materiales equivalentes; si falta dato, no comparable con razón.

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
| FR-001 | T-001, T-002, T-010, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-002 | T-001, T-002, T-011, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-001 | T-010, T-011, T-004, T-225, T-226, T-227, T-228, T-229, T-230, T-708 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-225, AC-226, AC-227, AC-228, AC-229, AC-230, AC-708 |
| NFR-002 | T-001, T-007, T-005, T-409 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-409 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-225, T-226, T-227, T-228, T-229, T-230, T-409, T-708 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-225, AC-226, AC-227, AC-228, AC-229, AC-230, AC-409, AC-708 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-225 | T-225 | AC-225 |
| FR-226 | T-226 | AC-226 |
| FR-227 | T-227 | AC-227 |
| FR-228 | T-228 | AC-228 |
| FR-229 | T-229 | AC-229 |
| FR-230 | T-230 | AC-230 |
| FR-409 | T-409 | AC-409 |
| FR-708 | T-708 | AC-708 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S02 Directory y composicion G01. [Mapa backend](../../docs/estructura-backend.md) y [orden SDD](../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: busqueda de directorio primero; feed comunitario integra S03 despues de nv-08. Ubicacion objetivo: services/directory/src/, nexolocal.Api/ y pruebas de contrato/recorrido.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
