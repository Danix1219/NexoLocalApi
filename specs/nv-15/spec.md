# NV-15 — Proyección de relaciones y lectura del grafo

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-15`

## 1. Objetivo

Constitución rectora: [fuente única](../../.specify/memory/constitution.md). Aplicación: Constitución §§2/4/5/6/11; D01/D03.

Proyección de relaciones y lectura del grafo

Proyección de relaciones y lectura del grafo. Servicios involucrados: S08, S02, S03, S01.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 07, 14, 15. Componentes: Extensión opcional. Hallazgos: H03.

Dependencias: nv-08, nv-12; habilitación condicionada a consultas/costo y aprobación.

- FR-001: Usuario obtiene candidatos relacionados con razón explicable.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Extensión condicionada a consultas/costo; CRUD actual continúa sin ella.
- Snapshots y eventId/sourceVersion evitan duplicados/replays; no PII ni permisos en grafo.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir S08 como proyección pública opcional de dos saltos, reconstrucción e invalidación; GraphQL lectura acotada si se adopta.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Grafo caído no bloquea funciones obligatorias; replay antiguo no revive autor borrado; cambiar destinatario elimina relación anterior; consulta excedida se rechaza.

- FR-100: Precisar habilitación opcional, límites de consulta y reconstrucción de S08 por snapshots/versiones/tombstones, sin almacenar contacto o domicilio.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/4/5/6/11; D01/D03.

### Historias verificables por operación propia

- FR-298: EP-098 — Consultar contexto y candidatos relacionados del grafo; actor Cuenta autenticada; consultas acotadas; autoridad S08. Opcional; no bloquea alcance obligatorio si no se habilita.

### Funcionalidades y obligaciones con dueño explícito

- FR-609: S08 Relaciones y descubrimiento: Proyección de relaciones públicas y consultas acotadas de dos saltos. Datos propios: nl_social_graph.

## 3. Contratos API

[Endpoints vivos](../../docs/endpoints.md) y [registro de contratos](../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../App_NexoLocal/specs/015-relaciones-opcional/spec.md).

### Asignación primaria vigente 0.4.0

- EP-098 → [ficha](../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

Neo4j derivado, nunca autoridad de permisos/PII. eventId/sourceVersion/snapshot y tombstones; retiro de aristas al editar/borrar. Operaciones GraphQL registradas, profundidad/costo/paginación acotados.

## 5. Seguridad

- Extensión condicionada a consultas/costo; CRUD actual continúa sin ella.
- Snapshots y eventId/sourceVersion evitan duplicados/replays; no PII ni permisos en grafo.

Ver [checklist seguridad](../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Grafo caído conserva directorio y pedidos.
- [ ] AC-002: Editar destinatario retira relación anterior; borrar autor elimina proyección.
- [ ] AC-003: Operación GraphQL excedida/no registrada se rechaza; paginación ligada a snapshot.

Suites y gate: [roles y gates](../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Grafo caído no bloquea funciones obligatorias; replay antiguo no revive autor borrado; cambiar destinatario elimina relación anterior; consulta excedida se rechaza.

- [ ] AC-100 → FR-100/NFR-100: Eliminar autor y reejecutar evento antiguo no revive relación; indisponibilidad no bloquea directorio/pedidos; GraphQL rechaza operación no registrada o excedida sin dar permisos privados.

- [ ] AC-298 → FR-298/NFR-001/NFR-003: EP-098: Extensión opcional de lectura, sin mutations ni Cypher arbitrario. Authorization por recurso/campo, dos saltos y profundidad propuesta 4; máximo 100 objetos, una operación y timeout 2 s. Coste y rate limit por usuario. No caché compartida de datos personalizados. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-609 → FR-609/NFR-002/NFR-003: Reconstrucción, borrado y orden de eventos; sin permisos privados basados en grafo. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.

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
| NFR-001 | T-010, T-004, T-298 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-298 |
| NFR-002 | T-001, T-007, T-005, T-609 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-609 |
| NFR-003 | T-002, T-010, T-021, T-003, T-005, T-298, T-609 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-298, AC-609 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-298 | T-298 | AC-298 |
| FR-609 | T-609 | AC-609 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S08 Relations condicionado. [Mapa backend](../../docs/estructura-backend.md) y [orden SDD](../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: proyeccion publica sin PII ni autoridad de permisos; reconstruccion tras eventos/tombstones. Ubicacion objetivo: services/relations/src/ y services/relations/tests/ solo al habilitarse.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil S08](../../docs/arquitectura/servicios/s08-relations.md) y [políticas comunes](../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de S08 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |
