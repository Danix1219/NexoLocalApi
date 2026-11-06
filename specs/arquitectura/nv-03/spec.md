# NV-03 — Gates de calidad y DevSecOps

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-03`

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§6/8/11; D01/D03.

Gates de calidad y DevSecOps

Fase de fundamento previa a funcionalidad. Conserva el orden de aprendizaje del profesor sin copiar infraestructura incompatible.

## 2. Alcance / No-alcance

Requisitos Móvil v2: SDD, calidad, contratos y entrega; no reemplaza el alcance de funcionalidades.

Dependencias: nv-02.

- FR-001: Definir evidencia, gates bloqueantes y advisory, SAST/dependencias/secretos y ensayos de concurrencia/carga/recuperación según riesgo de la historia.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir gates backend por riesgo: unit, integración, seguridad, datos NoSQL, contrato, análisis y evidencia; pipeline futuro con rutas verificadas.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Clasificar 401/403, conflicto 409, validación 422, límite 429 y dependencia caída; confirmar semántica exacta en contrato antes de generar consumidores.

- FR-100: Definir evidencia, gates bloqueantes y advisory, SAST/dependencias/secretos y ensayos de concurrencia/carga/recuperación según riesgo de la historia.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§6/8/11; D01/D03.

### Funcionalidades y obligaciones con dueño explícito

- FR-407: R07 Pruebas: Atender el requisito académico 7 en su superficie y coordinar la contraparte.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).

Sin nuevas operaciones asignadas; revisar dependencias/consumo antes de cambiar contrato.

### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md).

### Asignación primaria vigente 0.4.0

Sin operaciones primarias nuevas: entregable de gobierno/operación, sin endpoint inventado.

## 4. Modelo de datos

Esta fase define fundamento/documentación, sin nuevo agregado de negocio. El modelo del producto se consulta en [datos por propietario](../../../docs/modelo-datos.md); no presupone bases o proyectos creados.

### Diseño específico del incremento

API: evidencia por suite y propietario. App: modelos desde contrato cuando se autoricen generadores, cursores/versiones y estado de sincronización; no DTO manual divergente.

## 5. Seguridad

- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Documentos y límites coherentes con la constitución; lint documental sin referencias rotas.
- [ ] AC-002: Cuando se implemente esta fase, comandos y rutas reales constan en plan y evidencia; sin verdes prestados del profesor.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Clasificar 401/403, conflicto 409, validación 422, límite 429 y dependencia caída; confirmar semántica exacta en contrato antes de generar consumidores.

- [ ] AC-100 → FR-100/NFR-100: Cada gate indica propósito, entrada, herramienta por ratificar, alcance, salida/exit y responsable; 45%/80% son propuestas; mock, integración real y prueba de réplica no se confunden.

- [ ] AC-407 → FR-407/NFR-002/NFR-003: Preparar y ejecutar unitarias, seguridad e integración de ambas partes; distinguir dobles de MongoDB/Redis/proveedor/Android reales. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

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
| FR-001 | T-001, T-002, T-010, T-003 | AC-001, AC-002, AC-090, AC-100 |
| NFR-001 | T-010, T-004 | AC-001, AC-002, AC-090, AC-100 |
| NFR-002 | T-001, T-007, T-005, T-407 | AC-001, AC-002, AC-090, AC-100, AC-407 |
| NFR-003 | T-002, T-010, T-021, T-003, T-005, T-407 | AC-001, AC-002, AC-090, AC-100, AC-407 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-090, AC-100 |
| FR-407 | T-407 | AC-407 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: Calidad por proyecto habilitado. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: transicion de CI documental a CI de producto y dependencias de arquitectura verificables. Ubicacion objetivo: .github/workflows/, .ci/, scripts/ y tests/ del propietario.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

- FR-951: Sustituir el bloqueo documental por CI real de producto.
- [ ] AC-951 → FR-951/NFR-950: Con proyecto existente, CI ejecuta comandos de rutas reales y falla al romper build/test o arquitectura; lint no puede ser el unico gate verde. Registrar resultados/limites sin afirmar ejecuciones aun pendientes.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-951 | T-951, T-003 | AC-951 |
