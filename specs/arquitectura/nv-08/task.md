# TAREAS — nv-08

Spec: [spec.md](spec.md). Plan: [plan.md](plan.md). Constitución: [rectora](../../../.specify/memory/constitution.md).

Estado: documentación propuesta; todas las casillas pendientes. Preparación documental y ejecución futura son distintas. Un encargo posterior de implementación y Ready de la historia son necesarios antes de construir producto. IDs locales; al citar usar contexto/spec/T-ID.

## Fase A — Refinamiento y decisiones

- [ ] T-006 →FR-090,NFR-090. Distinguir trabajo, experiencia declarada y opinión vinculada; autoría real, menciones y guardados idempotentes. Acepta: AC-090 tiene precondición, resultado, fuente, contraparte y evidencia esperada definidos; decisiones pertinentes D01–D08 registradas sin firmas ficticias. Depende de: fuentes y dependencias explícitas en spec; sin dependencia de construcción local.
- [ ] T-020 →FR-100,NFR-100. Desglosar el escenario propio: Precisar autoría, experiencia declarada versus vinculada, elegibilidad interna y cambios/eliminación de menciones, guardados, reacciones y métricas. Acepta: AC-100 documenta caso positivo, negativo, fallo/reintento y responsable de cada superficie; exclusiones identificadas. Depende de: T-006.
- [ ] T-001 →FR-001,FR-002,NFR-002,FR-950,NFR-950. Preparar Ready de una historia acotada. Acepta: AC-950 con rutas y orden explicitados; requisitos y AC seleccionados, contratos pertinentes formalizados antes de consumo, decisiones bloqueantes resueltas, revisión humana identificada y plan/tareas conciliados; sin declarar Ready por lint. Depende de: T-960, T-950, T-006, T-020 y entradas de la spec; no requiere completar todo el dominio.

## Fase B — Entregable y ejecución posterior

- [ ] T-002 →FR-001,FR-002,NFR-003. Preparar el cambio del propietario con archivos reales al encargar implementación. Acepta: la historia seleccionada tiene archivos, versión, permisos y orden de cambio concretos; el estado documental no se presenta como aplicación creada. Depende de: T-001 y encargo correspondiente si hay ejecución de producto.
- [ ] T-010 →FR-001,NFR-001,NFR-003. Verificar la integración definida: Prestador publica trabajo/fotos; integra recomendaciones y guardados de este spec. Acepta: AC-002, AC-003 verificados con evidencia del alcance aplicable, incluyendo rechazo/fallo y sin sustituir integración real por doble; en diseño queda procedimiento pendiente explícito. Depende de: T-002 y contrato/decisiones de esa historia.
- [ ] T-011 →FR-002,NFR-001,NFR-003. Verificar la integración definida: Integrar experiencias/menciones/reputación desde este spec; no implementar un segundo caso de uso de opiniones. Acepta: AC-001, AC-002 verificados con evidencia del alcance aplicable, incluyendo rechazo/fallo y sin sustituir integración real por doble; en diseño queda procedimiento pendiente explícito. Depende de: T-002 y contrato/decisiones de esa historia.
- [ ] T-021 →FR-100,NFR-100,NFR-003. Comprobar el escenario específico. Acepta: AC-100 — Una opinión vinculada exige pedido done y dueño confirmado por S04; duplicados no alteran contadores; editar/quitar mención invalida proyecciones y autoría siempre usa ID del token. Registrar resultado real o pendiente sin inventar ejecución. Depende de: T-020 y T-002; evidencia de producto solo tras implementación autorizada.
- [ ] T-003 →FR-001,FR-002,NFR-003. Integrar frontera y datos reales de la historia. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 cuentan con comprobación de consumidor/provider, almacenamiento o dispositivo según superficie; fundamentos pueden producir evidencia documental identificada sin afirmar integración. Depende de: T-010, T-011, T-021, T-231, T-232, T-233, T-234, T-235, T-604.
- [ ] T-004 →NFR-001,NFR-100. Revisar privacidad, permisos, aislamiento, errores y fallos pertinentes. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 incluyen escenarios negativos; autoridad por recurso/estado se comprueba en API y presentación/limpieza en app; ninguna PII o credencial en evidencia. Depende de: T-003.
- [ ] T-007 →FR-090,NFR-090,NFR-002. Verificar el criterio transversal de la fase. Acepta: AC-090 — H03/H04/H07: cambio de cuenta no mezcla autoría; quitar mención actualiza diálogo; duplicar reintento de guardado no aumenta contador; fotos y pedidos se cuentan separados. Distinguir revisión documental de ejecución futura. Depende de: T-004; preparación del escenario ya en T-006.

## Fase C — Cierre y revisión

- [ ] T-005 →NFR-002,NFR-003. Conciliar documentos, ejecutar gates pertinentes y actualizar Memoria. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 tienen resultado/evidencia o pendiente explícito; salida/exit, entorno, limitación y revisor reales registrados; sin aprobación automática. Depende de: T-004, T-007 y checklist del incremento.

## Verificación disponible

Hoy: powershell -NoProfile -File scripts/check_sdd.ps1 desde la raíz correspondiente, más revisión de integridad de IDs y dependencias. Solo documental. Build/test/proveedores/Android no ejecutados por esta fase.

## Historias de operación — refinamiento e implementación futura

- [ ] T-231 →FR-231,NFR-001,NFR-003. Definir y después construir/comprobar EP-031 (POST /posts) en S03; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-231: EP-031: Categoría ofrecida, 0..4 medios propios aprobados; título automático hasta 60 caracteres. No exigir campos residuales problema/tiempo. Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.
- [ ] T-232 →FR-232,NFR-001,NFR-003. Definir y después construir/comprobar EP-032 (GET /posts) en S03; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-232: EP-032: Solo publicados; orden por createdAt y _id Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.
- [ ] T-233 →FR-233,NFR-001,NFR-003. Definir y después construir/comprobar EP-033 (GET /posts/{id}) en S03; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-233: EP-033: No exponer domicilio donde se realizó Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.
- [ ] T-234 →FR-234,NFR-001,NFR-003. Definir y después construir/comprobar EP-034 (PATCH /posts/{id}) en S03; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-234: EP-034: Volver a moderar cambios; conservar versión anterior solo según política Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.
- [ ] T-235 →FR-235,NFR-001,NFR-003. Definir y después construir/comprobar EP-035 (DELETE /posts/{id}) en S03; entradas/salida e invariantes enlazadas en fichas autoritativas, sin duplicar operación. Acepta: AC-235: EP-035: Ocultar feed e invalidar caché; purgar medios que queden sin referencias Actor permitido recibe el resultado previsto; validación, permiso/estado y fallo/reintento se comprueban sin exponer datos fuera del DTO. Previsto en línea base; contrato formal y aceptación pendientes. Depende de: T-002 y contrato formal/decisiones de esta historia; no exige construir todo el dominio.

## Funcionalidades y obligaciones verificables

- [ ] T-604 →FR-604,NFR-002,NFR-003. Definir y atender S03 Comunidad. Acepta: AC-604: Moderación previa y opinión vinculada validada. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada. Delimitar S03 con REST y eventos, dependencias S02, S04, S06 y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.

## Preparacion de rutas antes de Ready (0.4.1)

- [ ] T-950 →FR-950,NFR-950. Concretar dueño S03 Community, ubicacion objetivo src/Domains/community/ y services/community/tests/, archivos reales actuales y alcance: publicaciones y coordinacion con este spec/este spec; opinion vinculada espera pedidos. Registrar decisiones, contratos, dependencias de entrada y suites por caso; enlazar el mapa sin fabricar scaffolding o comandos de proyectos inexistentes. Acepta: AC-950 queda refinado en plan y evidencia documental, con limites/pendientes y revisor identificable antes de Ready; no ejecuta producto ni resuelve proveedores por inferencia. Depende de: T-006, T-020.

Orden aplicable: refinamiento y preparacion de rutas preceden Ready; construccion solo tras encargo/Ready, integracion espera las tareas propias adicionales, seguridad y cierre conservan sus dependencias. No esperar cierre completo de otra fase cuando solo se requiere su entrada concreta.

## Refinamiento del perfil de arquitectura

- [ ] T-960 →FR-960,NFR-960. Refinar el [perfil S03](../../../docs/arquitectura/servicios/s03-community.md) para la historia seleccionada, sin crear servicios vacíos. Acepta: AC-960 con patrón/motivo, invariantes, contrato, rutas, atomicidad, permisos y fallos comprobables; proveedores y revisión pendientes se registran sin inventarlos. Depende de: T-950.

## Tarea de trabajos autorizada
- [ ] T-991 →FR-991,FR-992,NFR-991. Implementar las cinco fichas de trabajos en Discovery con moderación cerrada, propiedad y publicación/recomendación/cuota atómicas. Acepta: AC-991/AC-992/AC-993 en WorkRulesTests, WorkPersistenceTests y WorkEndpointsTests de capas actuales. Depende de: T-002. Registrar evidencia y límites sin cerrar toda nv-08 ni firmar aprobación humana.
