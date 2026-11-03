# TAREAS — nv-02

Spec: [spec.md](spec.md). Plan: [plan.md](plan.md). Constitución: [rectora](../../.specify/memory/constitution.md).

Estado: documentación propuesta; todas las casillas pendientes. Preparación documental y ejecución futura son distintas. Un encargo posterior de implementación y Ready de la historia son necesarios antes de construir producto. IDs locales; al citar usar contexto/spec/T-ID.

## Fase A — Refinamiento y decisiones

- [ ] T-006 →FR-090,NFR-090. Definir contratos de frontera y matriz de versiones para el subset de una historia, con compatibilidad y ejemplos negativos. Acepta: AC-090 tiene precondición, resultado, fuente, contraparte y evidencia esperada definidos; decisiones pertinentes D01–D08 registradas sin firmas ficticias. Depende de: fuentes y dependencias explícitas en spec; sin dependencia de construcción local.
- [ ] T-020 →FR-100,NFR-100. Desglosar el escenario propio: Definir revisión y compatibilidad de contratos HTTP/WS/eventos por historia y revisión consumida por Flutter, separando públicas, internas y administrativas. Acepta: AC-100 documenta caso positivo, negativo, fallo/reintento y responsable de cada superficie; exclusiones identificadas. Depende de: T-006.
- [ ] T-001 →FR-001,NFR-002,FR-950,NFR-950. Preparar Ready de una historia acotada. Acepta: AC-950 con rutas y orden explicitados; requisitos y AC seleccionados, contratos pertinentes formalizados antes de consumo, decisiones bloqueantes resueltas, revisión humana identificada y plan/tareas conciliados; sin declarar Ready por lint. Depende de: T-950, T-006, T-020 y entradas de la spec; no requiere completar todo el dominio.

## Fase B — Entregable y ejecución posterior

- [ ] T-002 →FR-001,NFR-003. Desarrollar el diseño verificable de fundamento y, solo si se encarga, su configuración ejecutable. Acepta: la historia seleccionada tiene archivos, versión, permisos y orden de cambio concretos; el estado documental no se presenta como aplicación creada. Depende de: T-001 y encargo correspondiente si hay ejecución de producto.
- [ ] T-010 →FR-001,NFR-001,NFR-003. Atender el requisito propio: Definir revisión y compatibilidad de contratos HTTP/WS/eventos por historia y revisión consumida por Flutter, separando públicas, internas y administrativas. Acepta: AC-001, AC-002 verificados con evidencia del alcance aplicable, incluyendo rechazo/fallo y sin sustituir integración real por doble; en diseño queda procedimiento pendiente explícito. Depende de: T-002 y contrato/decisiones de esa historia.
- [ ] T-021 →FR-100,NFR-100,NFR-003. Comprobar el escenario específico. Acepta: AC-100 — Cada EP seleccionado identifica actor, tipos/nulos, errores, paginación, permisos por campo, reintento e idempotencia; un cambio incompatible tiene convivencia/migración antes del consumo. Registrar resultado real o pendiente sin inventar ejecución. Depende de: T-020 y T-002; evidencia de producto solo tras implementación autorizada.
- [ ] T-003 →FR-001,NFR-003. Integrar frontera y datos reales de la historia. Acepta: AC-001, AC-002, AC-090, AC-100 cuentan con comprobación de consumidor/provider, almacenamiento o dispositivo según superficie; fundamentos pueden producir evidencia documental identificada sin afirmar integración. Depende de: T-010, T-021, T-404.
- [ ] T-004 →NFR-001,NFR-100. Revisar privacidad, permisos, aislamiento, errores y fallos pertinentes. Acepta: AC-001, AC-002, AC-090, AC-100 incluyen escenarios negativos; autoridad por recurso/estado se comprueba en API y presentación/limpieza en app; ninguna PII o credencial en evidencia. Depende de: T-003.
- [ ] T-007 →FR-090,NFR-090,NFR-002. Verificar el criterio transversal de la fase. Acepta: AC-090 — Un consumidor puede reconocer nulos/enums/errores sin adivinar; una prueba con doble no se registra como integración real. Distinguir revisión documental de ejecución futura. Depende de: T-004; preparación del escenario ya en T-006.

## Fase C — Cierre y revisión

- [ ] T-005 →NFR-002,NFR-003. Conciliar documentos, ejecutar gates pertinentes y actualizar Memoria. Acepta: AC-001, AC-002, AC-090, AC-100 tienen resultado/evidencia o pendiente explícito; salida/exit, entorno, limitación y revisor reales registrados; sin aprobación automática. Depende de: T-004, T-007 y checklist del incremento.

## Verificación disponible

Hoy: powershell -NoProfile -File scripts/check_sdd.ps1 desde la raíz correspondiente, más revisión de integridad de IDs y dependencias. Solo documental. Build/test/proveedores/Android no ejecutados por esta fase.

## Funcionalidades y obligaciones verificables

- [ ] T-404 →FR-404,NFR-002,NFR-003. Definir y atender R04 Correspondencia con maqueta. Acepta: AC-404: Cubrir todos los M01–M27/W01 de la línea base y corregir H01–H12, sin eliminar funciones visibles ni promover simulaciones. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter. Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.

## Preparacion de rutas antes de Ready (0.4.1)

- [ ] T-950 →FR-950,NFR-950. Concretar dueño Contratos por propietario, ubicacion objetivo docs/contracts/ y adaptador Api/Contracts del propietario, archivos reales actuales y alcance: esquemas formales, errores y eventos; no crear un DTO universal ni duplicar autoridad en Flutter. Registrar decisiones, contratos, dependencias de entrada y suites por caso; enlazar el mapa sin fabricar scaffolding o comandos de proyectos inexistentes. Acepta: AC-950 queda refinado en plan y evidencia documental, con limites/pendientes y revisor identificable antes de Ready; no ejecuta producto ni resuelve proveedores por inferencia. Depende de: T-006, T-020.

Orden aplicable: refinamiento y preparacion de rutas preceden Ready; construccion solo tras encargo/Ready, integracion espera las tareas propias adicionales, seguridad y cierre conservan sus dependencias. No esperar cierre completo de otra fase cuando solo se requiere su entrada concreta.
