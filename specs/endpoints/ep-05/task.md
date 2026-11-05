# TAREAS — ep-05

[Spec](spec.md), [plan](plan.md). Todo pendiente; diseño no es producto.

- [ ] T-950 →FR-950,NFR-950. Concretar dueño S01 Identity, rutas objetivo services/identity/src/ y services/identity/tests/, archivos existentes y suites por caso de esta operación. Acepta: AC-950 queda refinado en plan con límites, pendientes y revisor identificable antes de Ready. Depende de: refinamiento de fuentes y dependencias de la spec.
- [ ] T-001 →FR-001,FR-002,NFR-002,FR-950,NFR-950. Cerrar Ready con decisiones resueltas y revisión humana identificada. Acepta: AC-950 con rutas y orden explicitados, documentado y después comprobado con evidencia real. Depende de: T-950.
- [ ] T-002 →FR-004,NFR-003. Formalizar el contrato de la operación: entradas, salida, códigos de error y versión. Acepta: AC-004 documentado y después comprobado contra el inventario, sin payload inventado. Depende de: T-001.
- [ ] T-003 →FR-001,NFR-001. Construir y comprobar el caso positivo del endpoint en el propietario habilitado. Acepta: AC-001 documentado y después comprobado con evidencia real, sin éxito ficticio. Depende de: T-002.
- [ ] T-004 →FR-002,NFR-001. Comprobar aislamiento, permiso y estado inválido. Acepta: AC-002 documentado y después comprobado con evidencia real, sin exponer datos fuera del DTO. Depende de: T-003.
- [ ] T-005 →FR-003,NFR-003. Comprobar fallo, reintento y dependencia caída. Acepta: AC-003 documentado y después comprobado con evidencia real. Depende de: T-003.
- [ ] T-006 →NFR-002. Conciliar evidencia, Memoria y revisión humana del incremento. Acepta: AC-001, AC-002, AC-003, AC-004 conciliados con evidencia diferenciada. Depende de: T-004, T-005.

Orden aplicable: la preparacion de rutas precede Ready; la construccion solo tras encargo y Ready. Ninguna tarea se marca por existir documentacion.
