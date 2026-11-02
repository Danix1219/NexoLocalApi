# NV-00 — Constitución y gobierno

Versión: 0.4.0 | Fecha: 2026-11-02 | Rama prevista: `nv-00`

## 1. Objetivo

Constitución rectora: [fuente única](../../.specify/memory/constitution.md). Aplicación: Constitución §§2/7/10/11/12; D01–D08.

Constitución y gobierno

Fase de fundamento previa a funcionalidad. Conserva el orden de aprendizaje del profesor sin copiar infraestructura incompatible.

## 2. Alcance / No-alcance

Requisitos Móvil v2: SDD, calidad, contratos y entrega; no reemplaza el alcance de funcionalidades.

Dependencias: Ninguna; inicia el gobierno.

- FR-001: Mantener registro de decisiones, excepciones y cambios de alcance con fuente, motivo, estado y persona revisora pendiente hasta asignación.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Gobierno de fuentes, decisiones, responsables y versiones previas al desarrollo; Codex y Claude Code con traspaso secuencial.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Reconstruir una tarea documental desde fuente hasta checklist y distinguir propuesta de ratificación.

- FR-100: Mantener registro de decisiones, excepciones y cambios de alcance con fuente, motivo, estado y persona revisora pendiente hasta asignación.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/7/10/11/12; D01–D08.

### Funcionalidades y obligaciones con dueño explícito

- FR-402: R02 Evaluación interactiva y con prompts: Atender el requisito académico 2 en su superficie y coordinar la contraparte.
- FR-405: R05 SDD y DevSecOps: Atender el requisito académico 5 en su superficie y coordinar la contraparte.
- FR-406: R06 Desarrollo con agentes: Atender el requisito académico 6 en su superficie y coordinar la contraparte.
- FR-411: R11 Matriz de versiones: Atender el requisito académico 11 en su superficie y coordinar la contraparte.
- FR-412: R12 MCP opcional: Atender el requisito académico 12 en su superficie y coordinar la contraparte.
- FR-418: R18 GitHub/coordinación: Atender el requisito académico 18 en su superficie y coordinar la contraparte.
- FR-451: PRE1 Entrevistas: Preparar tarea previa 1 y conciliarla con el otro contexto.
- FR-452: PRE2 Descripciones separadas: Preparar tarea previa 2 y conciliarla con el otro contexto.
- FR-453: PRE3 Componentes en hoja de cálculo: Preparar tarea previa 3 y conciliarla con el otro contexto.
- FR-454: PRE4 CRUD y funciones por endpoint: Preparar tarea previa 4 y conciliarla con el otro contexto.
- FR-455: PRE5 Referencias docentes: Preparar tarea previa 5 y conciliarla con el otro contexto.
- FR-456: PRE6 Estructura SDD: Preparar tarea previa 6 y conciliarla con el otro contexto.

## 3. Contratos API

[Endpoints vivos](../../docs/endpoints.md) y [registro de contratos](../../docs/contracts/README.md).

Sin nuevas operaciones asignadas; revisar dependencias/consumo antes de cambiar contrato.

### Frontera y fuentes

[Acuerdo común](../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md).

### Asignación primaria vigente 0.4.0

Sin operaciones primarias nuevas: entregable de gobierno/operación, sin endpoint inventado.

## 4. Modelo de datos

Esta fase define fundamento/documentación, sin nuevo agregado de negocio. El modelo del producto se consulta en [datos por propietario](../../docs/modelo-datos.md); no presupone bases o proyectos creados.

### Diseño específico del incremento

Sin datos de negocio: registrar fuente/versión, requisito, decisión y evidencia; ninguna firma automática.

## 5. Seguridad

- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Ver [checklist seguridad](../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Documentos y límites coherentes con la constitución; lint documental sin referencias rotas.
- [ ] AC-002: Cuando se implemente esta fase, comandos y rutas reales constan en plan y evidencia; sin verdes prestados del profesor.

Suites y gate: [roles y gates](../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Reconstruir una tarea documental desde fuente hasta checklist y distinguir propuesta de ratificación.

- [ ] AC-100 → FR-100/NFR-100: Ninguna decisión propuesta ni excepción puede figurar aceptada sin nombre/fecha reales; un cambio de alcance enlaza la spec y su impacto en contratos y app.

- [ ] AC-402 → FR-402/NFR-002/NFR-003: Reconstruir requisito/spec/tarea/contrato/prueba/evidencia y recorrido interactivo sin respuestas inventadas. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-405 → FR-405/NFR-002/NFR-003: Aplicar constitución/spec/plan/tareas/checklist y controles de calidad desde cada incremento en ambos contextos. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-406 → FR-406/NFR-002/NFR-003: Usar Codex/Claude Code con contexto, cambios acotados, bitácora de intervención real y revisión humana; OpenCode solo referencia docente. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-411 → FR-411/NFR-002/NFR-003: Definir antes del desarrollo versiones de servicios/API/contratos/eventos/datos/app/landing/infra y manifiesto por despliegue. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-412 → FR-412/NFR-002/NFR-003: Si se adopta, registrar origen, versión, finalidad y permisos mínimos; ausencia de MCP no bloquea producto ni autoriza instalaciones. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-418 → FR-418/NFR-002/NFR-003: Cuenta GitHub única del proyecto y carpeta compartida; registrar accesos y responsables. Correo de proyecto recomendado, sin compartir secretos. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-451 → FR-451/NFR-002/NFR-003: Entrevistas reales con consentimiento, evidencia y síntesis; definir cambios de alcance sin inventar participantes.
- [ ] AC-452 → FR-452/NFR-002/NFR-003: Descripción detallada en español del backend y móvil con funciones, actores y límites por contexto.
- [ ] AC-453 → FR-453/NFR-002/NFR-003: Conciliar 13 componentes backend y 27 móviles del libro, con responsabilidad, dependencia y aceptación.
- [ ] AC-454 → FR-454/NFR-002/NFR-003: Conciliar las 101 fichas: entradas/salidas/reglas, documentos NoSQL, llaves/índices y filtros/búsquedas, sin operación inventada por botón.
- [ ] AC-455 → FR-455/NFR-002/NFR-003: Registrar URLs/commit reales de proyectos docentes y comparación justificada; mientras falten, mantener pendiente sin simular revisión nueva.
- [ ] AC-456 → FR-456/NFR-002/NFR-003: Constituciones, secuencia docente, roles Codex/Claude, memoria, contratos/consumo y revisión humana definidos por contexto.

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
| FR-001 | T-001, T-002, T-010, T-003 | AC-001, AC-002, AC-090, AC-100 |
| NFR-001 | T-010, T-004 | AC-001, AC-002, AC-090, AC-100 |
| NFR-002 | T-001, T-007, T-005, T-402, T-405, T-406, T-411, T-412, T-418, T-451, T-452, T-453, T-454, T-455, T-456 | AC-001, AC-002, AC-090, AC-100, AC-402, AC-405, AC-406, AC-411, AC-412, AC-418, AC-451, AC-452, AC-453, AC-454, AC-455, AC-456 |
| NFR-003 | T-002, T-010, T-021, T-003, T-005, T-402, T-405, T-406, T-411, T-412, T-418, T-451, T-452, T-453, T-454, T-455, T-456 | AC-001, AC-002, AC-090, AC-100, AC-402, AC-405, AC-406, AC-411, AC-412, AC-418, AC-451, AC-452, AC-453, AC-454, AC-455, AC-456 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-090, AC-100 |
| FR-402 | T-402 | AC-402 |
| FR-405 | T-405 | AC-405 |
| FR-406 | T-406 | AC-406 |
| FR-411 | T-411 | AC-411 |
| FR-412 | T-412 | AC-412 |
| FR-418 | T-418 | AC-418 |
| FR-451 | T-451 | AC-451 |
| FR-452 | T-452 | AC-452 |
| FR-453 | T-453 | AC-453 |
| FR-454 | T-454 | AC-454 |
| FR-455 | T-455 | AC-455 |
| FR-456 | T-456 | AC-456 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.
