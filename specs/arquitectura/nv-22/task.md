# TAREAS — nv-22

[Spec](spec.md), [plan](plan.md). Todo pendiente; diseño no es producto.

- [ ] T-006 →FR-090,NFR-090. Refinar fuente/actor/precondiciones/contrato. Acepta: AC-090 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: fuentes/dependencias de spec.
- [ ] T-020 →FR-100,NFR-100. Precisar escenario propio. Acepta: AC-100 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-006.
- [ ] T-001 →FR-001,FR-002,FR-003,NFR-002,FR-950,NFR-950. Cerrar Ready con decisiones y revisión humana. Acepta: AC-950 con rutas y orden explicitados; AC-090 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-950, T-006, T-020.
- [ ] T-002 →FR-001,FR-002,FR-003,NFR-003. Concretar cambio y archivos al encargar. Acepta: AC-001, AC-002, AC-003 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-001 y encargo correspondiente.
- [ ] T-010 →FR-001,NFR-001. Preparar creación idempotente, validadores/índices, migración compatible, respaldo/restauración y supresión. Acepta: AC-001 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-002.
- [ ] T-011 →FR-002,NFR-001. Cada servicio escribe solo su base; no scripts ejecutables ahora; ACL/TTL/transacciones dependen del plan real. Acepta: AC-002 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-002.
- [ ] T-012 →FR-003,NFR-003. Integrar/reintentar/recuperar. Acepta: AC-003 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-002.
- [ ] T-021 →FR-100,NFR-100. Crear dos veces no duplica; restaurar respeta versión y reaplica supresiones antes de servir; rollback conserva consistencia. Acepta: AC-100 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-002, T-020.
- [ ] T-003 →NFR-003. Integrar consumidor/provider o entregable operativo. Acepta: AC-001, AC-002, AC-003 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-010, T-011, T-012, T-021, T-611, T-414.
- [ ] T-004 →NFR-001,NFR-100. Verificar aislamiento y permiso. Acepta: AC-002, AC-100 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-003.
- [ ] T-007 →FR-090,NFR-090,NFR-002. Comprobar refinamiento. Acepta: AC-090 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-004.
- [ ] T-005 →NFR-002,NFR-003. Conciliar evidencia/Memoria y revisión. Acepta: AC-001, AC-002, AC-003, AC-090, AC-100 documentados y después comprobados con evidencia apropiada, sin éxito ficticio. Depende de: T-007.

## Funcionalidades y obligaciones verificables

- [ ] T-611 →FR-611,NFR-002,NFR-003. Definir y atender I02 Almacenamiento y caché. Acepta: AC-611: ACL/índices/TTL/respaldos comprobados. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada. Delimitar I02 con SDK privado, dependencias Servicios de negocio y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.
- [ ] T-414 →FR-414,NFR-002,NFR-003. Definir y atender R14 NoSQL. Acepta: AC-414: Justificar motor por función y bases por propietario; definir scripts idempotentes, llaves/índices/validadores, migración y restauración. SQLite es local. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter. Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. Evidencia real cuando corresponda; diseño solo no acredita aceptación. Depende de: T-002 y entradas/decisiones de la historia; trabajo documental previo no requiere código.

## Preparacion de rutas antes de Ready (0.4.1)

- [ ] T-950 →FR-950,NFR-950. Concretar dueño Cada propietario de datos habilitado, ubicacion objetivo Infrastructure/Persistence/, scripts/ operativos y pruebas de recuperacion por servicio, archivos reales actuales y alcance: versionar cambios de datos y ensayar restauracion sin base compartida universal. Registrar decisiones, contratos, dependencias de entrada y suites por caso; enlazar el mapa sin fabricar scaffolding o comandos de proyectos inexistentes. Acepta: AC-950 queda refinado en plan y evidencia documental, con limites/pendientes y revisor identificable antes de Ready; no ejecuta producto ni resuelve proveedores por inferencia. Depende de: T-006, T-020.

Orden aplicable: refinamiento y preparacion de rutas preceden Ready; construccion solo tras encargo/Ready, integracion espera las tareas propias adicionales, seguridad y cierre conservan sus dependencias. No esperar cierre completo de otra fase cuando solo se requiere su entrada concreta.
