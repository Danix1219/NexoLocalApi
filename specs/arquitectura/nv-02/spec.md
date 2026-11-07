# NV-02 — Molde de incremento y contratos

Versión: 0.5.0 | Fecha: 2026-11-06 | Rama prevista: `nv-02`

> **Alineación E-001.** Incremento **transversal**: aplica a los tres módulos (`Identity`, `Discovery`, `Engagement`) de la API modular por dominios, no a servicios desplegables independientes. Los identificadores `S01`–`S08` se conservan como **mapa de dominios**. Ver [mapa de specs](../README.md) y [constitución §4](../../../.specify/memory/constitution.md).

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§5/7/11; D01/D02.

Molde de incremento y contratos

Fase de fundamento previa a funcionalidad. Conserva el orden de aprendizaje del profesor sin copiar infraestructura incompatible.

## 2. Alcance / No-alcance

Requisitos Móvil v2: SDD, calidad, contratos y entrega; no reemplaza el alcance de funcionalidades.

Dependencias: nv-00.

- FR-001: Definir revisión y compatibilidad de contratos HTTP/WS/eventos por historia y revisión consumida por Flutter, separando públicas, internas y administrativas.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir contratos de frontera y matriz de versiones para el subset de una historia, con compatibilidad y ejemplos negativos.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Un consumidor puede reconocer nulos/enums/errores sin adivinar; una prueba con doble no se registra como integración real.

- FR-100: Definir revisión y compatibilidad de contratos HTTP/WS/eventos por historia y revisión consumida por Flutter, separando públicas, internas y administrativas.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§5/7/11; D01/D02.

### Funcionalidades y obligaciones con dueño explícito

- FR-404: R04 Correspondencia con maqueta: Atender el requisito académico 4 en su superficie y coordinar la contraparte.

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

API: inventario EP, contrato formal futuro y envelope de eventos. App: fixtures sintéticos sin credenciales; evidencia distingue dobles, servidor y dispositivo.

## 5. Seguridad

- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Documentos y límites coherentes con la constitución; lint documental sin referencias rotas.
- [ ] AC-002: Cuando se implemente esta fase, comandos y rutas reales constan en plan y evidencia; sin verdes prestados del profesor.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Un consumidor puede reconocer nulos/enums/errores sin adivinar; una prueba con doble no se registra como integración real.

- [ ] AC-100 → FR-100/NFR-100: Cada EP seleccionado identifica actor, tipos/nulos, errores, paginación, permisos por campo, reintento e idempotencia; un cambio incompatible tiene convivencia/migración antes del consumo.

- [ ] AC-404 → FR-404/NFR-002/NFR-003: Cubrir todos los M01–M27/W01 de la línea base y corregir H01–H12, sin eliminar funciones visibles ni promover simulaciones. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

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
| NFR-002 | T-001, T-007, T-005, T-404 | AC-001, AC-002, AC-090, AC-100, AC-404 |
| NFR-003 | T-002, T-010, T-021, T-003, T-005, T-404 | AC-001, AC-002, AC-090, AC-100, AC-404 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-090, AC-100 |
| FR-404 | T-404 | AC-404 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: Contratos por propietario. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: esquemas formales, errores y eventos; no crear un DTO universal ni duplicar autoridad en Flutter. Ubicacion objetivo: docs/contracts/ y adaptador Api/Contracts del propietario.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
