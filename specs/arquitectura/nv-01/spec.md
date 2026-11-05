# NV-01 — Fundación de servicios y entorno

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-01`

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§3/4/6/9; D01/D03.

Fundación de servicios y entorno

Fase de fundamento previa a funcionalidad. Conserva el orden de aprendizaje del profesor sin copiar infraestructura incompatible.

## 2. Alcance / No-alcance

Requisitos Móvil v2: SDD, calidad, contratos y entrega; no reemplaza el alcance de funcionalidades.

Dependencias: nv-00.

- FR-001: Definir fundamento común de eventos/outbox/inbox, configuración por entorno y límites de confianza entre servicios, con identidad interna y propietario de cada escritura.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Delimitar G01/S01-S07, propietarios de datos, entornos y configuración; preparar primera vertical sin construir todos los servicios.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Documentar arranque, configuración faltante y fallo de dependencia del backend, identidades internas y propietarios sin afirmar proyecto creado; la UI GPS corresponde a app.

- FR-100: Definir fundamento común de eventos/outbox/inbox, configuración por entorno y límites de confianza entre servicios, con identidad interna y propietario de cada escritura.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§3/4/6/9; D01/D03.

### Historias verificables por operación propia

- FR-289: EP-089 — Comprobar proceso vivo; actor Infraestructura interna; autoridad Transversal. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-290: EP-090 — Comprobar capacidad de servir tráfico; actor Infraestructura interna; autoridad Transversal. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-601: G01 Entrada y composición: Rutas HTTPS, agregación de feed/comparación y observabilidad de entrada. Datos propios: Sin base de negocio.
- FR-610: I01 Eventos e idempotencia: Distribución a cada consumidor y recuperación de fallas. Datos propios: Colecciones de cada dueño + colas.
- FR-403: R03 Cinco componentes del proyecto: Atender el requisito académico 3 en su superficie y coordinar la contraparte.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md).

### Asignación primaria vigente 0.4.0

- EP-089 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-090 → [ficha](../../../docs/contracts/fichas-operaciones.md)

## 4. Modelo de datos

Esta fase define fundamento/documentación, sin nuevo agregado de negocio. El modelo del producto se consulta en [datos por propietario](../../../docs/modelo-datos.md); no presupone bases o proyectos creados.

### Diseño específico del incremento

API: inventario de bases por servicio y configuración sin secretos. App: zona pública, preferencias, sesión y caché privada separadas; token fuera de SQLite.

## 5. Seguridad

- Sin producto ni proveedor simulado presentado como real.
- Contratos/plantillas y configuración no deben contradecir la constitución.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Documentos y límites coherentes con la constitución; lint documental sin referencias rotas.
- [ ] AC-002: Cuando se implemente esta fase, comandos y rutas reales constan en plan y evidencia; sin verdes prestados del profesor.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Documentar arranque, configuración faltante y fallo de dependencia del backend, identidades internas y propietarios sin afirmar proyecto creado; la UI GPS corresponde a app.

- [ ] AC-100 → FR-100/NFR-100: Cada comando crítico tiene frontera transaccional local; evento versionado incluye eventId, sourceVersion y correlación mínima, sin PII; reentrega, fuera de orden, cola de error y reconciliación tienen comportamiento definido.

- [ ] AC-289 → FR-289/NFR-001/NFR-003: EP-089: No consultar todas las dependencias en liveness Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-290 → FR-290/NFR-001/NFR-003: EP-090: Comprobar dependencias indispensables del servicio, no datos de usuario Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-601 → FR-601/NFR-002/NFR-003: Mínimo dos réplicas en evaluación; autorización también en destino. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.
- [ ] AC-610 → FR-610/NFR-002/NFR-003: Doble entrega no duplica efecto y cola de errores operable. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.
- [ ] AC-403 → FR-403/NFR-002/NFR-003: Inventariar nube, bases de datos, backend, móvil y landing con propietario, versión e integración. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

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
| NFR-001 | T-010, T-004, T-289, T-290 | AC-001, AC-002, AC-090, AC-100, AC-289, AC-290 |
| NFR-002 | T-001, T-007, T-005, T-601, T-610, T-403 | AC-001, AC-002, AC-090, AC-100, AC-403, AC-601, AC-610 |
| NFR-003 | T-002, T-010, T-021, T-003, T-005, T-289, T-290, T-601, T-610, T-403 | AC-001, AC-002, AC-090, AC-100, AC-289, AC-290, AC-403, AC-601, AC-610 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-090, AC-100 |
| FR-289 | T-289 | AC-289 |
| FR-290 | T-290 | AC-290 |
| FR-601 | T-601 | AC-601 |
| FR-610 | T-610 | AC-610 |
| FR-403 | T-403 | AC-403 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: G01/S01 y fundamento comun. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: consolidacion de plantilla, capas, referencias, lanzamiento y Docker de primera vertical. Ubicacion objetivo: nexolocal.Api/, services/identity/src/, services/identity/tests/ y solucion raiz.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

- FR-951: Consolidar la primera plantilla en el checkout backend.
- [ ] AC-951 → FR-951/NFR-950: Archivo de origen/destino trazado, sin .vs/bin/obj/*.user/secretos; solucion y contenedor apuntan al host real; build/arranque se documentan despues de configurar CI real de nv-03. No exige completar toda nv-03 antes de crear el scaffold que esa fase valida.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-951 | T-951, T-003 | AC-951 |

- FR-952: Comprobar limites entre proyectos y servicios.
- [ ] AC-952 → FR-952/NFR-950: Dependencias permitidas pasan y una referencia cruzada prohibida se rechaza en comprobacion reproducible; cada proyecto tiene dueño y rollback del traslado comprobable, sin generar ocho servicios vacios.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-952 | T-952, T-003 | AC-952 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil G01](../../../docs/arquitectura/servicios/g01-gateway.md) y [políticas comunes](../../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de G01 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |

## Extensión autorizada del scaffold

- FR-961: Aplicar el encargo explícito de base física de G01 y S01–S08 en la solución original, con hosts/capas por propietario y Worker S07 separado; conservar S08 condicional y sin lógica de negocio.
- [ ] AC-961 → FR-961/NFR-960: Proyectos compilan; solución y referencias respetan propietarios; prueba negativa rechaza referencias ajenas; hosts arrancan independientemente. Reportar resultados reales y límites, sin afirmar patrones o integración funcional por disponer de carpetas. Revisión humana y consolidación Git/CI pendientes se registran.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-961 | T-961 | AC-961 |
