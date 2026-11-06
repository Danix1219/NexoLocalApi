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
| AC-001 | | | | |
| AC-002 | | | | |
| AC-003 | | | | |
| AC-004 | | | | |
| AC-005 | | | | |
| AC-006 | | | | |
| AC-007 | | | | |
| AC-008 | | | | |
| AC-009 | | | | |
| AC-010 | | | | |

## Salida

- [ ] Los 10 AC con evidencia ejecutada.
- [ ] `Memoria.md` actualizado: decisión E-001 con su porqué, lo retirado y la deuda abierta.
- [ ] Revisor humano real con nombre y fecha. No se firma por haber escrito documentos.
- [ ] **Sin desplegar**: el despliegue y la creación de infraestructura requieren encargo aparte.

## Deuda que sale de este incremento

- [ ] `services/community` (363 líneas): decidir qué migra a Discovery.
- [ ] Límite de conexiones de M0 confirmado y `maxPoolSize` ajustado en consecuencia.
- [x] Consolidación de specs 26 → 14, ejecutada el 2026-11-05. Ver [mapa](../../README.md).
- [ ] `deploy-azure.yml` migrado de Container Apps a App Service.
