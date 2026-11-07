# TAREAS — nv-00

Spec: [spec.md](spec.md). Plan: [plan.md](plan.md). Constitución: [rectora](../../../.specify/memory/constitution.md).

Estado: propuesta; todas las casillas pendientes. IDs locales; al citar usar contexto/spec/T-ID.

**Bloqueante de entrada**: la enmienda E-001 es propuesta. Ninguna tarea inicia sin la revisión de Johann. **T-011 además exige que sus cambios sin commitear estén guardados.**

A diferencia de las versiones anteriores de este incremento, estas tareas **producen código** y sus criterios se verifican con comandos reales.

---

## Fase 1 — Inventario antes de borrar

- [ ] T-001 →FR-009. Inventariar lo que se retira antes de tocarlo: los 7 proyectos de servicio sin implementación, los `.csproj` asociados, `docs/architecture-selection.json` y las tecnologías descartadas. **Revisar aparte `services/community`** (3 archivos, 363 líneas): decidir qué migra a Discovery y qué se descarta. Acepta: AC-009. Depende de: revisión E-001.
  - Criterio: `docs/decisions/registro.md` lista cada elemento con ruta, líneas y motivo. **Nada borrado todavía.**

## Fase 2 — Solución y bloques comunes

- [ ] T-002 →FR-001. Crear `src/Nexo.sln` y los proyectos vacíos de las 5 capas × 3 dominios + Screens + BuildingBlocks + Host + tests, con sus referencias entre proyectos. Acepta: AC-001. Depende de: T-001.
  - Criterio: `dotnet build src/Nexo.sln` compila sin errores ni advertencias.

- [ ] T-003 →FR-001. Fijar en `Directory.Build.props` el `TargetFramework net10.0`, nullable, warnings como errores y las versiones exactas del stack de constitución §3. Acepta: AC-001. Depende de: T-002.
  - Criterio: ningún proyecto declara versión propia de paquete; el build falla ante una advertencia.

- [ ] T-004 →FR-003. Implementar `Nexo.Common`: `Result<T>`, `PagedResult<T>` con cursor `afterId` y `limit` máximo 50, `GeoPoint`, `ICurrentUser` y la extensión `AddCommon()`. Acepta: AC-001, AC-003. Depende de: T-002.
  - Criterio: pruebas unitarias de `Result<T>` en éxito y fallo, y de `PagedResult<T>` rechazando `limit` mayor que 50.

- [ ] T-005 →FR-003. Implementar `Nexo.Outbox`: `OutboxMessage`, `IOutboxRepository` sobre MongoDB, `IOutboxService` y `OutboxDispatcher` con sondeo de 5 s e idempotencia por `MessageId`. Acepta: AC-003. Depende de: T-004.
  - Criterio: prueba unitaria que procesa el mismo `MessageId` dos veces y verifica **un solo efecto**.

## Fase 3 — Fronteras

- [ ] T-006 →FR-004. Escribir las pruebas de arquitectura en `tests/Architecture` con NetArchTest: dirección de dependencias dentro de cada dominio y prohibición de referenciar capas internas de otro dominio. Acepta: AC-004. Depende de: T-002.
  - Criterio: la prueba **falla** ante una referencia cruzada deliberada y **pasa** al dejar solo `*.Contracts`. Se verifica el caso negativo.

> T-006 va **antes** del traslado de Identity, para que el movimiento quede validado por la prueba y no al revés.

## Fase 4 — Host, datos y estado compartido

- [ ] T-007 →FR-002. `Nexo.Host` con `Program.cs` de dos roles según `NEXO_ROLE`, montaje de los `MapGroup` por dominio, serialización con generadores, compresión Brotli/Gzip y las sondas `/health/live` y `/health/ready`. Acepta: AC-002, AC-007. Depende de: T-004.
  - Criterio: con `NEXO_ROLE=api` responde `/health/live`; con `NEXO_ROLE=worker` procesa la cola y **no** expone HTTP.

- [ ] T-008 →FR-005. Registrar las tres bases como `IMongoDatabase` con clave por dominio y `maxPoolSize` acotado. **Confirmar antes el límite de conexiones real en el panel de Atlas.**. Acepta: AC-005. Depende de: T-007.
  - Criterio: cada dominio resuelve solo su base; una escritura cruzada en pruebas falla. `maxPoolSize` documentado con el límite verificado.

- [ ] T-009 →FR-006,NFR-001,NFR-003. Integrar Redis para caché L2, lista de `jti` revocados, contadores exactos de rate limit, idempotencia y backplane. **Reutilizar `RedisRateLimiting.cs` de Johann** si ya está commiteado. Acepta: AC-006. Depende de: T-007.
  - Criterio: con dos instancias locales, un token revocado en A es rechazado por B **en la petición siguiente**.

- [ ] T-010 →FR-007. `docker-compose.yml` con MongoDB en réplica, Redis y emulador de colas, más el script de inicio de las tres bases. Acepta: AC-007. Depende de: T-008.
  - Criterio: `docker compose up -d` y `/health/ready` responde 200; al detener Redis responde **503**, no 200.

## Fase 5 — Traslado y retirada

- [ ] T-011 →FR-008. Mover `services/identity` a `src/Domains/Identity` conservando su código y sus pruebas, y `services/identity/tests` a `tests/Identity.Unit`. Ajustar namespaces y referencias; **no reescribir lógica de dominio**. Acepta: AC-008. Depende de: T-006, **y de que Johann haya commiteado sus cambios**.
  - Criterio: las pruebas existentes de Identity pasan sin modificar sus asertos; T-006 sigue en verde.

- [ ] T-012 →FR-009,NFR-002. Retirar los 7 proyectos sin implementación y los `.csproj` sobrantes, archivar `architecture-selection.json` y registrar la decisión con el inventario de T-001. Acepta: AC-009. Depende de: T-011.
  - Criterio: la solución no contiene los proyectos retirados; `dotnet build` sigue verde; el registro enumera cada retirada con su motivo.

## Fase 6 — Seguridad, evidencia y despliegue

- [ ] T-013 →NFR-001. Revisión de seguridad del fundamento: sin secretos en el repositorio, configuración por entorno, TLS, cabeceras y el checklist ASVS L2 de esta fase. Acepta: AC-006, AC-007. Depende de: T-009.
  - Criterio: escaneo de secretos sin hallazgos; las entradas aplicables del checklist, resueltas.

- [ ] T-014 →NFR-002. Cerrar evidencia del incremento: comandos ejecutados, versiones, entorno, resultados y revisor real en `checklists/gates.md` y en `Memoria.md`. Acepta: AC-009. Depende de: T-012, T-013.
  - Criterio: cada AC enlaza su evidencia; ninguna casilla marcada sin ejecución.

- [ ] T-015 →NFR-004. Reescribir `deploy-azure.yml` de Container Apps por servicio a **App Service plan B1 con dos instancias**, con el worker como WebJob continuo en el mismo plan, y documentar el cálculo de costo. Acepta: AC-010. Depende de: T-007.
  - Criterio: el workflow declara un plan, una app y dos instancias; el cálculo (~26 USD/mes) consta en la evidencia. **Sin desplegar**: el despliegue requiere encargo aparte.

---

## Dependencias

```
E-001 (revisión Johann)
   └── T-001
        └── T-002 ── T-003
             ├── T-004 ── T-005
             │     └── T-007 ── T-008 ── T-010
             │            ├── T-009 ── T-013
             │            └── T-015
             └── T-006
                   └── T-011 (+ commit de Johann) ── T-012 ── T-014
```

Sin ciclos. T-014 cierra.

## Cobertura

| Tarea | Requisitos | Criterios |
|---|---|---|
| T-001 | FR-009 | AC-009 |
| T-002 | FR-001 | AC-001 |
| T-003 | FR-001 | AC-001 |
| T-004 | FR-003 | AC-001, AC-003 |
| T-005 | FR-003 | AC-003 |
| T-006 | FR-004 | AC-004 |
| T-007 | FR-002 | AC-002, AC-007 |
| T-008 | FR-005 | AC-005 |
| T-009 | FR-006, NFR-001, NFR-003 | AC-006 |
| T-010 | FR-007 | AC-007 |
| T-011 | FR-008 | AC-008 |
| T-012 | FR-009, NFR-002 | AC-009 |
| T-013 | NFR-001 | AC-006, AC-007 |
| T-014 | NFR-002 | AC-009 |
| T-015 | NFR-004 | AC-010 |

Los 9 FR y los 4 NFR tienen tarea; los 10 AC tienen comprobación asignada.
