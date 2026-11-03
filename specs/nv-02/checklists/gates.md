# Checklist — nv-02

## DoD
- [ ] Spec y plan conciliados con archivos reales; tareas trazan FR/NFR.
- [ ] Contratos usados formalizados y pruebas de consumidor/provider ejecutadas cuando corresponda.
- [ ] Escenarios AC verificados con evidencia real; hallazgos negativos incluidos.
- [ ] Build/analyze, pruebas, seguridad y cobertura pertinentes pasan cuando exista código.
- [ ] [CHECKLIST_PR.md](../../../CHECKLIST_PR.md) y Memoria actualizados.
- [ ] Sin secretos ni resultados copiados del profesor.

## Aprobación
- [ ] Revisor: por asignar. Fecha: pendiente.
- [ ] Borrador documental: no hay producto ejecutado.
- [ ] Borrador con evidencia solo después de ejecución real; aprobación solo por revisión registrada.

## Control de alineación del incremento

- [ ] FR-090/NFR-090, T-006/T-007 y AC-090 conciliados, sin sustituir los criterios existentes.
- [ ] Un consumidor puede reconocer nulos/enums/errores sin adivinar; una prueba con doble no se registra como integración real.
- [ ] Contraparte y contrato de la historia identificados; evidencia distingue documental/doble/integración/dispositivo.
- [ ] Decisiones bloqueantes del acuerdo común resueltas antes de Ready; revisores reales pendientes hasta asignación.

## Integridad de cobertura 0.3.0

- [ ] Todos los FR/NFR de la spec aparecen en tareas y tabla de trazabilidad; cada AC tiene tarea real.
- [ ] T-006/T-007/T-020/T-021 existen; dependencias locales resueltas y sin ciclos.
- [ ] AC-100: Cada EP seleccionado identifica actor, tipos/nulos, errores, paginación, permisos por campo, reintento e idempotencia; un cambio incompatible tiene convivencia/migración antes del consumo.
- [ ] El revisor distingue cumplimiento documental de aceptación de producto; D01–D08 pertinentes y excepciones visibles.

## Funciones/obligaciones de fuente

- [ ] R04 Correspondencia con maqueta / AC-404: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.

## Integridad de alcance 0.4.0

- [ ] Operaciones, funciones y obligaciones de esta spec tienen FR/tarea/AC y contrato/consumo correspondiente.
- [ ] No se implementan de nuevo operaciones delegadas a otra spec ni se invocan internas/admin desde Flutter.
- [ ] Estado opcional/complemento/pendiente no se convierte en cumplimiento obligatorio o aprobado por existir en catálogo.

## Preparacion estructural y orden 0.4.1

- [ ] AC-950 / T-950: rutas actuales/objetivo, dueño, decisiones, dependencia de entrada, suites y evidencia definidos en plan; T-001 no pasa Ready sin esta preparacion. No marcar porque exista el mapa general.
- [ ] Construccion usa solo el propietario habilitado y no accede a proyectos/bases ajenos; todo comando de producto apunta a una ruta existente y su resultado real.
- [ ] Jira/Confluence pospuestos siguen pendientes sin enlaces inventados; CI documental y revision de diseño no equivalen a aceptacion funcional.
