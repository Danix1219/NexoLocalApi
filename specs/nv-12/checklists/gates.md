# Checklist — nv-12

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
- [ ] Push a sesión anterior no entrega datos de nueva cuenta; borrado parcial no informa completado; cuenta bloqueada de inmediato; restaurar backup aplica supresiones antes de servir.
- [ ] Contraparte y contrato de la historia identificados; evidencia distingue documental/doble/integración/dispositivo.
- [ ] Decisiones bloqueantes del acuerdo común resueltas antes de Ready; revisores reales pendientes hasta asignación.

## Integridad de cobertura 0.3.0

- [ ] Todos los FR/NFR de la spec aparecen en tareas y tabla de trazabilidad; cada AC tiene tarea real.
- [ ] T-006/T-007/T-020/T-021 existen; dependencias locales resueltas y sin ciclos.
- [ ] AC-100: Cuenta bloqueada al inicio; falta de acuse deja trámite pendiente, reentrega no duplica efecto; reaplicar supresiones al restaurar evita resucitar datos; retención se ratifica, no se inventa.
- [ ] El revisor distingue cumplimiento documental de aceptación de producto; D01–D08 pertinentes y excepciones visibles.

## Criterios por operación

- [ ] EP-012 / AC-212: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-013 / AC-213: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.

## Integridad de alcance 0.4.0

- [ ] Operaciones, funciones y obligaciones de esta spec tienen FR/tarea/AC y contrato/consumo correspondiente.
- [ ] No se implementan de nuevo operaciones delegadas a otra spec ni se invocan internas/admin desde Flutter.
- [ ] Estado opcional/complemento/pendiente no se convierte en cumplimiento obligatorio o aprobado por existir en catálogo.
