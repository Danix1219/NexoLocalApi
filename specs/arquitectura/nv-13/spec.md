# NV-13 — ML y asistencia generativa evaluables

Versión: 0.5.0 | Fecha: 2026-11-06 | Rama prevista: `nv-13`

> **Alineación E-001.** Incremento **transversal**: la moderación y la asistencia generativa son **adaptadores de Infrastructure** consumidos por los módulos que publican contenido, no un servicio aparte. El ML en dispositivo corresponde a la app. No es un servicio desplegable independiente: comparte host, se despliega en una app de App Service con **2 instancias**, y el estado compartido vive en **Redis**. Los identificadores `S01`–`S08` se conservan como **mapa de dominios**. Ver [mapa de specs](../README.md) y [constitución §4](../../../.specify/memory/constitution.md).

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§2/3/5/6/11; D04/D06.

ML y asistencia generativa evaluables

ML y asistencia generativa evaluables. Servicios involucrados: S06, Flutter.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 07, 16. Componentes: M25. Hallazgos: Sin hallazgo específico asociado.

Dependencias: nv-05, nv-08, nv-09; proveedor, dataset y cuota por ratificar.

- FR-001: Backend mantiene categorías/contrato y límites que consume la función ML móvil; inferencia local pertenece a 013-inteligencia, sin nuevo endpoint supuesto.
- FR-002: Usuario pide borrador generativo, lo edita y publica solo tras moderación.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Heurística de palabras no acredita ML; separar modelos/evaluación.
- No inventar garantías, licencias, precios o experiencia; abstenerse con baja confianza.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir asistencia generativa con autorización, límites, moderación y evaluación; no sustituir ML por reglas de palabras.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Evaluar macro-F1/abstención/latencia según función acordada y generación sin inventar credenciales/precios; sin proveedor no aprobar integración académica aunque edición manual funcione.

- FR-100: Delimitar ML de imagen como capacidad móvil propuesta y S06 como proveedor del borrador generativo EP-082, sin inventar endpoint de inferencia no aprobado.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/3/5/6/11; D04/D06.

### Historias verificables por operación propia

- FR-282: EP-082 — Generar borrador editable de solicitud o trabajo; actor Cuenta limitada; autoridad S06. Previsto en línea base; contrato formal y aceptación pendientes.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../../App_NexoLocal/specs/013-inteligencia/spec.md).

### Asignación primaria vigente 0.4.0

- EP-082 → [ficha](../../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

Modelo/dataset separados de datos productivos, procedencia/consentimiento, conjunto evaluación independiente. GenAI no recibe contacto/domicilio/conversaciones privadas. Métricas y umbrales se fijan antes del ensayo.

## 5. Seguridad

- Heurística de palabras no acredita ML; separar modelos/evaluación.
- No inventar garantías, licencias, precios o experiencia; abstenerse con baja confianza.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Dataset separado entrenamiento/evaluación con procedencia y consentimiento.
- [ ] AC-002: Modelo sin confianza se abstiene; usuario corrige categoría.
- [ ] AC-003: API generativa caída no publica; salida no aprobada no se guarda.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Evaluar macro-F1/abstención/latencia según función acordada y generación sin inventar credenciales/precios; sin proveedor no aprobar integración académica aunque edición manual funcione.

- [ ] AC-100 → FR-100/NFR-100: Backend limita uso/costo, excluye PII y no persiste salida sin aprobación; app demuestra ML real por separado; indisponibilidad permite redacción manual pero no aprueba aceptación académica.

- [ ] AC-282 → FR-282/NFR-001/NFR-003: EP-082: No persistir texto por defecto; sin datos precisos; moderar salida Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

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
| NFR-001 | T-010, T-011, T-004, T-282 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-282 |
| NFR-002 | T-001, T-007, T-005 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-282 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-282 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-282 | T-282 | AC-282 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: Propietarios de ML/GenAI, S06 y consumidores. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: ratificar dueño, proveedor y dataset; no crear un servicio IA generico por anticipado. Ubicacion objetivo: Application/Ports e Infrastructure del propietario definido para cada caso.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
