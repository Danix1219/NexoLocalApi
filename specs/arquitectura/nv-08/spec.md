# NV-08 — Trabajos, experiencias, menciones y guardados

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-08`

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§2/4/5/6; H03/H04/H07.

Trabajos, experiencias, menciones y guardados

Trabajos, experiencias, menciones y guardados. Servicios involucrados: S03, S02, S06.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 04, 07, 09, 14, 16. Componentes: M14, M15, M16, M20. Hallazgos: H03, H04, H07.

Dependencias: nv-06, nv-05; elegibilidad de opinión vinculada depende de nv-10, no la comunidad declarada.

- FR-001: Prestador publica trabajo/fotos; integra recomendaciones y guardados de este spec.
- FR-002: Integrar experiencias/menciones/reputación desde este spec; no implementar un segundo caso de uso de opiniones.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Trabajo 10..200 con hasta cuatro fotos; experiencia 10..280 con estrellas 1..5.
- Una mención estructurada providerId y authorId real; no identidad por displayName.
- Opinión de pedido requiere done y dueño validado en S04.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Distinguir trabajo, experiencia declarada y opinión vinculada; autoría real, menciones y guardados idempotentes.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; H03/H04/H07: cambio de cuenta no mezcla autoría; quitar mención actualiza diálogo; duplicar reintento de guardado no aumenta contador; fotos y pedidos se cuentan separados.

- FR-100: Precisar autoría, experiencia declarada versus vinculada, elegibilidad interna y cambios/eliminación de menciones, guardados, reacciones y métricas.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/4/5/6; H03/H04/H07.

### Historias verificables por operación propia

- FR-231: EP-031 — Publicar un trabajo realizado; actor Prestador; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-232: EP-032 — Consultar trabajos filtrados y perfil; actor Público; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-233: EP-033 — Abrir detalle de trabajo; actor Público; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-234: EP-034 — Corregir contenido de trabajo; actor Autor; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-235: EP-035 — Retirar trabajo; actor Autor o moderador; autoridad S03. Previsto en línea base; contrato formal y aceptación pendientes.

### Funcionalidades y obligaciones con dueño explícito

- FR-604: S03 Comunidad: Trabajos, experiencias, reacciones, guardados y métricas. Datos propios: nl_community.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../../App_NexoLocal/specs/008-comunidad/spec.md).

### Asignación primaria vigente 0.4.0

- EP-031 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-032 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-033 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-034 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-035 → [ficha](../../../docs/contracts/fichas-operaciones.md)

| Operación integrada | Nuevo dueño de incremento |
|---|---|
| EP-036 | [este spec](../este spec/spec.md) |
| EP-037 | [este spec](../este spec/spec.md) |
| EP-038 | [este spec](../este spec/spec.md) |
| EP-039 | [este spec](../este spec/spec.md) |
| EP-040 | [este spec](../este spec/spec.md) |
| EP-041 | [este spec](../este spec/spec.md) |
| EP-042 | [este spec](../este spec/spec.md) |
| EP-043 | [este spec](../este spec/spec.md) |
| EP-044 | [este spec](../este spec/spec.md) |
| EP-045 | [este spec](../este spec/spec.md) |
| EP-046 | [este spec](../este spec/spec.md) |
| EP-093 | [este spec](../este spec/spec.md) |
| EP-094 | [este spec](../este spec/spec.md) |
| EP-095 | [este spec](../este spec/spec.md) |

Los FR/AC heredados de esta fase comprueban integración con esas capacidades; sus tareas EP y casos de uso se ejecutan únicamente en la spec primaria del inventario. El propietario S01–S08 no cambia.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S03: posts, reviews, reactions, saved y metrics. authorId del token; providerId estructurado; único por pedido cuando vinculado. Trabajo 10..200/cuatro fotos; experiencia 10..280/estrellas 1..5.

## 5. Seguridad

- Trabajo 10..200 con hasta cuatro fotos; experiencia 10..280 con estrellas 1..5.
- Una mención estructurada providerId y authorId real; no identidad por displayName.
- Opinión de pedido requiere done y dueño validado en S04.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Quitar mención actualiza el dato estructurado y rechaza publicación sin prestador requerido; formulario/diálogo se comprueban en la contraparte móvil.
- [ ] AC-002: Ana publica: Carlos no la ve como propia.
- [ ] AC-003: Repetir guardado/reacción no duplica ni aumenta contador indebidamente.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: H03/H04/H07: cambio de cuenta no mezcla autoría; quitar mención actualiza diálogo; duplicar reintento de guardado no aumenta contador; fotos y pedidos se cuentan separados.

- [ ] AC-100 → FR-100/NFR-100: Una opinión vinculada exige pedido done y dueño confirmado por S04; duplicados no alteran contadores; editar/quitar mención invalida proyecciones y autoría siempre usa ID del token.

- [ ] AC-231 → FR-231/NFR-001/NFR-003: EP-031: Categoría ofrecida, 0..4 medios propios aprobados; título automático hasta 60 caracteres. No exigir campos residuales problema/tiempo. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-232 → FR-232/NFR-001/NFR-003: EP-032: Solo publicados; orden por createdAt y _id Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-233 → FR-233/NFR-001/NFR-003: EP-033: No exponer domicilio donde se realizó Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-234 → FR-234/NFR-001/NFR-003: EP-034: Volver a moderar cambios; conservar versión anterior solo según política Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-235 → FR-235/NFR-001/NFR-003: EP-035: Ocultar feed e invalidar caché; purgar medios que queden sin referencias Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-604 → FR-604/NFR-002/NFR-003: Moderación previa y opinión vinculada validada. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.

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
| NFR-001 | T-010, T-011, T-004, T-231, T-232, T-233, T-234, T-235 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-231, AC-232, AC-233, AC-234, AC-235 |
| NFR-002 | T-001, T-007, T-005, T-604 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-604 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-231, T-232, T-233, T-234, T-235, T-604 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-231, AC-232, AC-233, AC-234, AC-235, AC-604 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-231 | T-231 | AC-231 |
| FR-232 | T-232 | AC-232 |
| FR-233 | T-233 | AC-233 |
| FR-234 | T-234 | AC-234 |
| FR-235 | T-235 | AC-235 |
| FR-604 | T-604 | AC-604 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S03 Community. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: publicaciones y coordinacion con este spec/este spec; opinion vinculada espera pedidos. Ubicacion objetivo: services/community/src/ y services/community/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil S03](../../../docs/arquitectura/servicios/s03-community.md) y [políticas comunes](../../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de S03 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |
