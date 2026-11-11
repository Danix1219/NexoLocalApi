# Checklist — nv-04

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
- [ ] Logout offline limpia acceso local y muestra revocación remota pendiente; recuperar contraseña invalida sesiones según política ratificada; refresh concurrente no crea bucles.
- [ ] Contraparte y contrato de la historia identificados; evidencia distingue documental/doble/integración/dispositivo.
- [ ] Decisiones bloqueantes del acuerdo común resueltas antes de Ready; revisores reales pendientes hasta asignación.

## Integridad de cobertura 0.3.0

- [ ] Todos los FR/NFR de la spec aparecen en tareas y tabla de trazabilidad; cada AC tiene tarea real.
- [ ] T-006/T-007/T-020/T-021 existen; dependencias locales resueltas y sin ciclos.
- [ ] AC-100: Revocar en A impide acceso en B y conexiones abiertas; Redis caído consulta fuente durable o niega; recuperación no enumera y la política documenta qué otras sesiones invalida.
- [ ] El revisor distingue cumplimiento documental de aceptación de producto; D01–D08 pertinentes y excepciones visibles.

## Criterios por operación

- [ ] EP-001 / AC-201: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-002 / AC-202: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-003 / AC-203: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-004 / AC-204: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-005 / AC-205: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-007 / AC-207: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-008 / AC-208: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-073 / AC-273: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.
- [ ] EP-101 / AC-301: Previsto en línea base; contrato formal y aceptación pendientes. Evidencia positiva/negativa/fallo y revisión de contrato enlazadas; no marcar si solo está el catálogo.

## Funciones/obligaciones de fuente

- [ ] S01 Identidad / AC-602: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.
- [ ] R10 Logout / AC-410: fuente y criterio conciliados; evidencia apropiada o pendiente explícito, sin datos inventados.

## Integridad de alcance 0.4.0

- [ ] Operaciones, funciones y obligaciones de esta spec tienen FR/tarea/AC y contrato/consumo correspondiente.
- [ ] No se implementan de nuevo operaciones delegadas a otra spec ni se invocan internas/admin desde Flutter.
- [ ] Estado opcional/complemento/pendiente no se convierte en cumplimiento obligatorio o aprobado por existir en catálogo.

- [ ] H03/AC-703 con evidencia del caso auditado y corrección en superficie propia.

## Preparacion estructural y orden 0.4.1

- [ ] AC-950 / T-950: rutas actuales/objetivo, dueño, decisiones, dependencia de entrada, suites y evidencia definidos en plan; T-001 no pasa Ready sin esta preparacion. No marcar porque exista el mapa general.
- [ ] Construccion usa solo el propietario habilitado y no accede a proyectos/bases ajenos; todo comando de producto apunta a una ruta existente y su resultado real.
- [ ] CI documental y revision de diseño no equivalen a aceptacion funcional.

- [ ] AC-960 / T-960: perfil propio refinado para la historia, patrones justificados, fallos y suites definidos; T-001 espera esta salida y revisión humana. Sin aprobación por existir documentación.

- [ ] Arquitectura interna corresponde al perfil vigente y sus proyectos reales; reglas/permisos permanecen en el núcleo, adaptadores aislados y ventajas medidas antes de afirmar mejoras de rendimiento.


## Iteracion de conciliacion 2026-11-08

- [ ] AC-970: guardas de changeEmail, proposito publico y confianza por correo; revision humana pendiente.
- Las pruebas de esta iteracion usan Mongo/Redis efimeros; no acreditan entrega real por Gmail.
- Las referencias historicas a SMS y telefono unico quedan sustituidas para esta iteracion por el registro vigente.

Evidencia local ejecutada: SDK 10.0.302; build Release, cero errores y advertencias; format
verify-no-changes sin diferencias; suite con NEXO_DISABLE_DOTENV=1: 56 correctas (16 Common,
7 arquitectura, 15 seguridad, 18 integracion), cero fallidas/omitidas. Identity/Discovery/Engagement
Unit no contienen casos y no se cuentan como cobertura. Mongo/Redis de Testcontainers reales;
SMTP apuntado a localhost en pruebas, sin entrega real Gmail. Wrapper SDD en workspace aislado:
26 specs, traza e integridad correctas y 8 pruebas del validador correctas. No es aceptacion humana.


## Cuenta local — revisión pendiente

- [ ] AC-981: evidencia de CuentaTests, límites externos y revisión humana identificada.
- [ ] AC-982: evidencia de CuentaTests, límites externos y revisión humana identificada.
- [ ] AC-983: evidencia de CuentaTests, límites externos y revisión humana identificada.
- [ ] AC-984: evidencia de CuentaTests, límites externos y revisión humana identificada.
- [ ] AC-985: evidencia de CuentaTests, límites externos y revisión humana identificada.
- [ ] AC-986: evidencia de CuentaTests, límites externos y revisión humana identificada.
- [ ] AC-987: evidencia de CuentaTests, límites externos y revisión humana identificada.
- [ ] AC-988: evidencia de CuentaTests, límites externos y revisión humana identificada.

Evidencia ejecutada el 2026-11-09: build Release con SDK 10.0.302, cero errores/advertencias;
format verify-no-changes PASS; suite final con .env desactivado: 96 correctas (16 Common,
7 arquitectura, 55 seguridad, 18 integración), cero fallidas/omitidas. CuentaTests aporta 40
casos nuevos. Incluye negativos BOLA, moderación fallida/no configurada, null/ausente, límites,
CAS, relay repetido, invalidación de JWT/refresh y baja/cancelación/reanudación. Mongo/Redis
reales; Content Safety HTTP y avisos SMTP con dobles identificados. SDD en copia aislada e
integridad local verifican 26 specs, 484 tareas, 468 requisitos y 323 criterios; no aceptación cloud.
Las tres suites Unit sin casos no cuentan como pruebas ejecutadas. Firmas humanas pendientes.


## Incremento 2026-11-10 — correo y ubicación
- [x] AC-990 verificado mediante T-990 y las suites indicadas. Revisión humana general pendiente.
