# EP-02 — Registro de cuenta con correo

Versión: 0.5.0 | Fecha: 2026-11-05 | Rama prevista: `ep-02`

## 1. Objetivo

Crear cuenta única del solicitante a partir de una prueba de control del correo, con consentimiento versionado.
Constitución: [rectora](../../../.specify/memory/constitution.md), §§2/4/5/6/7/8/9/11. Desglose de endpoint sobre capacidad ya documentada en [nv-04](../../arquitectura/nv-04/spec.md); no crea operación nueva. Superficie de maqueta: §7.1 y §7.3 alta de cuenta.

## 2. Alcance / No-alcance

Alcance: EP-003 POST /auth/register. Dependencias: [nv-01](../../arquitectura/nv-01/spec.md), [nv-02](../../arquitectura/nv-02/spec.md), [nv-03](../../arquitectura/nv-03/spec.md) y datos de [nv-22](../../arquitectura/nv-22/spec.md). Propietario: S01 Identity.

- FR-001: Entregar la capacidad del endpoint: Crear cuenta única del solicitante a partir de una prueba de control del correo, con consentimiento versionado.
- FR-002: Aplicar la regla de frontera: Correo único normalizado; no revelar si el correo ya existe; nunca registrar la contraseña en claro ni en logs.
- FR-003: Definir errores, reintentos y compatibilidad antes de implementar; integrar sin cambiar propiedad de datos.
- FR-004: Formalizar el contrato de cada operación propia: entradas, salida, códigos y versión, enlazado al inventario.
- NFR-001: Aislamiento por identidad, recurso y estado; datos mínimos y sin PII fuera del DTO.
- NFR-002: Evidencia documental, doble e integración real diferenciadas; revisión humana pendiente.
- NFR-003: Errores, reintentos y compatibilidad verificables; sin éxito ficticio ante dependencia caída.

Fuera: operaciones de otra spec, bases de otro propietario, proveedores no ratificados y pantallas de la app.

Nota de alcance: Cambio de canal: el identificador es el correo, no phoneE164. Entrada `email` mas `proofToken` del flujo de ep-03.

## 3. Contratos API

[Inventario](../../../docs/contracts/inventario.json), [contratos](../../../docs/contracts/README.md) e [índice de endpoints](../../../docs/endpoints.md). El identificador de cuenta de este flujo es el correo electrónico; ver [registro de decisiones](../../../docs/decisions/registro.md).

### Asignación primaria vigente

- EP-003 `POST /auth/register` → [ficha](../../../docs/contracts/fichas-operaciones.md)

| EP | Método y ruta | Actor | Entidad | Regla |
|---|---|---|---|---|
| EP-003 | `POST /auth/register` | Público con prueba de correo | `accounts` | Correo único; nombre moderado; secreto protegido con hash adaptativo |

## 4. Modelo de datos

[Datos por propietario](../../../docs/modelo-datos.md). S01 Identity conserva su frontera; índices, TTL y validadores se declaran en [nv-22](../../arquitectura/nv-22/spec.md). Conexión y cadena por servicio: [nv-01](../../arquitectura/nv-01/spec.md).

## 5. Seguridad

Correo único normalizado; no revelar si el correo ya existe; nunca registrar la contraseña en claro ni en logs. [Checklist ASVS](../../../docs/asvs-l2-checklist.md). Sin PII ni secretos en evidencia; secretos por configuración protegida, nunca en el repositorio.

## 6. Tests / Gates

- [ ] AC-001: Actor permitido obtiene el resultado previsto del contrato con entrada válida.
- [ ] AC-002: Actor sin permiso o con estado inválido no ejecuta ni recibe campos privados.
- [ ] AC-003: Fallo, reintento o dependencia caída conserva consistencia y no confirma éxito ficticio.
- [ ] AC-004: El contrato formal existe y coincide con el inventario; la app no copia payloads inventados.

Hoy solo lint y revisión documental. Unit, contrato, integración y recuperación se ejecutan al implementar con rutas reales.

## 7. Plan de entrega

[Plan](plan.md), [tareas](task.md), [gates](checklists/gates.md). Integrar con nv-04 sin implementar dos veces el mismo EP.

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

Dueño de este incremento: S01 Identity. [Mapa backend](../../../docs/estructura-backend.md) y [orden SDD](../../../docs/orden-desarrollo-backend.md).

- FR-950: Antes de Ready, concretar archivos, rutas, dueño, referencias y pruebas del alcance: EP-003 POST /auth/register. Ubicacion objetivo: services/identity/src/ y services/identity/tests/.
- NFR-950: Separar preparacion documental de ejecucion; respetar capas y datos por propietario. Ningun endpoint, estado Ready o aceptacion se deriva de una carpeta.

- [ ] AC-950: Plan contiene rutas actuales y objetivo, decisiones previas, dependencia de entrada, contrato, suites y dueño; T-001 no pasa Ready sin esa preparacion.

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-950 | T-950, T-001 | AC-950 |
| NFR-950 | T-950, T-001 | AC-950 |
