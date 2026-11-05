# EP-01 — Pantalla principal del solicitante

Versión: 0.5.0 | Fecha: 2026-11-05 | Rama prevista: `ep-01`

## 1. Objetivo

Entregar el inicio por zona y modo al usuario que solicita servicio, con tarjetas y cursor estable.
Constitución: [rectora](../../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de endpoint sobre capacidad ya documentada en [nv-07](../../arquitectura/nv-07/spec.md); no crea operación nueva. Superficie de maqueta: §7.4 inicio y §9 tarjetas.

## 2. Alcance / No-alcance

Alcance: EP-030 GET /feed. Dependencias: [nv-01](../../arquitectura/nv-01/spec.md), [nv-02](../../arquitectura/nv-02/spec.md), [nv-03](../../arquitectura/nv-03/spec.md) y datos de [nv-22](../../arquitectura/nv-22/spec.md). Propietario: S03 Community.

- FR-001: Entregar la capacidad del endpoint: Entregar el inicio por zona y modo al usuario que solicita servicio, con tarjetas y cursor estable.
- FR-002: Aplicar la regla de frontera: Solo DTO público; sin coordenadas exactas ni datos de contacto; el modo de UI no concede privilegios.
- FR-003: Definir errores, reintentos y compatibilidad antes de implementar; integrar sin cambiar propiedad de datos.
- FR-004: Formalizar el contrato de cada operación propia: entradas, salida, códigos y versión, enlazado al inventario.
- NFR-001: Aislamiento por identidad, recurso y estado; datos mínimos y sin PII fuera del DTO.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad verificables; sin éxito ficticio ante dependencia caída.

Fuera: operaciones de otra spec, bases de otro propietario, proveedores no ratificados y pantallas de la app.

Nota de alcance: Primer endpoint que la app consume sin sesión; el gateway compone y degrada parcialmente sin fingir éxito.

## 3. Contratos API

[Inventario](../../../docs/contracts/inventario.json), [contratos](../../../docs/contracts/README.md) e [índice de endpoints](../../../docs/endpoints.md). El identificador de cuenta de este flujo es el correo electrónico; ver [registro de decisiones](../../../docs/decisions/registro.md).

### Asignación primaria vigente

- EP-030 `GET /feed` → [ficha](../../../docs/contracts/fichas-operaciones.md)

| EP | Método y ruta | Actor | Entidad | Regla |
|---|---|---|---|---|
| EP-030 | `GET /feed` | Público o cuenta según filtro | `posts` | Gateway agrega DTO públicos; para mine exige usuario; fallos parciales explícitos; ubicación privada no sale del servidor |

## 4. Modelo de datos

[Datos por propietario](../../../docs/modelo-datos.md). S03 Community conserva su frontera; índices, TTL y validadores se declaran en [nv-22](../../arquitectura/nv-22/spec.md). Conexión y cadena por servicio: [nv-01](../../arquitectura/nv-01/spec.md).

## 5. Seguridad

Solo DTO público; sin coordenadas exactas ni datos de contacto; el modo de UI no concede privilegios. [Checklist ASVS](../../../docs/asvs-l2-checklist.md). Sin PII ni secretos en evidencia; secretos por configuración protegida, nunca en el repositorio.

## 6. Tests / Gates

- [ ] AC-001: Actor permitido obtiene el resultado previsto del contrato con entrada válida.
- [ ] AC-002: Actor sin permiso o con estado inválido no ejecuta ni recibe campos privados.
- [ ] AC-003: Fallo, reintento o dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-004: El contrato formal existe y coincide con el inventario; la app no copia payloads inventados.

Hoy solo lint y revisión documental. Unit, contrato, integración y recuperación se ejecutan al implementar con rutas reales.

## 7. Plan de entrega

[Plan](plan.md), [tareas](task.md), [gates](checklists/gates.md). Integrar con nv-07 sin implementar dos veces el mismo EP.

## Aprobación

Estado: Borrador documental; sin implementación ni evidencia de producto.
- [ ] Revisor humano: nombre/fecha pendientes.
- [ ] Decisiones pertinentes resueltas antes de Ready; no firma automática.

## Trazabilidad del incremento

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-001 | T-001, T-003 | AC-001 |
| FR-002 | T-001, T-004 | AC-002 |
| FR-003 | T-005 | AC-003 |
| FR-004 | T-002 | AC-004 |
| NFR-001 | T-003, T-004 | AC-001, AC-002 |
| NFR-002 | T-001, T-006 | AC-001, AC-002, AC-003, AC-004 |
| NFR-003 | T-002, T-005 | AC-003, AC-004 |

## Preparacion de estructura y secuencia

Dueño de este incremento: S03 Community. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md).

- FR-950: Antes de Ready, concretar archivos, rutas, dueño, referencias y pruebas del alcance: EP-030 GET /feed. Ubicacion objetivo: services/community/src/ y services/community/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950: Plan contiene rutas actuales y objetivo, decisiones previas, dependencia de entrada, contrato, suites y dueño; T-001 no pasa Ready sin esa preparacion.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
