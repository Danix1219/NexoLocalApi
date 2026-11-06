# Checklist — nv-00

Spec: [spec.md](../spec.md). Plan: [plan.md](../plan.md). Tareas: [task.md](../task.md).

**Primer incremento con código.** Los gates se verifican con comandos reales, no solo con lint documental.

## Entrada (antes de iniciar)

- [ ] Enmienda **E-001** revisada por Johann (cambia constitución §3 y §4).
- [ ] Cambios sin commitear de Johann guardados: `services/identity/src/Api/Program.cs`, `NexoLocal.Identity.Infrastructure.csproj` y `Cache/RedisRateLimiting.cs` (sin añadir a git). **Bloquea T-011.**
- [ ] Inventario de T-001 completo: nada se borra sin registro previo.
- [ ] Límite de conexiones del cluster M0 verificado en el panel de Atlas.

## DoD

- [ ] Spec y plan conciliados con archivos reales; tareas trazan FR/NFR.
- [ ] Contratos usados formalizados y pruebas de consumidor/provider ejecutadas cuando corresponda.
- [ ] Escenarios AC verificados con evidencia real; hallazgos negativos incluidos.
- [ ] Ninguna casilla marcada sin ejecución y sin revisor identificado.

## Gates técnicos

| # | Gate | Comando | AC |
|---|---|---|---|
| 1 | Compila | `dotnet build src/Nexo.sln` | AC-001 |
| 2 | Unitarias | `dotnet test src/Nexo.sln --filter "Category=Unit"` | AC-003 |
| 3 | Arquitectura | `dotnet test --filter "Category=Architecture"` | AC-004 |
| 4 | Integración | `docker compose up -d` + `--filter "Category=Integration"` | AC-005, AC-007 |
| 5 | Dos instancias | Dos contenedores del host; revocar en A, verificar rechazo en B | AC-006 |
| 6 | Degradación | Detener Redis; `/health/ready` debe dar **503**, no 200 | AC-007 |
| 7 | Identity intacto | Pruebas de Identity en verde tras el traslado, sin tocar asertos | AC-008 |
| 8 | Secretos | Escaneo sin hallazgos | NFR-001 |
| 9 | Retirada | Solución sin los 7 proyectos; registro con motivos | AC-009 |
| 10 | Costo | Workflow con plan B1 y dos instancias; cálculo documentado | AC-010 |

## Comprobaciones que exigen caso negativo

No basta el camino feliz:

- [ ] **AC-004**: la prueba de fronteras **falla** ante una referencia cruzada deliberada.
- [ ] **AC-005**: una escritura de un dominio sobre la base de otro **falla**.
- [ ] **AC-006**: el token revocado es rechazado por la **otra** instancia, no solo por la que lo revocó.
- [ ] **AC-007**: con Redis caído, readiness da 503 y **no** 200 por omisión.

## Evidencia

| AC | Comando y entorno | Resultado | Fecha | Revisor |
|---|---|---|---|---|
| AC-001 | `dotnet build src/Nexo.sln` · Windows, SDK 10.0.403 de `global.json` | **Compilacion correcta. 0 advertencias, 0 errores** (21 s), repetida despues de la retirada de T-012 | 2026-11-06 | pendiente de firma humana |
| AC-002 | `dotnet run --project src/Host/Nexo.Host` con `NEXO_ROLE=api` y con `=worker` | **Ejecutado.** Rol `api`: escucha en 127.0.0.1:5188 y `/health/live` responde **200**. Rol `worker`: proceso vivo y **ningun puerto en escucha** para su PID, comprobado con `Get-NetTCPConnection` | 2026-11-06 | pendiente de firma humana |
| AC-003 | `dotnet test src/Nexo.sln` con `docker compose up -d` | **70 pruebas, 0 fallos**: Common 16, Identity 29, Architecture 7, Discovery 7 e **Integration 11** | 2026-11-06 | pendiente de firma humana |
| AC-004 | `dotnet test tests/Nexo.Architecture.Tests` | **7 pruebas en verde**, incluido el caso negativo con `Nexo.Fixtures.BadDiscovery` | 2026-11-06 | pendiente de firma humana |
| AC-005 | `Una_escritura_de_un_dominio_no_aparece_en_la_base_de_otro` contra MongoDB real | **En verde.** Las tres bases existen (`mongo-init.js` las crea de forma idempotente) y lo escrito por un dominio no aparece en la base de otro | 2026-11-06 | pendiente de firma humana |
| AC-006 | `Un_jti_revocado_en_A_es_rechazado_por_B` contra Redis real | **En verde**, junto con `La_revocacion_no_alcanza_a_otras_sesiones`, el contador de rate limit compartido entre instancias y la idempotencia de un reintento que cae en la otra instancia | 2026-11-06 | pendiente de firma humana |
| AC-007 | Sondas con el entorno arriba y con Redis caido | **Los dos casos en verde**: con `docker compose up -d` readiness responde 200; con Redis caido responde **503 y no 200 por omision**; liveness sigue en 200 en ambos | 2026-11-06 | pendiente de firma humana |
| AC-008 | `dotnet test tests/Nexo.Identity.Unit` | **29 pruebas, 0 fallos** sobre `Account`, `Session`, `EmailChallenge` y `EmailProof`. Nota: el `services/identity/tests` original solo contenia README, no habia asertos que conservar | 2026-11-06 | pendiente de firma humana |
| AC-009 | Solucion sin los 7 proyectos; registro con motivos | **Ejecutado con `git rm`**: `services/` y `nexolocal.Api/` eliminados, `nexolocal.Api.slnx` retirada, `architecture-selection.json` archivado. Quedan 28 `.csproj` (20 en `src/`, 8 en `tests/`) y el build sigue en 0/0. Inventario y motivos en el [registro](../../../../docs/decisions/registro.md) | 2026-11-06 | pendiente de firma humana |
| AC-010 | Workflow con plan B1 y dos instancias | **Workflow reescrito**: `deploy-azure.yml` declara un plan B1, `--number-of-workers 2` con verificacion que falla si hay menos, una app por rol en el mismo plan y el calculo (~26 USD/mes). **Sin desplegar** | 2026-11-06 | pendiente de firma humana |

### Defectos encontrados y corregidos durante la verificacion

**Prefijo de Discovery fuera de la spec.** El feed quedaba en `/api/v1/feed` mientras el resto de dominios usaba `/v1/<dominio>/*`, contra la superficie que fija la seccion 3 de la spec. Corregido a `/v1/discovery/feed` y comprobado en la UI de OpenAPI. Arrastraba ademas el contrato entregado al equipo movil, que citaba las rutas de la arquitectura anterior: [actualizado](../../../../docs/contracts/contrato-primeros-7-endpoints.md) con un aviso de que la version previa no corresponde a nada desplegado.

**`mongo-init.js` solo creaba la primera base.** Usaba `target._domain`, y un nombre con guion bajo colisiona con las propiedades internas del objeto `db` de mongosh, asi que lanzaba tras la primera iteracion. Con `getCollection('_domain')` crea las tres.

### Arranque bloqueado por una dependencia caida

Con Mongo inalcanzable, `IdentityIndexBootstrap` y `DiscoveryIndexBootstrap` esperaban el timeout del driver **dentro de `StartAsync`**, de modo que el host tardaba ~30 s en escuchar y `/health/ready` no podia responder 503 mientras tanto. Ahora la verificacion de indices corre en segundo plano y el host escucha de inmediato: comprobado a los 12 s del arranque, con `live` en 200 y `ready` en 503. Sigue siendo idempotente y un fallo sigue quedando en el registro sin tumbar el proceso.

## Salida

- [ ] Los 10 AC con evidencia ejecutada.
- [ ] `Memoria.md` actualizado: decisión E-001 con su porqué, lo retirado y la deuda abierta.
- [ ] Revisor humano real con nombre y fecha. No se firma por haber escrito documentos.
- [ ] **Sin desplegar**: el despliegue y la creación de infraestructura requieren encargo aparte.

## Deuda que sale de este incremento

- [ ] `services/community` (363 líneas): decidir qué migra a Discovery.
- [ ] Límite de conexiones de M0 confirmado y `maxPoolSize` ajustado en consecuencia.
- [x] Consolidación de specs 26 → 14, ejecutada el 2026-11-05. Ver [mapa](../../README.md).
- [x] `deploy-azure.yml` migrado de Container Apps a App Service B1 con dos instancias, el 2026-11-06.
- [x] `Nexo.Identity.Unit` cubierto (29 pruebas) y `Nexo.Integration` escrito el 2026-11-06.
- [x] `tests/Nexo.Integration` ejecutado con Docker levantado el 2026-11-06: AC-005, AC-006 y AC-007 con evidencia, 11 de 11 en verde.
- [ ] `Nexo.Engagement.Unit` sigue vacio: el dominio Engagement son 53 lineas de andamiaje.
- [ ] `terraform validate` y `terraform apply` de `infra/terraform`: la Fase 0 no esta cerrada hasta que Azure responda 200.
- [x] Front Door descartado por costo el 2026-11-06: CDN en Cloudflare R2 (enmienda **E-002**), ~63 → ~28 USD/mes.
- [ ] **Requisito 15 en riesgo**: exige el CDN de la nube seleccionada y R2 es de Cloudflare. Necesita visto bueno del profesor antes de declararlo cumplido.
- [ ] Rol `worker` desplegado en las 2 instancias del plan: el despachador corre por duplicado. La idempotencia por `MessageId` evita doble efecto, pero conviene un cerrojo en Redis.
