# Checklist — nv-01

## DoD
- [ ] Spec y plan conciliados con archivos reales; tareas trazan FR/NFR.
- [ ] Contratos usados formalizados y pruebas de consumidor/provider ejecutadas cuando corresponda.
- [ ] Escenarios AC verificados con evidencia real; hallazgos negativos incluidos.
- [ ] Build/analyze, pruebas, seguridad y cobertura pertinentes pasan cuando exista código.
- [ ] [CHECKLIST_PR.md](../../../../CHECKLIST_PR.md) y Memoria actualizados.
- [ ] Sin secretos ni resultados copiados del profesor.

## Aprobación
- [ ] Revisor: por asignar. Fecha: pendiente.
- [ ] Borrador documental: no hay producto ejecutado.
- [ ] Borrador con evidencia solo después de ejecución real; aprobación solo por revisión registrada.

## Control de alineación del incremento

- [ ] FR-090/NFR-090, T-006/T-007 y AC-090 conciliados, sin sustituir los criterios existentes.
- [ ] Documentar arranque, configuración faltante y fallo de dependencia sin afirmar proyecto creado; app conserva selección manual si GPS se deniega.
- [ ] Contraparte y contrato de la historia identificados; evidencia distingue documental/doble/integración/dispositivo.
- [ ] Decisiones bloqueantes del acuerdo común resueltas antes de Ready; revisores reales pendientes hasta asignación.

## Integridad de cobertura 0.3.0

- [ ] Todos los FR/NFR de la spec aparecen en tareas y tabla de trazabilidad; cada AC tiene tarea real.
- [ ] T-006/T-007/T-020/T-021 existen; dependencias locales resueltas y sin ciclos.
- [ ] AC-100: Cada comando crítico tiene frontera transaccional local; evento versionado incluye eventId, sourceVersion y correlación mínima, sin PII; reentrega, fuera de orden, cola de error y reconciliación tienen comportamiento definido.
- [ ] El revisor distingue cumplimiento documental de aceptación de producto; D01–D08 pertinentes y excepciones visibles.

## Criterios por operación

- [ ] EP-089 / AC-289: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-090 / AC-290: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.

## Funciones/obligaciones de fuente

- [ ] G01 Entrada y composición / AC-601: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.
- [ ] I01 Eventos e idempotencia / AC-610: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.
- [ ] R03 Cinco componentes del proyecto / AC-403: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.

## Integridad de alcance 0.4.0

- [ ] Operaciones, funciones y obligaciones de esta spec tienen FR/tarea/AC y contrato/consumo correspondiente.
- [ ] No se implementan de nuevo operaciones delegadas a otra spec ni se invocan internas/admin desde Flutter.
- [ ] Estado opcional/complemento/pendiente no se convierte en cumplimiento obligatorio o aprobado por existir en catálogo.

## Preparacion estructural y orden 0.4.1

- [ ] AC-950 / T-950: rutas actuales/objetivo, dueño, decisiones, dependencia de entrada, suites y evidencia definidos en plan; T-001 no pasa Ready sin esta preparacion. No marcar porque exista el mapa general.
- [ ] Construccion usa solo el propietario habilitado y no accede a proyectos/bases ajenos; todo comando de producto apunta a una ruta existente y su resultado real.
- [ ] Jira/Confluence pospuestos siguen pendientes sin enlaces inventados; CI documental y revision de diseño no equivalen a aceptacion funcional.
- [ ] AC-951 / T-951: Archivo de origen/destino trazado, sin .vs/bin/obj/*.user/secretos; solucion y contenedor apuntan al host real; build/arranque se documentan despues de configurar CI real de nv-03. No exige completar toda nv-03 antes de crear el scaffold que esa fase valida. Resultado/entorno/revisor y limitaciones registrados; estado pendiente hasta comprobacion real.
- [ ] AC-952 / T-952: Dependencias permitidas pasan y una referencia cruzada prohibida se rechaza en comprobacion reproducible; cada proyecto tiene dueño y rollback del traslado comprobable, sin generar ocho servicios vacios. Resultado/entorno/revisor y limitaciones registrados; estado pendiente hasta comprobacion real.

- [ ] AC-960 / T-960: perfil propio refinado para la historia, patrones justificados, fallos y suites definidos; T-001 espera esta salida y revisión humana. Sin aprobación por existir documentación.

- [ ] AC-961 / T-961: base física de todos los propietarios conciliada con build, límites y arranque reales; S08 condicional, sin negocio y sin confundir scaffold con aceptación funcional. Revisión humana pendiente identificada.
