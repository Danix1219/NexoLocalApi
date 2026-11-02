# TAREAS — nv-15

Spec: [spec.md](spec.md). Plan: [plan.md](plan.md). Constitución: [rectora](../../.specify/memory/constitution.md).

Estado: documentación propuesta; todas las casillas pendientes. Preparación documental y ejecución futura son distintas. Un encargo posterior de implementación y Ready de la historia son necesarios antes de construir producto. IDs locales; al citar usar contexto/spec/T-ID.

## Fase A — Refinamiento y decisiones

- [ ] T-006 →FR-090,NFR-090. Definir S08 como proyección pública opcional de dos saltos, reconstrucción e invalidación; GraphQL lectura acotada si se adopta. Acepta: AC-090 tiene precondición, resultado, fuente, contraparte y evidencia esperada definidos; decisiones pertinentes D01–D08 registradas sin firmas ficticias. Depende de: fuentes y dependencias explícitas en spec; sin dependencia de construcción local.
- [ ] T-020 →FR-100,NFR-100. Desglosar el escenario propio: Precisar habilitación opcional, límites de consulta y reconstrucción de S08 por snapshots/versiones/tombstones, sin almacenar contacto o domicilio. Acepta: AC-100 documenta caso positivo, negativo, fallo/reintento y responsable de cada superficie; exclusiones identificadas. Depende de: T-006.
- [ ] T-001 →FR-001,NFR-002. Preparar Ready de una historia acotada. Acepta: requisitos y AC seleccionados, contratos pertinentes formalizados antes de consumo, decisiones bloqueantes resueltas, revisión humana identificada y plan/tareas conciliados; sin declarar Ready por lint. Depende de: T-006, T-020 y entradas de la spec; no requiere completar todo el dominio.

## Fase B — Entregable y ejecución posterior

- [ ] T-002 →FR-001,NFR-003. Preparar el cambio del propietario con archivos reales al encargar implementación. Acepta: la historia seleccionada tiene archivos, versión, permisos y orden de cambio concretos; el estado documental no se presenta como aplicación creada. Depende de: T-001 y encargo correspondiente si hay ejecución de producto.
- [ ] T-010 →FR-001,NFR-001,NFR-003. Atender el requisito propio: Usuario obtiene candidatos relacionados con razón explicable. Acepta: AC-001, AC-002, AC-003 verificados con evidencia del alcance aplicable, incluyendo rechazo/fallo y sin sustituir integración real por doble; en diseño queda procedimiento pendiente explícito. Depende de: T-002 y contrato/decisiones de esa historia.
- [ ] T-021 →FR-100,NFR-100,NFR-003. Comprobar el escenario específico. Acepta: AC-100 — Eliminar autor y reejecutar evento antiguo no revive relación; indisponibilidad no bloquea directorio/pedidos; GraphQL rechaza operación no registrada o excedida sin dar permisos privados. Registrar resultado real o pendiente sin inventar ejecución. Depende de: T-020 y T-002; evidencia de producto solo tras implementación autorizada.
- [ ] T-003 →FR-001,NFR-003. Integrar frontera y datos reales de la historia. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 cuentan con comprobación de consumidor/provider, almacenamiento o dispositivo según superficie; fundamentos pueden producir evidencia documental identificada sin afirmar integración. Depende de: T-010, T-021, T-298, T-609.
- [ ] T-004 →NFR-001,NFR-100. Revisar privacidad, permisos, aislamiento, errores y fallos pertinentes. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 incluyen escenarios negativos; autoridad por recurso/estado se comprueba en API y presentación/limpieza en app; ninguna PII o credencial en evidencia. Depende de: T-003.
- [ ] T-007 →FR-090,NFR-090,NFR-002. Verificar el criterio transversal de la fase. Acepta: AC-090 — Grafo caído no bloquea funciones obligatorias; replay antiguo no revive autor borrado; cambiar destinatario elimina relación anterior; consulta excedida se rechaza. Distinguir revisión documental de ejecución futura. Depende de: T-004; preparación del escenario ya en T-006.

## Fase C — Cierre y revisión

- [ ] T-005 →NFR-002,NFR-003. Conciliar documentos, ejecutar gates pertinentes y actualizar Memoria. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 tienen resultado/evidencia o pendiente explícito; salida/exit, entorno, limitación y revisor reales registrados; sin aprobación automática. Depende de: T-004, T-007 y checklist del incremento.

## Verificación disponible

Hoy: powershell -NoProfile -File scripts/check_sdd.ps1 desde la raíz correspondiente, más revisión de integridad de IDs y dependencias. Solo documental. Build/test/proveedores/Android no ejecutados por esta fase.

## Historias de operación — refinamiento e implementación futura

- [ ] T-298 →FR-298,NFR-001,NFR-003. Definir y después construir/comprobar EP-098 (POST /graphql/social/v1) en S08; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-298: EP-098: Extensión opcional de lectura, sin mutations ni Cypher arbitrario. Authorization por recurso/campo, dos saltos y profundidad propuesta 4; máximo 100 objetos, una operación y timeout 2 s. Coste y rate limit por usuario. No caché compartida de datos personalizados. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Opcional; no bloquea alcance obligatorio si no se habilita. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.

## Funcionalidades y obligaciones verificables

- [ ] T-609 →FR-609,NFR-002,NFR-003. Definir y atender S08 Relaciones y descubrimiento. Acepta: AC-609: Reconstrucción, borrado y orden de eventos; sin permisos privados basados en grafo. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada. Delimitar S08 con Eventos; GraphQL de lectura opcional, dependencias S02/S03/S01; colas y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.
