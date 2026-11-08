# Checklist — nv-11

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
- [ ] H05/H11: texto rápido/manual utiliza misma intención; reintento mismo ID da una entrega; nuevo pedido cambia cabecera sin reescribir mensajes; logout invalida conexión existente.
- [ ] Contraparte y contrato de la historia identificados; evidencia distingue documental/doble/integración/dispositivo.
- [ ] Decisiones bloqueantes del acuerdo común resueltas antes de Ready; revisores reales pendientes hasta asignación.

## Integridad de cobertura 0.3.0

- [ ] Todos los FR/NFR de la spec aparecen en tareas y tabla de trazabilidad; cada AC tiene tarea real.
- [ ] T-006/T-007/T-020/T-021 existen; dependencias locales resueltas y sin ciclos.
- [ ] AC-100: Logout/expiración/revocación corta uso de conexión; no token persistible en URL/log; mismo clientMessageId confirma una vez; aviso perdido se recupera de historial autorizado por cursor.
- [ ] El revisor distingue cumplimiento documental de aceptación de producto; D01–D08 pertinentes y excepciones visibles.

## Criterios por operación

- [ ] EP-066 / AC-266: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-067 / AC-267: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-068 / AC-268: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-069 / AC-269: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-070 / AC-270: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-071 / AC-271: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-072 / AC-272: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-074 / AC-274: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.

## Funciones/obligaciones de fuente

- [ ] S05 Mensajería / AC-606: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.

## Integridad de alcance 0.4.0

- [ ] Operaciones, funciones y obligaciones de esta spec tienen FR/tarea/AC y contrato/consumo correspondiente.
- [ ] No se implementan de nuevo operaciones delegadas a otra spec ni se invocan internas/admin desde Flutter.
- [ ] Estado opcional/complemento/pendiente no se convierte en cumplimiento obligatorio o aprobado por existir en catálogo.

- [ ] H05/AC-705 con evidencia del caso auditado y corrección en superficie propia.

- [ ] H11/AC-711 con evidencia del caso auditado y corrección en superficie propia.

## Preparacion estructural y orden 0.4.1

- [ ] AC-950 / T-950: rutas actuales/objetivo, dueño, decisiones, dependencia de entrada, suites y evidencia definidos en plan; T-001 no pasa Ready sin esta preparacion. No marcar porque exista el mapa general.
- [ ] Construccion usa solo el propietario habilitado y no accede a proyectos/bases ajenos; todo comando de producto apunta a una ruta existente y su resultado real.
- [ ] CI documental y revision de diseño no equivalen a aceptacion funcional.

- [ ] AC-960 / T-960: perfil propio refinado para la historia, patrones justificados, fallos y suites definidos; T-001 espera esta salida y revisión humana. Sin aprobación por existir documentación.

- [ ] Arquitectura interna corresponde al perfil vigente y sus proyectos reales; reglas/permisos permanecen en el núcleo, adaptadores aislados y ventajas medidas antes de afirmar mejoras de rendimiento.
