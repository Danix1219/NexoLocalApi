# Checklist — nv-10

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
- [ ] H01/H02: pendiente/rechazado no expone teléfono/domicilio; cancelar retira también caché/rutas; dos aceptaciones simultáneas dejan solo una asignación activa.
- [ ] Contraparte y contrato de la historia identificados; evidencia distingue documental/doble/integración/dispositivo.
- [ ] Decisiones bloqueantes del acuerdo común resueltas antes de Ready; revisores reales pendientes hasta asignación.

## Integridad de cobertura 0.3.0

- [ ] Todos los FR/NFR de la spec aparecen en tareas y tabla de trazabilidad; cada AC tiene tarea real.
- [ ] T-006/T-007/T-020/T-021 existen; dependencias locales resueltas y sin ciclos.
- [ ] AC-100: Cancel/rechazo no devuelven teléfono, dirección precisa o ruta; ok/done exige participante y consentimiento vigente; carrera de aceptación deja una asignación y evento coherente, nunca dos.
- [ ] El revisor distingue cumplimiento documental de aceptación de producto; D01–D08 pertinentes y excepciones visibles.

## Criterios por operación

- [ ] EP-058 / AC-258: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-059 / AC-259: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-060 / AC-260: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-061 / AC-261: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-062 / AC-262: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-063 / AC-263: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-064 / AC-264: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-065 / AC-265: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-088 / AC-288: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.

## Funciones/obligaciones de fuente

- [ ] S04 Solicitudes y pedidos / AC-605: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.

## Integridad de alcance 0.4.0

- [ ] Operaciones, funciones y obligaciones de esta spec tienen FR/tarea/AC y contrato/consumo correspondiente.
- [ ] No se implementan de nuevo operaciones delegadas a otra spec ni se invocan internas/admin desde Flutter.
- [ ] Estado opcional/complemento/pendiente no se convierte en cumplimiento obligatorio o aprobado por existir en catálogo.

- [ ] H02/AC-702 con evidencia del caso auditado y corrección en superficie propia.

## Preparacion estructural y orden 0.4.1

- [ ] AC-950 / T-950: rutas actuales/objetivo, dueño, decisiones, dependencia de entrada, suites y evidencia definidos en plan; T-001 no pasa Ready sin esta preparacion. No marcar porque exista el mapa general.
- [ ] Construccion usa solo el propietario habilitado y no accede a proyectos/bases ajenos; todo comando de producto apunta a una ruta existente y su resultado real.
- [ ] CI documental y revision de diseño no equivalen a aceptacion funcional.

- [ ] AC-960 / T-960: perfil propio refinado para la historia, patrones justificados, fallos y suites definidos; T-001 espera esta salida y revisión humana. Sin aprobación por existir documentación.

- [ ] Arquitectura interna corresponde al perfil vigente y sus proyectos reales; reglas/permisos permanecen en el núcleo, adaptadores aislados y ventajas medidas antes de afirmar mejoras de rendimiento.
