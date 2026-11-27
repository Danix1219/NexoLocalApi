# Auditoría de evaluación v2 — Backend (NexoLocalApi)

Respuestas a la sección **1. Backend** del documento *Auditoría de evaluación v2* (18/nov/2026).
Revisado el 2026-11-27 sobre `develop` + rama `fix/auditoria-backend`. Cada respuesta indica dónde comprobarla.

## Correcciones hechas durante la auditoría

| Hallazgo | Corrección | Dónde |
|---|---|---|
| Sin SAST en CI | Job `1 · Análisis estático`: **Semgrep** (`p/owasp-top-ten`, `p/csharp`), bloquea severidad ERROR | `.github/workflows/pipeline.yml` |
| Sin escaneo de IaC | **Trivy config** sobre `infra/terraform` (HIGH/CRITICAL, informe en el resumen) | `pipeline.yml` |
| Sin DAST | **OWASP ZAP baseline** contra el sitio recién desplegado | `.github/workflows/deploy-azure.yml` |
| 14 rutas sin ficha en `specs/endpoints` | Fichas nuevas: chat cifrado E2EE (12 rutas), entrega de mensajes, subidas/portada, elegibilidad de opinión | `specs/endpoints/10-mensajes/`, `14-imagenes-cdn/`, `09-opiniones/` |
| Buscador no entendía "se me tapó el baño" | Clasificador **ML.NET** + respaldo `gpt-4.1-mini` | rama `feature/buscador-intencion` |
| Asistente respondía 500 si la moderación caía | Responde `503 assist.unavailable` | PR #39 (ya en `develop`) |

## Respuestas

| # | Pregunta | Respuesta |
|---|---|---|
| 1 | Nube | **Microsoft Azure** (App Service Linux, Key Vault, Blob Storage, Front Door, Content Safety, Azure OpenAI, Azure SignalR, Application Insights) |
| 2 | ¿Se despliega en la nube? | **Sí.** `deploy-azure.yml` publica en App Service `nexolocal` al entrar a `main`, verifica `/health/ready` 6 veces y etiqueta `api-vX.Y.Z` |
| 3 | Servicios externos fuera de la nube principal | **MongoDB Atlas** (datos), **Upstash Redis** (sesiones revocadas, idempotencia, rate limit, backplane SignalR), **Firebase Cloud Messaging** (push), **Gmail SMTP** (correos de códigos), **Photon/OpenStreetMap** (geocodificación) |
| 4 | % de seguridad DevSecOps | **≈ 85 %** (estimación propia): secretos, SAST, SCA, IaC, DAST, pruebas de seguridad, Key Vault y TLS cubiertos; faltan OIDC en el despliegue de la API (usa perfil de publicación), firma de artefactos/SBOM, WAF y pentest manual |
| 5 | Controles implementados / faltantes (OWASP) | **Implementados:** SAST (Semgrep), SCA (`dotnet list package --vulnerable`, Dependabot), secretos (gitleaks + Key Vault + variables de App Service, `.env` fuera de git), DAST (ZAP baseline), IaC (Trivy, `terraform validate`), validación de entradas en cada endpoint (longitudes, regex, listas cerradas), Problem Details sin trazas, rate limiting, cifrado de direcciones con Key Vault (envelope), moderación Content Safety, sin CORS habilitado (API solo para la app móvil), HTTPS only + TLS 1.2. **Faltan:** WAF delante de la API (A05), SBOM y firma de artefactos (A08), alertas de seguridad en Application Insights (A09), pentest/DAST activo autenticado |
| 6 | ¿Nivel bancario/fintech? | **No del todo.** Cumple buena parte de ASVS L2 (`docs/asvs-l2-checklist.md`), pero un banco exigiría WAF, HSM/claves gestionadas por cliente, pentest externo, MFA y monitoreo SOC |
| 7 | Vulnerabilidades críticas | **Ninguna crítica conocida.** El job de dependencias no reporta paquetes vulnerables. Riesgos a vigilar: secretos compartidos por chat entre el equipo (rotarlos) y chat E2EE sin moderación de contenido (decisión documentada) |
| 8 | Herramientas DevSecOps en CI | gitleaks, **Semgrep (SAST)**, **Trivy (IaC)**, `dotnet format`, NuGet vulnerable/deprecated, `terraform fmt/validate`, NetArchTest (fronteras), xUnit unitarias/integración (Testcontainers)/seguridad, puerta de calidad, trazabilidad de PR y flujo Git |
| 9 | Herramientas DevSecOps en CD | Perfil de publicación guardado como secreto de GitHub (`AZURE_BACKAPP_PUBLISH_PROFILE`), entorno protegido `produccion`, sondas de salud en 2 instancias, **OWASP ZAP baseline (DAST)**, etiqueta de versión, `infra.yml` con Terraform y login a Azure por **OIDC** |
| 10 | Endpoints / sin spec | **89 rutas** (más el hub `/hubs/chat` y `/connect/token`). Tras la auditoría **todas tienen ficha** en `specs/endpoints` (91 fichas). Antes faltaban las 14 listadas arriba |
| 11 | Agentes de IA | **2**: Codex y Claude Code (`AGENTS.md`, `CLAUDE.md`, `docs/herramientas-ia.md`) |
| 12 | Skills | **0 en el repositorio**: por regla del equipo no se versionan carpetas `.claude/.codex` vacías ni supuestas (`docs/herramientas-ia.md`) |
| 13 | Memoria.md | **Sí**, `Memoria.md` en la raíz, con una entrada por incremento (decisión, verificación, pendientes) |
| 14 | Pruebas unitarias | **Sí: 191 aprobadas** (Common 48, Discovery 78, Engagement 54, Identity 4, Arquitectura 7) en `develop`; con el buscador, Discovery sube a 90 |
| 15 | Pruebas de seguridad | **Sí**: `tests/Nexo.Seguridad.Tests`, 91 casos (auth, IDOR, rate limit, cabeceras, borrado de cuenta, negociación del chat). Requieren Docker; corren en CI |
| 16 | Pruebas de integración | **Sí**: `tests/Nexo.Integration`, 55 casos con Mongo real (Testcontainers). Corren en CI |
| 17 | Endpoints sin algún tipo de prueba | Pantallas 15 (`/v1/screens/*`): solo unitarias. `GET /internal/orders/{id}/review-eligibility`: solo integración indirecta vía opiniones. Adjuntos E2EE (`PUT/GET .../attachments`): sin prueba de seguridad propia (sí integración) |
| 18 | Otras pruebas | Arquitectura (NetArchTest), contratos/integridad SDD (`scripts/test_sdd_integrity.py`), sonda de endpoints (`scripts/probe_endpoints.py`), verificación de Content Safety (`check_content_safety.ps1`), smoke de despliegue y DAST |
| 19 | Instancias del backend | **2 instancias** de App Service plan **B1** (`worker_count = 2`, `infra/terraform/main.tf`) |
| 20 | Dónde se aplica caché | HTTP `Cache-Control` en catálogo (`public, max-age=3600`), opiniones (`public, max-age=60`), imágenes (`private, max-age=300`) y `no-store` en datos privados; en la app, caché local sin conexión |
| 21 | ¿Redis? | **Sí**, Upstash Redis: revocación de sesiones, idempotencia, rate limit compartido entre instancias, backplane y tickets de SignalR |
| 22 | Logout | **Sí**, `POST /v1/identity/logout`: revoca el refresh token y guarda el `jti` del access token en Redis hasta que expira |
| 23 | MCP | **Ninguno en el proyecto.** Los agentes trabajan con el repositorio; no hay servidores MCP configurados |
| 24 | Bases de datos | **MongoDB Atlas** (NoSQL documental), 3 bases: `nexo_identity`, `nexo_discovery`, `nexo_engagement`. Redis (clave-valor) como apoyo |
| 25 | Optimización de BD | 68 índices declarados (únicos, parciales, compuestos, **TTL** para códigos/cupos, geoespaciales `2dsphere`), proyecciones, paginación por cursor, `MaxPoolSize=40`, transacciones solo donde hay dos escrituras, outbox |
| 26 | Load Balancer | **Sí**, el balanceador integrado de App Service reparte entre las 2 instancias (el despliegue lo comprueba). Front Door definido para medios |
| 27 | IaC | **Terraform** (`infra/terraform`: grupo, plan, web app, Log Analytics, App Insights, Storage, colas, Front Door) con su workflow `infra.yml` |
| 28 | SSL/TLS | **Sí**: `https_only = true`, `minimum_tls_version = "1.2"`, certificado de `*.azurewebsites.net` |
| 29 | Arquitectura | **Monolito modular con API** (Identity, Discovery, Engagement, Screens) con fronteras verificadas por pruebas y outbox entre módulos; preparado para separarse en microservicios (`docs/base-microservicios.md`) |
| 30 | Imágenes en CDN | **Parcial.** Front Door (CDN de Azure) está en Terraform, pero hoy las imágenes salen por `/v1/discovery/media/{id}` desde la API (Blob privado + moderación). Pendiente apuntar `MediaStorage__PublicBaseUrl` al endpoint de Front Door |
| 31 | Observabilidad | **Sí**: Application Insights + Log Analytics (Terraform y `APPLICATIONINSIGHTS_CONNECTION_STRING`), health checks `/health/live` y `/health/ready` |
| 32 | JWT con Redis | **Sí**: lista de revocación por `jti` en Redis, consultada en cada petición autenticada (`Program.cs`, `SessionRevocation.cs`) |
| 33 | Caching, Rate limiting, Middleware, Pagination, Indexing, JSON serializers, Payload compression | **Todos**: Cache-Control; `AddRateLimiter` + limitador compartido en Redis; middlewares de revocación, cancelación y excepciones; cursores opacos; 68 índices; `System.Text.Json` (`ConfigureHttpJsonOptions`); `AddResponseCompression` (Brotli/Gzip) |
| 34 | Versionamiento | **Sí**: API en `/v1/...`, SemVer en `VERSION` verificado por `.ci/check-version.py`, etiquetas `api-vX.Y.Z`, ramas `release/x.y.z` (`docs/versionamiento.md`) |

## Pendientes que quedan abiertos

- CDN de imágenes activo (punto 30) y WAF.
- SBOM/firma de artefactos y alertas de seguridad.
- Ejecutar localmente las suites de integración y seguridad requiere Docker Desktop encendido.
