# NV-00 — Arquitectura base: API modular por dominios

Versión: 0.5.0 | Fecha: 2026-11-05 | Estado: Borrador documental | Rama prevista: `nv-00`

## 1. Objetivo

Constitución rectora: [fuente única](../../../.specify/memory/constitution.md) §3 Stack, §4 Arquitectura y §11 enmienda E-001.

Fijar **la** arquitectura del backend y dejarla construible: una **API REST modular por dominios** en .NET 10, con tres dominios de frontera estricta, un binario que arranca como API o worker, y despliegue en Azure App Service **B1 con dos instancias**.

Esta spec sustituye el contenido de gobierno que ocupaba nv-00 hasta la versión 0.4.2. El gobierno vive ahora en la constitución §7 y §11 y en `docs/decisions/registro.md`; **una spec de arquitectura define arquitectura**.

## 2. Alcance / No-alcance

Dependencias: ninguna. Es la fase de fundamento.

### Decisiones que esta spec ratifica

**D-1. Una API modular por dominios, no 8 microservicios.** Motivo cuantificado en la enmienda E-001: con plan **B1** (1 vCPU, 1.75 GB) y el requisito de **2 instancias**, 8 microservicios no caben en memoria compartiendo plan, y un plan por servicio cuesta ~208 USD/mes contra 100 USD de crédito. Esta arquitectura cuesta ~26 USD/mes. El requisito 15 concede *"libertad de uso API o Microservicios"*.

**D-2. Sin grafo. MongoDB resuelve las relaciones.** El documento de requisitos define Neo4j como *"opción propuesta"* y *"proyección reconstruible"* con API *"opcional"*, y exige *"comparar costo antes de elegir"*. AuraDB Free se auto-pausa por inactividad y se elimina si queda pausada ~30 días, con la evaluación el 2 de diciembre. Se usa `$graphLookup` más las colecciones `recommendations` y `saved`, alternativa que el propio documento contempla.

**D-3. Tres motores NoSQL, un despliegue.** MongoDB (documentos), Redis (clave-valor) y Blob+CDN (objetos). El número de motores y el número de despliegues son decisiones independientes.

### Requisitos locales

- **FR-001**: Solución .NET 10 con tres APIs de dominio (`Identity`, `Discovery`, `Engagement`) más `Screens`, cada una con las capas `Contracts/Domain/Application/Infrastructure/Endpoints` y **una única arquitectura interna para todas**.
- **FR-002**: Un solo host que arranca en rol `api` o `worker` según `NEXO_ROLE`.
- **FR-003**: Bloques comunes `Nexo.Common` (`Result<T>`, `PagedResult<T>` con cursor, `GeoPoint`, `ICurrentUser`) y `Nexo.Outbox` (mensaje, repositorio, servicio y despachador idempotente).
- **FR-004**: Frontera entre dominios verificada automáticamente: un dominio solo referencia `*.Contracts` de otro, nunca sus capas internas.
- **FR-005**: Tres bases en el cluster M0 (`nexo_identity`, `nexo_discovery`, `nexo_engagement`), cada una escrita por un único dominio, con `maxPoolSize` acotado para no agotar las conexiones del plan gratuito.
- **FR-006**: Estado compartido entre instancias en Redis: caché L2, contadores exactos de rate limit, revocación de sesión y backplane de tiempo real. **Nada de eso en memoria del proceso.**
- **FR-007**: Entorno local reproducible con MongoDB (réplica), Redis y emulador de colas; la API arranca contra él sin configuración manual adicional.
- **FR-008**: Conservar el dominio `Identity` existente (14 archivos, ~1361 líneas) sin reescribir su lógica; cambia su ubicación y su unidad de despliegue, no su código.
- **FR-009**: Retirar los 7 proyectos de servicio sin implementación y las tecnologías descartadas en constitución §3, dejando constancia de lo retirado y su motivo.

### Requisitos no funcionales

- **NFR-001**: Autorización, aislamiento por usuario, privacidad y errores explícitos según constitución §2 y §6.
- **NFR-002**: Trazabilidad y evidencia real; no declarar integración ejecutada con dobles.
- **NFR-003**: Las dos instancias son intercambiables: una petición puede caer en cualquiera con el mismo resultado, incluida la sesión revocada.
- **NFR-004**: Presupuesto verificable: un plan B1, dos instancias, ~26 USD/mes, worker en el mismo plan sin costo adicional.

### Fuera de alcance

Funcionalidad de negocio (nv-00 y siguientes), despliegue ejecutado, creación de infraestructura en la nube y publicación. Esta entrega es documental y de scaffolding.

## 3. Contratos API

[Endpoints vivos](../../../docs/endpoints.md) y [registro de contratos](../../../docs/contracts/README.md).

Esta spec **no publica operaciones de negocio**. Expone las dos sondas de plataforma ya previstas:

| Operación | Método y ruta | Regla |
|---|---|---|
| EP-089 Liveness | `GET /health/live` | Proceso vivo. **No consulta dependencias** |
| EP-090 Readiness | `GET /health/ready` | Comprueba Mongo y Redis. Nunca datos de usuario |

**Superficie por dominio**, que fija esta spec para todo lo que venga después:

```
/v1/identity/*     → API de Identidad     → nexo_identity
/v1/discovery/*    → API de Directorio    → nexo_discovery
/v1/engagement/*   → API de Coordinación  → nexo_engagement
/v1/screens/*      → composición de pantallas (sin base propia)
```

Convenciones transversales: rutas con versión `/v1/…`, errores RFC 9457 con `code` estable, paginación por cursor con `limit` máximo 50, `Idempotency-Key` en los POST que crean recursos, JSON `camelCase` con serializadores generados y compresión Brotli/Gzip (requisito 15: *API performance*).

## 4. Modelo de datos

Modelo del producto en [datos por propietario](../../../docs/modelo-datos.md). Esta spec fija **propiedad y fronteras**, no los agregados de negocio.

| Base | Dueño | Contenido |
|---|---|---|
| `nexo_identity` | API de Identidad | Cuentas, sesiones, dispositivos, revocación |
| `nexo_discovery` | API de Directorio | Perfiles, catálogo, trabajos, solicitudes, guardados, recomendaciones |
| `nexo_engagement` | API de Coordinación | Interés, pedidos, eventos de pedido, chat, opiniones, reportes, notificaciones |
| — | Screens | Sin base propia |

**Las tres viven en un solo cluster Atlas M0**, que admite 100 bases y 500 colecciones. No es una limitación de esta arquitectura: Atlas permite **un solo cluster gratuito por proyecto**, así que con microservicios sería igual.

Convenciones: `_id` como UUID v7 (`Guid.CreateVersion7()`), fechas UTC, montos como `int` en centavos MXN, ubicaciones GeoJSON `Point [lng, lat]`, colecciones en `snake_case` plural.

**Atlas Free son 512 MB en total, no por base.** Las imágenes nunca se guardan en MongoDB, solo su URL de CDN; el estado volátil vive en Redis.

### Relaciones sin grafo (D-2)

| Caso | Resolución en MongoDB |
|---|---|
| Recomendaciones de un trabajo | `recommendations`, único `{accountId, workId}` |
| "Quienes recomendaron esto también recomendaron…" | `$graphLookup` o `$lookup` de dos etapas |
| Prestadores relacionados por categoría y zona | Índice `2dsphere` + `categories` |
| Guardados | `saved`, único `{accountId, kind, targetId}` |

Cada dominio escribe solo su base. Lecturas entre dominios por interfaz de contrato con timeout y circuit breaker; cambios de estado por eventos de outbox con consumidores idempotentes.

## 5. Seguridad

- Sin secretos en archivos, logs ni mensajes de error; configuración por entorno.
- Redis no es historial durable, pero **sí es autoridad de revocación de sesión**: no se trata como caché expulsable (constitución §6).
- Con dos instancias la revocación debe ser efectiva en ambas: la lista de `jti` revocados vive en Redis, nunca en memoria del proceso. **Es lo que hace real el logout del requisito 10.**
- Salida por HTTPS/TLS (requisito 13).
- Frontera de dominio verificada en CI: una referencia cruzada a capas internas rompe el build.

Ver [checklist ASVS L2](../../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] **AC-001 → FR-001/FR-003**: `dotnet build` compila la solución completa; las pruebas de arquitectura confirman la dirección de dependencias dentro de cada dominio.
- [ ] **AC-002 → FR-002**: el mismo binario responde `/health/live` con `NEXO_ROLE=api`, y procesa la cola sin exponer HTTP con `NEXO_ROLE=worker`.
- [ ] **AC-003 → FR-003**: prueba unitaria de `Result<T>` en éxito y fallo y de `PagedResult<T>` con cursor; el despachador procesa el mismo `MessageId` dos veces **sin duplicar efecto**.
- [ ] **AC-004 → FR-004**: NetArchTest **falla** cuando un dominio referencia una capa interna de otro y **pasa** cuando solo referencia `*.Contracts`. Se comprueba el caso negativo, no solo el positivo.
- [ ] **AC-005 → FR-005**: existen las tres bases y cada una es escrita por un único dominio; una escritura cruzada en pruebas falla. `maxPoolSize` declarado y por debajo del límite del cluster.
- [ ] **AC-006 → FR-006/NFR-003**: con dos instancias levantadas, un token revocado en la instancia A es rechazado por la instancia B **en la petición siguiente**.
- [ ] **AC-007 → FR-007**: con el entorno local arriba, `/health/ready` responde 200; al detener Redis responde **503 y no 200 por omisión**.
- [ ] **AC-008 → FR-008**: las pruebas existentes de Identity siguen pasando tras el traslado, sin cambios en su lógica de dominio.
- [ ] **AC-009 → FR-009**: la solución no contiene los 7 proyectos retirados, y `docs/decisions/registro.md` enumera lo retirado con su motivo.
- [ ] **AC-010 → NFR-004**: la configuración de despliegue declara un plan B1 con dos instancias y el cálculo de costo consta en la evidencia.

Suites y gate: [roles y gates](../../../docs/roles-y-gates.md). Pendiente de ejecución: hoy solo hay validación documental.

## 7. Plan de entrega

Spec → plan → task → gates → Memoria → checklist → revisión. Escribir esta spec no despliega nada. Ver [runbook](../../../docs/runbook-release.md).

## Aprobación

Estado: Borrador documental; sin implementación ni evidencia de producto.

- [ ] Revisor humano identificado — **Johann** (autor de la arquitectura previa); nombre y fecha de revisión pendientes.
- [ ] CHECKLIST_PR.md completado con evidencia propia.
- [ ] Sin conflicto con constitución y requisitos del proyecto.

**Bloqueante de entrada**: la enmienda E-001 es una propuesta. Esta spec no pasa a refinada hasta que Johann la revise, por tratarse de un cambio sobre arquitectura que él definió.

No firmar ni marcar aprobada sin revisión real.

## Trazabilidad del incremento

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-001 | T-002, T-003 | AC-001 |
| FR-002 | T-007 | AC-002 |
| FR-003 | T-004, T-005 | AC-001, AC-003 |
| FR-004 | T-006 | AC-004 |
| FR-005 | T-008 | AC-005 |
| FR-006 | T-009 | AC-006 |
| FR-007 | T-010 | AC-007 |
| FR-008 | T-011 | AC-008 |
| FR-009 | T-001, T-012 | AC-009 |
| NFR-001 | T-009, T-013 | AC-006, AC-007 |
| NFR-002 | T-012, T-014 | AC-009 |
| NFR-003 | T-009 | AC-006 |
| NFR-004 | T-015 | AC-010 |

Cobertura de diseño, no ejecución aprobada.

## Mapa de dominios conservado

Los dominios del documento de requisitos siguen siendo el mapa funcional; cambia su unidad de despliegue, no su contenido.

| Dominio previo | Destino |
|---|---|
| G01 Entrada y composición | `Screens` + host |
| S01 Identidad | API de Identidad |
| S02 Directorio y descubrimiento | API de Directorio |
| S03 Comunidad | API de Directorio |
| S04 Solicitudes y pedidos | API de Coordinación |
| S05 Mensajería | API de Coordinación |
| S06 Confianza y contenido | API de Coordinación + adaptador de moderación |
| S07 Notificaciones | API de Coordinación + worker |
| S08 Relaciones con grafos | **Retirado** (D-2): `$graphLookup` sobre MongoDB |
