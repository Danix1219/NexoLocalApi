# TAREAS — nv-01

Spec: [spec.md](spec.md). Plan: [plan.md](plan.md). Constitución: [rectora](../../.specify/memory/constitution.md).

Estado: documentación propuesta; todas las casillas pendientes. Preparación documental y ejecución futura son distintas. Un encargo posterior de implementación y Ready de la historia son necesarios antes de construir producto. IDs locales; al citar usar contexto/spec/T-ID.

## Fase A — Refinamiento y decisiones

- [ ] T-006 →FR-090,NFR-090. Delimitar G01/S01-S07, propietarios de datos, entornos y configuración; preparar primera vertical sin construir todos los servicios. Acepta: AC-090 tiene precondición, resultado, fuente, contraparte y evidencia esperada definidos; decisiones pertinentes D01–D08 registradas sin firmas ficticias. Depende de: fuentes y dependencias explícitas en spec; sin dependencia de construcción local.
- [ ] T-020 →FR-100,NFR-100. Desglosar el escenario propio: Definir fundamento común de eventos/outbox/inbox, configuración por entorno y límites de confianza entre servicios, con identidad interna y propietario de cada escritura. Acepta: AC-100 documenta caso positivo, negativo, fallo/reintento y responsable de cada superficie; exclusiones identificadas. Depende de: T-006.
- [ ] T-001 →FR-001,NFR-002. Preparar Ready de una historia acotada. Acepta: requisitos y AC seleccionados, contratos pertinentes formalizados antes de consumo, decisiones bloqueantes resueltas, revisión humana identificada y plan/tareas conciliados; sin declarar Ready por lint. Depende de: T-006, T-020 y entradas de la spec; no requiere completar todo el dominio.

## Fase B — Entregable y ejecución posterior

- [ ] T-002 →FR-001,NFR-003. Desarrollar el diseño verificable de fundamento y, solo si se encarga, su configuración ejecutable. Acepta: la historia seleccionada tiene archivos, versión, permisos y orden de cambio concretos; el estado documental no se presenta como aplicación creada. Depende de: T-001 y encargo correspondiente si hay ejecución de producto.
- [ ] T-010 →FR-001,NFR-001,NFR-003. Atender el requisito propio: Definir fundamento común de eventos/outbox/inbox, configuración por entorno y límites de confianza entre servicios, con identidad interna y propietario de cada escritura. Acepta: AC-001, AC-002 verificados con evidencia del alcance aplicable, incluyendo rechazo/fallo y sin sustituir integración real por doble; en diseño queda procedimiento pendiente explícito. Depende de: T-002 y contrato/decisiones de esa historia.
- [ ] T-021 →FR-100,NFR-100,NFR-003. Comprobar el escenario específico. Acepta: AC-100 — Cada comando crítico tiene frontera transaccional local; evento versionado incluye eventId, sourceVersion y correlación mínima, sin PII; reentrega, fuera de orden, cola de error y reconciliación tienen comportamiento definido. Registrar resultado real o pendiente sin inventar ejecución. Depende de: T-020 y T-002; evidencia de producto solo tras implementación autorizada.
- [ ] T-003 →FR-001,NFR-003. Integrar frontera y datos reales de la historia. Acepta: AC-001, AC-002, AC-090, AC-100 cuentan con comprobación de consumidor/provider, almacenamiento o dispositivo según superficie; fundamentos pueden producir evidencia documental identificada sin afirmar integración. Depende de: T-010, T-021, T-289, T-290, T-601, T-610, T-403.
- [ ] T-004 →NFR-001,NFR-100. Revisar privacidad, permisos, aislamiento, errores y fallos pertinentes. Acepta: AC-001, AC-002, AC-090, AC-100 incluyen escenarios negativos; autoridad por recurso/estado se comprueba en API y presentación/limpieza en app; ninguna PII o credencial en evidencia. Depende de: T-003.
- [ ] T-007 →FR-090,NFR-090,NFR-002. Verificar el criterio transversal de la fase. Acepta: AC-090 — Documentar arranque, configuración faltante y fallo de dependencia del backend, identidades internas y propietarios sin afirmar proyecto creado; la UI GPS corresponde a app. Distinguir revisión documental de ejecución futura. Depende de: T-004; preparación del escenario ya en T-006.

## Fase C — Cierre y revisión

- [ ] T-005 →NFR-002,NFR-003. Conciliar documentos, ejecutar gates pertinentes y actualizar Memoria. Acepta: AC-001, AC-002, AC-090, AC-100 tienen resultado/evidencia o pendiente explícito; salida/exit, entorno, limitación y revisor reales registrados; sin aprobación automática. Depende de: T-004, T-007 y checklist del incremento.

## Verificación disponible

Hoy: powershell -NoProfile -File scripts/check_sdd.ps1 desde la raíz correspondiente, más revisión de integridad de IDs y dependencias. Solo documental. Build/test/proveedores/Android no ejecutados por esta fase.

## Historias de operación — refinamiento e implementación futura

- [ ] T-289 →FR-289,NFR-001,NFR-003. Definir y después construir/comprobar EP-089 (GET /health/live) en Transversal; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-289: EP-089: No consultar todas las dependencias en liveness Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.
- [ ] T-290 →FR-290,NFR-001,NFR-003. Definir y después construir/comprobar EP-090 (GET /health/ready) en Transversal; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-290: EP-090: Comprobar dependencias indispensables del servicio, no datos de usuario Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.

## Funcionalidades y obligaciones verificables

- [ ] T-601 →FR-601,NFR-002,NFR-003. Definir y atender G01 Entrada y composición. Acepta: AC-601: Mínimo dos réplicas en evaluación; autorización también en destino. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada. Delimitar G01 con REST y upgrade WebSocket, dependencias S01-S08 (S08 condicionado) y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.
- [ ] T-610 →FR-610,NFR-002,NFR-003. Definir y atender I01 Eventos e idempotencia. Acepta: AC-610: Doble entrega no duplica efecto y cola de errores operable. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada. Delimitar I01 con Eventos JSON versionados, dependencias Todos los propietarios y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.
- [ ] T-403 →FR-403,NFR-002,NFR-003. Definir y atender R03 Cinco componentes del proyecto. Acepta: AC-403: Inventariar nube, bases de datos, backend, móvil y landing con propietario, versión e integración. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter. Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.
