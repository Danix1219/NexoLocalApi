# TAREAS — nv-12

Spec: [spec.md](spec.md). Plan: [plan.md](plan.md). Constitución: [rectora](../../../.specify/memory/constitution.md).

Estado: documentación propuesta; todas las casillas pendientes. Preparación documental y ejecución futura son distintas. Un encargo posterior de implementación y Ready de la historia son necesarios antes de construir producto. IDs locales; al citar usar contexto/spec/T-ID.

## Fase A — Refinamiento y decisiones

- [ ] T-006 →FR-090,NFR-090. Definir dispositivos, bandeja y delivery push; saga de eliminación con acuse por servicio y reintentos/reconciliación. Acepta: AC-090 tiene precondición, resultado, fuente, contraparte y evidencia esperada definidos; decisiones pertinentes D01–D08 registradas sin firmas ficticias. Depende de: fuentes y dependencias explícitas en spec; sin dependencia de construcción local.
- [ ] T-020 →FR-100,NFR-100. Desglosar el escenario propio: Precisar reintentos/dedupe de notificación y saga de borrado por todos los propietarios habilitados, incluidos objetos/caché/colas/proyecciones y restauración de backups. Acepta: AC-100 documenta caso positivo, negativo, fallo/reintento y responsable de cada superficie; exclusiones identificadas. Depende de: T-006.
- [ ] T-001 →FR-001,FR-002,NFR-002,FR-950,NFR-950. Preparar Ready de una historia acotada. Acepta: AC-950 con rutas y orden explicitados; requisitos y AC seleccionados, contratos pertinentes formalizados antes de consumo, decisiones bloqueantes resueltas, revisión humana identificada y plan/tareas conciliados; sin declarar Ready por lint. Depende de: T-950, T-006, T-020 y entradas de la spec; no requiere completar todo el dominio.

## Fase B — Entregable y ejecución posterior

- [ ] T-002 →FR-001,FR-002,NFR-003. Preparar el cambio del propietario con archivos reales al encargar implementación. Acepta: la historia seleccionada tiene archivos, versión, permisos y orden de cambio concretos; el estado documental no se presenta como aplicación creada. Depende de: T-001 y encargo correspondiente si hay ejecución de producto.
- [ ] T-010 →FR-001,NFR-001,NFR-003. Verificar la integración definida: Integrar notificaciones/dispositivos de nv-21 con logout y borrado; la bandeja/push tiene implementación primaria en nv-21. Acepta: AC-001, AC-002 verificados con evidencia del alcance aplicable, incluyendo rechazo/fallo y sin sustituir integración real por doble; en diseño queda procedimiento pendiente explícito. Depende de: T-002 y contrato/decisiones de esa historia.
- [ ] T-011 →FR-002,NFR-001,NFR-003. Atender el requisito propio: Cuenta solicita borrado y consulta progreso hasta confirmación de todos. Acepta: AC-002, AC-003 verificados con evidencia del alcance aplicable, incluyendo rechazo/fallo y sin sustituir integración real por doble; en diseño queda procedimiento pendiente explícito. Depende de: T-002 y contrato/decisiones de esa historia.
- [ ] T-021 →FR-100,NFR-100,NFR-003. Comprobar el escenario específico. Acepta: AC-100 — Cuenta bloqueada al inicio; falta de acuse deja trámite pendiente, reentrega no duplica efecto; reaplicar supresiones al restaurar evita resucitar datos; retención se ratifica, no se inventa. Registrar resultado real o pendiente sin inventar ejecución. Depende de: T-020 y T-002; evidencia de producto solo tras implementación autorizada.
- [ ] T-003 →FR-001,FR-002,NFR-003. Integrar frontera y datos reales de la historia. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 cuentan con comprobación de consumidor/provider, almacenamiento o dispositivo según superficie; fundamentos pueden producir evidencia documental identificada sin afirmar integración. Depende de: T-010, T-011, T-021, T-212, T-213.
- [ ] T-004 →NFR-001,NFR-100. Revisar privacidad, permisos, aislamiento, errores y fallos pertinentes. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 incluyen escenarios negativos; autoridad por recurso/estado se comprueba en API y presentación/limpieza en app; ninguna PII o credencial en evidencia. Depende de: T-003.
- [ ] T-007 →FR-090,NFR-090,NFR-002. Verificar el criterio transversal de la fase. Acepta: AC-090 — Push a sesión anterior no entrega datos de nueva cuenta; borrado parcial no informa completado; cuenta bloqueada de inmediato; restaurar backup aplica supresiones antes de servir. Distinguir revisión documental de ejecución futura. Depende de: T-004; preparación del escenario ya en T-006.

## Fase C — Cierre y revisión

- [ ] T-005 →NFR-002,NFR-003. Conciliar documentos, ejecutar gates pertinentes y actualizar Memoria. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 tienen resultado/evidencia o pendiente explícito; salida/exit, entorno, limitación y revisor reales registrados; sin aprobación automática. Depende de: T-004, T-007 y checklist del incremento.

## Verificación disponible

Hoy: powershell -NoProfile -File scripts/check_sdd.ps1 desde la raíz correspondiente, más revisión de integridad de IDs y dependencias. Solo documental. Build/test/proveedores/Android no ejecutados por esta fase.

## Historias de operación — refinamiento e implementación futura

- [ ] T-212 →FR-212,NFR-001,NFR-003. Definir y después construir/comprobar EP-012 (POST /me/deletion) en S01; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-212: EP-012: Bloquear cuenta y emitir evento; no afirmar completado al recibir Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.
- [ ] T-213 →FR-213,NFR-001,NFR-003. Definir y después construir/comprobar EP-013 (GET /me/deletion/{id}) en S01; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-213: EP-013: Receipt solo permite consultar este trámite, nunca reabrir cuenta Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.

## Preparacion de rutas antes de Ready (0.4.1)

- [ ] T-950 →FR-950,NFR-950. Concretar dueño S01/S07 y cada propietario activo, ubicacion objetivo adaptadores/outbox/inbox de cada servicio y tests/EndToEnd/, archivos reales actuales y alcance: borrado inicia en identidad; cierre distribuido integra dueños sin acceder a sus bases. Registrar decisiones, contratos, dependencias de entrada y suites por caso; enlazar el mapa sin fabricar scaffolding o comandos de proyectos inexistentes. Acepta: AC-950 queda refinado en plan y evidencia documental, con limites/pendientes y revisor identificable antes de Ready; no ejecuta producto ni resuelve proveedores por inferencia. Depende de: T-006, T-020.

Orden aplicable: refinamiento y preparacion de rutas preceden Ready; construccion solo tras encargo/Ready, integracion espera las tareas propias adicionales, seguridad y cierre conservan sus dependencias. No esperar cierre completo de otra fase cuando solo se requiere su entrada concreta.
