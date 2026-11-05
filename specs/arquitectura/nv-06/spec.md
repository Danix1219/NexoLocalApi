# NV-06 — Perfiles, catálogo, cobertura y edición

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-06`

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md). Aplicación: Constitución §§2/5/6; D01/D05.

Perfiles, catálogo, cobertura y edición

Perfiles, catálogo, cobertura y edición. Servicios involucrados: S02, S06.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 04, 09, 14, 15, 16. Componentes: M04, M10, M11, M21. Hallazgos: H06, H07, H09, H10.

Dependencias: nv-04, nv-05.

- FR-001: Cuenta crea perfil persona/negocio y completa opcionales; consume catálogo de nv-17.
- FR-002: Prestador edita permanentemente horario/precios/condiciones; integra políticas de contacto de nv-25.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- Una política versionada calcula completitud en alta y perfil.
- Separar fotos/publicaciones/pedidos completados; radio cero es válido para solo local.
- Origen domiciliario privado; dirección comercial pública solo por intención explícita.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir persona/negocio, catálogo, cobertura y preferencias; política de completitud y contadores únicos; edición permanente.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; H06/H07/H09/H10: mismo porcentaje/policyVersion en alta y perfil, fotos no cuentan como trabajos, radio cero solo local y edición siempre accesible.

- FR-100: Definir consulta y edición de catálogo/perfil/modalidad con completitud versionada, contadores separados, permisos por campo e invalidación.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§2/5/6; D01/D05.

### Historias verificables por operación propia

- FR-216: EP-016 — Crear perfil de persona o negocio; actor Cuenta sin perfil; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-217: EP-017 — Consultar perfil público y señales; actor Público; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-218: EP-018 — Consultar perfil editable y completitud; actor Prestador propietario; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-219: EP-019 — Actualizar datos comerciales y servicios; actor Prestador propietario; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-220: EP-020 — Editar modalidad y zona de atención; actor Prestador propietario; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-221: EP-021 — Configurar horarios y urgencias; actor Prestador propietario; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-222: EP-022 — Definir visita y precios orientativos; actor Prestador propietario; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-223: EP-023 — Confirmar vigencia de información; actor Prestador propietario; autoridad S02. Previsto en línea base; contrato formal y aceptación pendientes.
- FR-224: EP-024 — Retirar perfil de publicación; actor Prestador reautenticado; autoridad S02. Complemento de diseño; condición/UX y alcance se ratifican antes de Ready, sin pantalla inventada.

### Funcionalidades y obligaciones con dueño explícito

- FR-603: S02 Directorio y descubrimiento: Perfiles, catálogo, condiciones, búsqueda, cobertura y geocodificación. Datos propios: nl_directory + Redis caché.

- FR-706: Corregir H06: Completitud única; integrar propietarios implicados sin duplicar regla.

- FR-707: Corregir H07: Contadores honestos; integrar propietarios implicados sin duplicar regla.

- FR-709: Corregir H09: Edición permanente; integrar propietarios implicados sin duplicar regla.

- FR-710: Corregir H10: Modalidad solo local; integrar propietarios implicados sin duplicar regla.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).


### Frontera y fuentes

[Acuerdo común](../../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../../App_NexoLocal/specs/006-perfiles/spec.md).

### Asignación primaria vigente 0.4.0

- EP-016 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-017 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-018 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-019 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-020 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-021 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-022 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-023 → [ficha](../../../docs/contracts/fichas-operaciones.md)
- EP-024 → [ficha](../../../docs/contracts/fichas-operaciones.md)

| Operación integrada | Nuevo dueño de incremento |
|---|---|
| EP-014 | [nv-17](../nv-17/spec.md) |
| EP-015 | [nv-17](../nv-17/spec.md) |
| EP-091 | [nv-25](../nv-25/spec.md) |
| EP-096 | [nv-25](../nv-25/spec.md) |

Los FR/AC heredados de esta fase comprueban integración con esas capacidades; sus tareas EP y casos de uso se ejecutan únicamente en la spec primaria del inventario. El propietario S01–S08 no cambia.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

S02: categories, providers y read_cache; perfil por cuenta único, GeoJSON [longitud,latitud], origen privado y dirección comercial explícita. Límites: tres precios y seis fotos de portafolio.

## 5. Seguridad

- Una política versionada calcula completitud en alta y perfil.
- Separar fotos/publicaciones/pedidos completados; radio cero es válido para solo local.
- Origen domiciliario privado; dirección comercial pública solo por intención explícita.

Ver [checklist seguridad](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Alta básica funciona sin fotos/precios; siete fotos conserva máximo seis.
- [ ] AC-002: Horario cierre<=apertura y visita pagada sin monto se rechazan.
- [ ] AC-003: Después de completar horario/precio sigue existiendo acceso a editar.
- [ ] AC-004: Respuesta pública no contiene privateOrigin ni teléfono no permitido.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: H06/H07/H09/H10: mismo porcentaje/policyVersion en alta y perfil, fotos no cuentan como trabajos, radio cero solo local y edición siempre accesible.

- [ ] AC-100 → FR-100/NFR-100: La lectura pública excluye privateOrigin y contacto sin consentimiento; editar horario/precio conserva acceso permanente; seis fotos de portafolio y tres precios no se convierten en trabajos o ventas.

- [ ] AC-216 → FR-216/NFR-001/NFR-003: EP-016: Un perfil por cuenta; mínimos de registro; texto/medios aprobados; portafolio de registro hasta seis fotos aprobadas; no contar fotos como trabajos ni reutilizar providerId=me entre cuentas. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-217 → FR-217/NFR-001/NFR-003: EP-017: Ocultar origen privado y datos de cuenta; teléfono según permisos Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-218 → FR-218/NFR-001/NFR-003: EP-018: Solo propio; completitud se calcula desde campos persistidos Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-219 → FR-219/NFR-001/NFR-003: EP-019: Máximo tres precios orientativos en UI inicial; catálogo/categorías aparte; contenido moderado; editor permanente permite corregir campos completos, no solo faltantes. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-220 → FR-220/NFR-001/NFR-003: EP-020: Validar rango de coordenadas y radio positivo en dom/ambos, cero permitido en local; publicación comercial explícita Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-221 → FR-221/NFR-001/NFR-003: EP-021: Cierre posterior a apertura para alcance inicial; validar zona IANA Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-222 → FR-222/NFR-001/NFR-003: EP-022: Cero representa visita gratis; ausencia no equivale a cero; tres filas visibles; no comparar montos de unidades distintas. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-223 → FR-223/NFR-001/NFR-003: EP-023: No aumentar reputación por pulsar actualizar Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.
- [ ] AC-224 → FR-224/NFR-001/NFR-003: EP-024: Ampliación operativa; definir tratamiento de pedidos abiertos y vínculos Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO.

- [ ] AC-603 → FR-603/NFR-002/NFR-003: Consulta por radio/modalidad sin revelar origen privado. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.

- [ ] AC-706 → FR-706/NFR-001/NFR-003: Alta/perfil/cuenta reciben la misma política versionada sobre los mismos datos; fórmula por ratificar sin porcentajes inventados.

- [ ] AC-707 → FR-707/NFR-001/NFR-003: Fotos de portafolio, publicaciones, pedidos resueltos y recomendaciones son contadores distintos; seis fotos no equivalen a seis servicios.

- [ ] AC-709 → FR-709/NFR-001/NFR-003: Tras completar horario/precios/condiciones/contacto se conserva acceso a editar y respuesta actualiza vistas/caché.

- [ ] AC-710 → FR-710/NFR-001/NFR-003: Radio cero en local no vuelve invisible negocio ni muestra menos de 0 km a domicilio; selector y búsqueda respetan modalidad.

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
| NFR-001 | T-010, T-011, T-004, T-216, T-217, T-218, T-219, T-220, T-221, T-222, T-223, T-224, T-706, T-707, T-709, T-710 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-216, AC-217, AC-218, AC-219, AC-220, AC-221, AC-222, AC-223, AC-224, AC-706, AC-707, AC-709, AC-710 |
| NFR-002 | T-001, T-007, T-005, T-603 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-603 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-216, T-217, T-218, T-219, T-220, T-221, T-222, T-223, T-224, T-603, T-706, T-707, T-709, T-710 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100, AC-216, AC-217, AC-218, AC-219, AC-220, AC-221, AC-222, AC-223, AC-224, AC-603, AC-706, AC-707, AC-709, AC-710 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-004, AC-090, AC-100 |
| FR-216 | T-216 | AC-216 |
| FR-217 | T-217 | AC-217 |
| FR-218 | T-218 | AC-218 |
| FR-219 | T-219 | AC-219 |
| FR-220 | T-220 | AC-220 |
| FR-221 | T-221 | AC-221 |
| FR-222 | T-222 | AC-222 |
| FR-223 | T-223 | AC-223 |
| FR-224 | T-224 | AC-224 |
| FR-603 | T-603 | AC-603 |
| FR-706 | T-706 | AC-706 |
| FR-707 | T-707 | AC-707 |
| FR-709 | T-709 | AC-709 |
| FR-710 | T-710 | AC-710 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: S02 Directory. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: perfiles/cobertura/edicion y consumo del catalogo de nv-17 sin reimplementarlo. Ubicacion objetivo: services/directory/src/ y services/directory/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |

## Perfil de arquitectura del propietario (2026-11-03)

[Perfil S02](../../../docs/arquitectura/servicios/s02-directory.md) y [políticas comunes](../../../docs/arquitectura/politicas-comunes.md). Diseño propuesto; no selecciona proveedores ni acredita implementación.

- FR-960: Refinar la historia de S02 con su estructura, puertos, invariantes, frontera de datos e integración del perfil; justificar los patrones aplicables y excluir los innecesarios.
- NFR-960: Definir concurrencia, idempotencia, permisos y recuperación pertinentes sin dependencias de proyectos/datos ajenos ni datos privados en eventos; mantener carácter condicional de S08.
- [ ] AC-960 → FR-960/NFR-960: Antes de Ready, plan registra patrones elegidos y motivo, rutas reales/objetivo, contrato, atomicidad y escenarios positivo/negativo/fallo con suites y evidencia esperada. Decisiones bloqueantes resueltas y revisión humana identificada; diseño no equivale a producto.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-960 | T-960 | AC-960 |
| NFR-960 | T-960 | AC-960 |
