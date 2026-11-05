# NV-22 — Operación de datos, migraciones y recuperación

Versión: 0.4.1 | Fecha: 2026-11-02 | Rama prevista: `nv-22`

## 1. Objetivo

Preparar creación idempotente, validadores/índices, migración compatible, respaldo/restauración y supresión.
Constitución: [rectora](../../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de capacidad existente, sin implementación.

## 2. Alcance / No-alcance

Dependencias: nv-01/nv-02/nv-03; cada propietario habilitado y saga nv-12. Propietario: Transversal por propietario.

- FR-001: Preparar creación idempotente, validadores/índices, migración compatible, respaldo/restauración y supresión.
- FR-002: Aplicar la regla: Cada servicio escribe solo su base; no scripts ejecutables ahora; ACL/TTL/transacciones dependen del plan real.
- FR-003: Integrar y recuperar sin cambiar propiedad de datos.
- FR-090: Refinar historias con fuente, actor, precondición, contrato/revisión y exclusiones.
- FR-100: Resolver el escenario: Crear dos veces no duplica; restaurar respeta versión y reaplica supresiones antes de servir; rollback conserva consistencia.
- NFR-001: Aislamiento por identidad/recurso/estado y datos mínimos según constitución.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad definidos antes de implementar.
- NFR-090: Sin proveedor/costo/stack/firma supuestos como ratificados.
- NFR-100: Este desglose no añade servicios, endpoints o pantallas por sí mismo.

Fuera: código/proveedores/infra ejecutados ahora, datos de base ajena y nuevas operaciones no justificadas.

### Funcionalidades y obligaciones con dueño explícito

- FR-611: I02 Almacenamiento y caché: Documentos, coordinación, caché y archivos por responsabilidad. Datos propios: Propiedad separada.
- FR-414: R14 NoSQL: Atender el requisito académico 14 en su superficie y coordinar la contraparte.

## 3. Contratos API

[Inventario](../../../docs/contracts/inventario.json) y [contratos](../../../docs/contracts/README.md). Operaciones propias se detallan por EP; sin EP el entregable operativo no requiere ruta inventada.
[Acuerdo](../../../../docs_nexo/SDD_ALINEACION.md) y [fuentes](../../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md).

### Asignación primaria vigente 0.4.0

Sin operaciones primarias nuevas: entregable de gobierno/operación, sin endpoint inventado.

## 4. Modelo de datos

[Datos por propietario](../../../docs/modelo-datos.md). Transversal por propietario mantiene su frontera. Identificar documentos, índices, versión, eventos y retención por historia; no crear base por tener otra spec.

## 5. Seguridad

Cada servicio escribe solo su base; no scripts ejecutables ahora; ACL/TTL/transacciones dependen del plan real. [Checklist](../../../docs/asvs-l2-checklist.md). Identidad de servicio y permisos vigentes; sin PII/secretos en evidencia.

## 6. Tests / Gates

- [ ] AC-001: Crear dos veces no duplica; restaurar respeta versión y reaplica supresiones antes de servir; rollback conserva consistencia.
- [ ] AC-002: Actor sin permiso no ejecuta ni recibe campos privados; permitido obtiene solo resultado del contrato.
- [ ] AC-003: Fallo/reintento/dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-090: Cada historia identifica entrada/salida, precondición, revisión de contrato y evidencia esperada.
- [ ] AC-100: Cada servicio escribe solo su base; no scripts ejecutables ahora; ACL/TTL/transacciones dependen del plan real. Verificar caso positivo, negativo y recuperación pertinente con evidencia real cuando haya producto.

Solo lint/revisión documental ahora; unit/contrato/NoSQL/Redis/proveedor/recuperación al implementar con rutas reales.

- [ ] AC-611 → FR-611/NFR-002/NFR-003: ACL/índices/TTL/respaldos comprobados. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.
- [ ] AC-414 → FR-414/NFR-002/NFR-003: Justificar motor por función y bases por propietario; definir scripts idempotentes, llaves/índices/validadores, migración y restauración. SQLite es local. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

## 7. Plan de entrega

[Plan](plan.md), [tareas](task.md), [gates](checklists/gates.md). Integrar con fase origen sin implementar dos veces el mismo EP.

## Aprobación

Estado: Borrador documental; sin implementación ni evidencia de producto.
- [ ] Revisor humano: nombre/fecha pendientes.
- [ ] Decisiones pertinentes resueltas antes de Ready; no firma automática.

La traza acredita asignación documental, no pruebas ejecutadas. Las operaciones internas/admin y opcionales mantienen límites expresos.

## Integración y límites del desglose

Las historias de EP tienen un único dueño primario en el inventario y mapa bilateral; los requisitos agregados previos de fases origen verifican integración con sus subcapacidades. No son una segunda implementación. Una dependencia de cierre de experiencia integrada no impide construir el subset anterior que sí tiene contratos/entradas. Las condiciones bloqueantes específicas se resuelven antes de Ready de cada historia.

## Trazabilidad del incremento

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-001 | T-001, T-002, T-010 | AC-001, AC-002, AC-003, AC-090 |
| FR-002 | T-001, T-002, T-011 | AC-001, AC-002, AC-003, AC-090 |
| FR-003 | T-001, T-002, T-012 | AC-001, AC-002, AC-003, AC-090 |
| FR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-001 | T-010, T-011, T-004 | AC-001, AC-002, AC-100 |
| NFR-002 | T-001, T-007, T-005, T-611, T-414 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-414, AC-611 |
| NFR-003 | T-002, T-012, T-003, T-005, T-611, T-414 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-414, AC-611 |
| NFR-090 | T-006, T-007 | AC-090 |
| NFR-100 | T-020, T-021, T-004 | AC-002, AC-100 |
| FR-611 | T-611 | AC-611 |
| FR-414 | T-414 | AC-414 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.

## Preparacion de estructura y secuencia 0.4.1

Dueño de este incremento: Cada propietario de datos habilitado. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md) concretan la propuesta; no ratifican proveedores ni crean proyectos. Las rutas siguientes son objetivo hasta ejecutarse.

- FR-950: Antes de Ready, concretar archivos/rutas, dueño, referencias y pruebas del alcance: versionar cambios de datos y ensayar restauracion sin base compartida universal. Ubicacion objetivo: Infrastructure/Persistence/, scripts/ operativos y pruebas de recuperacion por servicio.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario, conservar Git/SDD y exigir CI de producto al introducir codigo. Jira/Confluence pospuestos no se inventan; ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950 → FR-950/NFR-950: Plan contiene rutas actuales/objetivo, decisiones previas, dependencia de entrada, historia seleccionada, suites/evidencia y dueño; matriz de tareas cubre el cambio y Ready espera esa preparacion. Si una ruta no aplica, justificar con superficie/autoridad, sin crear un proyecto artificial. Verificacion de producto queda pendiente hasta ejecutarse.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
