# Autoridad de contratos

El [inventario JSON](inventario.json) contiene 101 operaciones EP con propietario y spec viva asignada. Es catálogo de diseño, NO OpenAPI aprobado ni API implementada. [docs/endpoints.md](../endpoints.md) es su índice navegable generado; no editar una segunda forma del payload allí.

Formalizar OpenAPI/AsyncAPI/JSON Schema o SDL por frontera antes de la historia que lo consuma, con tipos, ejemplos, permisos, errores, compatibilidad y pruebas. Mantener un esquema autoritativo y generar modelos; specs enlazan, Flutter consume. La especificación de Nexo difiere del JSON legacy del profesor y no debe copiar str/int/RowVersion de SQL.

Mientras no exista contrato formal, formalContract=null y estado por-formalizar; no anunciar compilación, integración ni consumer/provider aprobados. La ubicación previa sdd/contracts/inventario.json es snapshot histórico 1.2.0 y no se edita para evolucionar contratos.

## Preparación y cambio de una frontera

Leer [acuerdo API–app](../../../docs_nexo/SDD_ALINEACION.md). Registrar spec productora/consumidora, subset EP/eventos, revisión y compatibilidad con clientes distribuidos. Cada operación define base/ruta, método, actor, requeridos/nulos, tipos/enums, DTO filtrado, errores, cursor/límites, idempotencia/reintentos y ejemplos negativos. Resolver semántica antes de implementar/generar clientes.

API pública usa /api/v1 como base; /internal, /health, /ws y /graphql conservan superficie propia según contrato. El catálogo incluye internas/admin: no todas son consumo Flutter. Eventos necesitan productor/consumidores, schemaVersion, eventId/sourceVersion, carga mínima y reglas de orden/reentrega/borrado. No se crean esquemas formales en esta revisión.

Ante incompatibilidad: cambiar spec/contrato primero, definir convivencia/migración y coordinar consumidor. La app enlaza autoridad y pruebas sin copiar payloads en su índice.

## Desglose funcional 0.4.0

El inventario conserva 101 IDs, métodos, rutas y propietarios; se reasigna la spec primaria para separar capacidades revisables. [Fichas de operaciones](fichas-operaciones.md) transcribe su diseño como vista documental y [cobertura local](../cobertura-funcional.md) enlaza FR/tarea/AC por operación. El [mapa bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) identifica 92 consumos móviles y 9 exclusiones directas internas/admin/infra.

Asignación primaria no cambia dueño de datos ni crea otro microservicio. Fases origen comprueban integración con subcapacidades, sin volver a construir la misma operación. Ningún formalContract se marca aprobado por este desglose; faltan tipos exactos, esquemas, compatibilidad y pruebas de producto por historia.
