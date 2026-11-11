# Constitución — API_NexoLocal

Versión: 0.4.0 documental. Estado: Borrador; stack y arquitectura ratificados por enmienda E-001 (§11), pendiente de revisión humana. Fuente de verdad: `.specify/memory/constitution.md`.

## 1. Propósito y alcance

Backend por dominios G01/S01-S07 y extensión S08, con APIs y datos propios. Una spec es un incremento liberable, no un permiso para construir todos los servicios de golpe.

## 2. Principios

Fuentes vigentes: requisitos académicos, maqueta aprobada y hallazgos H01-H12. Seguridad, aislamiento por usuario, datos mínimos, fallos explícitos y evidencia verificable. Spec antes de cambio observable. Nunca inferir autorización de los alias Tú/me ni del modo de pantalla. No copiar los defectos de la maqueta como reglas permitidas.

## 3. Stack

**Ratificado 2026-11-05** (enmienda E-001, ver §11). Stack fijo, sin alternativas abiertas:

| Capa | Tecnología |
|---|---|
| Framework | .NET 10 LTS · ASP.NET Core Minimal APIs |
| Identidad | OpenIddict + OpenIddict.MongoDb (JWT Bearer) |
| CQRS | Mediator.SourceGenerator (MIT) |
| Validación | FluentValidation |
| Base de datos | MongoDB Atlas Free (M0) — **una base por módulo**, 512 MB en total |
| Caché y estado volátil | Redis — caché L2, rate limiting exacto, tickets de WebSocket, pub/sub |
| Colas | Azure Storage Queues + patrón outbox/inbox |
| Archivos | Azure Blob Storage + CDN de Azure |
| Moderación | Azure AI Content Safety |
| Tiempo real | SignalR sobre Redis como backplane |
| Push | Firebase Cloud Messaging |
| Observabilidad | OpenTelemetry → Application Insights |
| Nube | **Azure App Service, plan B1, 2 instancias** |
| IaC | Terraform |

**Descartado y por qué** (no reabrir sin enmienda):

| Tecnología | Motivo |
|---|---|
| **MinIO** | El requisito 15 exige el CDN *de la nube seleccionada*. MinIO es autoalojado y no es CDN → Azure Blob + CDN |
| **Neo4j y grafo (S08)** | El documento de requisitos lo define como *"opción propuesta"*, como **proyección reconstruible** (no fuente de verdad) con *"una API de lectura opcional"*, y exige *"comparar costo (…) antes de elegir"*. Hecha esa comparación: **AuraDB Free se auto-pausa por inactividad y se elimina si queda pausada ~30 días**, con la evaluación el 2 de diciembre. El mismo documento ofrece la alternativa: *"MongoDB ofrece `$graphLookup` para ciertos recorridos"* y *"las menciones actuales continúan funcionando mediante REST y MongoDB"*. Las relaciones se resuelven con `recommendations`, `saved` y `$graphLookup`. **No se pierde ninguna función de la maqueta** |
| **GraphQL** | Era la superficie de lectura del grafo S08. Sin grafo, REST cubre el catálogo completo |
| **YARP** | Era la puerta de los microservicios. Con un solo despliegue no hay nada que enrutar |
| **Seq** | El requisito 15 pide observabilidad *en la nube* → Application Insights |
| **AWS y Google Cloud** | Nube única ratificada: Azure (cuenta Azure for Students disponible) |
| **Jira y Confluence** | Ningún requisito los pide; bloqueaban tareas con dependencias inexistentes |

Fijar versiones exactas al preparar la primera implementación; no hay paquetes instalados por estos documentos.

## 4. Arquitectura

**API REST modular por dominios** (enmienda E-001, ver §11). En la literatura técnica este patrón se llama *monolito modular*; se usa aquí el nombre descriptivo porque refleja mejor lo que se construye y lo que el requisito 15 autoriza ("libertad de uso API o Microservicios").

Un único despliegue con APIs de dominio de frontera estricta:

| Módulo | Responsabilidad | Base |
|---|---|---|
| `Identity` | Cuenta, sesión, dispositivos, borrado | `nexo_identity` |
| `Discovery` | Perfiles, catálogo, búsqueda geoespacial, trabajos, solicitudes, guardados | `nexo_discovery` |
| `Engagement` | Interés, pedidos, chat, opiniones, contacto, reportes, notificaciones | `nexo_engagement` |
| `Screens` | Composición por pantalla con `Task.WhenAll`. Sin base propia | — |

**Una sola arquitectura interna para todos los módulos**: `Contracts ← Domain ← Application ← Infrastructure ← Endpoints`. Caso de uso y permisos en aplicación; adaptadores de datos e integraciones en infraestructura. Verificado por NetArchTest en CI.

**Un host por réplica**: atiende HTTP y ejecuta el despachador de outbox en segundo plano. No se configura `NEXO_ROLE`. Decisión operativa del 2026-11-06, conciliada el 2026-11-08.

Frontera entre módulos: ningún dominio referencia proyectos de otro dominio. La propagación prevista usa eventos versionados y consumidores idempotentes; un mensaje en outbox no acredita implementación de su consumidor. **Cada módulo escribe solo su base.** Transacción local en Engagement para pedido y asignación.

Los 8 microservicios previos (G01/S01-S08) quedan como **mapa de dominios**, no como unidades de despliegue.

## 5. Contratos y datos

`docs/contracts/inventario.json` conserva la línea base histórica de IDs y trazabilidad; sus rutas antiguas no son aliases HTTP. Las fichas vigentes en `specs/endpoints/` gobiernan cada contrato refinado; `docs/endpoints.md` identifica las rutas registradas y sus límites contra el código actual. OpenAPI/AsyncAPI se formalizan por incremento; el índice no sustituye los esquemas ni acredita despliegue. IDs y dinero/fechas según contratos de Nexo, sin copiar prefijos SQL ni PascalCase legacy. Regla de teléfono/domicilio por estado y consentimiento.

## 6. Configuración y seguridad

Decisión del usuario (2026-11-08): verificación por correo mediante SMTP/MailKit (Gmail configurado por entorno). El móvil es contacto opcional declarado, sin SMS. Correo confirmado no implica teléfono confirmado. Ver el [registro vigente](../../docs/decisions/registro.md).

Sin secretos en archivos, errores o logs. Configuración por entorno. Redis no es historial durable ni una caché expulsable para revocación. Moderar antes de persistir contenido; rechazar por omisión cuando no se puede comprobar autorización. CORS/headers web se aplican a superficies HTTP pertinentes; no inventar antiforgery de Razor para Flutter.

## 7. Gobernanza SDD

Flujo `specs/arquitectura/nv-XX/spec.md → plan.md → task.md → checklists/gates.md`. Un cambio pequeño por revisión, tareas con FR/NFR y aceptación verificable. Docs vivos en docs/, especificaciones los enlazan. Rama de trabajo futura según id de spec. Un revisor humano real; no copiar firmas del profesor. Borrador documental → refinada → lista para desarrollar → implementada con evidencia → aprobada; Borrador con evidencia solo tras ejecutar producto y documentar resultados.

Cada FR/NFR debe aparecer en una tarea definida y cada AC tener comprobación asignada. No citar tareas ausentes ni mantener dependencias circulares. La cobertura de diseño queda en tabla por spec; plan y checklist deben usar los mismos IDs. Las capacidades transversales tienen dueño único y los recorridos enlazan su integración.

## 8. Gates de calidad

Hoy solo lint documental. Al implementar: restore→build→unit→integration→security→database→contract→critic→endpoints→sdd→coverage→delta advisory→mutation advisory→SAST; definir herramientas y comandos reales en la fase de calidad. Cobertura 45% y delta 80% advisory se toman como base docente a ratificar; jamás sustituyen casos críticos de permisos. No crear CI ficticio ni indicadores verdes sin ejecución.

## 9. Entrega y operación

Nexo requiere CI/CD, nube, dos instancias HTTP, TLS, observabilidad, CDN y Play/landing, aunque los ejemplos pospongan hosting/CD y Redis. Costo cero se verifica con cuenta/región. Una publicación, pipeline o despliegue requiere el encargo correspondiente; esta entrega es documental.

## 10. Memoria

Memoria.md registra Estado, Decisiones con porqué, Deuda y A evitar por fase. Breve, sin datos sensibles. Una regla permanente se documenta en constitución/AGENTS. No confundir esta bitácora de proyecto con la memoria global del asistente.

## 11. Revisión y excepciones

Registrar diferencia con el profesor y razón de producto, manteniendo formato y secuencia. Ratificar stack/proveedores y responsable antes de Ready del primer incremento. Controles propuestos y escenarios de aceptación no son pruebas ejecutadas.

### E-001 — API modular por dominios sobre microservicios (2026-11-05)

**Cambia**: §3 Stack y §4 Arquitectura. **Estado**: propuesta, pendiente de revisión de Johann.

**Motivo — restricción de infraestructura real.** La suscripción disponible es **Azure App Service plan B1** (1 vCPU, 1.75 GB RAM, ~13 USD/mes por instancia) y el requisito 15 exige **al menos 2 instancias**:

| Opción | Cuentas | Resultado |
|---|---|---|
| 8 microservicios en un plan B1 | 8 apps × 2 instancias = 16 procesos .NET en 1.75 GB; ~100 MB por runtime en reposo → ~1.6 GB solo al arrancar | No cabe en memoria |
| Un plan B1 por microservicio | 8 planes × ~26 USD = **~208 USD/mes** | Agota los 100 USD de crédito en ~2 semanas; la evaluación es el 2 de diciembre |
| **API modular por dominios** | 1 app × 2 instancias en 1 plan B1 = **~26 USD/mes** | Cabe, y el crédito cubre ~3.8 meses |

**Sobre la separación de datos.** El aislamiento físico de bases que justifica los microservicios no existe en el plan gratuito: Atlas permite **un solo cluster M0 por proyecto**, de modo que 8 servicios acabarían conectados al mismo cluster de 512 MB. La separación real que sí se obtiene —y que esta arquitectura conserva— es **una base por dominio dentro del cluster** (M0 admite 100 bases y 500 colecciones).

**Sobre varios motores de datos.** El número de motores y el número de despliegues son decisiones independientes: un solo proceso puede usar MongoDB, Redis y Blob a la vez, como hace esta arquitectura. "No todos los dominios usan el mismo motor" no obliga a separar despliegues.

**Motivos adicionales**: el requisito 15 concede *"libertad de uso API o Microservicios"*, así que los microservicios no son exigidos. App Service balancea entre instancias del mismo plan de forma nativa, con lo que el requisito 13 se cumple sin pagar Front Door. Y 8 despliegues implicaban 8 pipelines de CI-CD para los requisitos 5, 7 y 8, además de las 8 arquitecturas internas distintas que fijaba `docs/architecture-selection.json`.

**Qué se conserva**: el módulo `Identity` mantiene íntegro el trabajo existente (14 archivos, ~1361 líneas) y su arquitectura hexagonal, que pasa a ser la arquitectura única de todos los módulos. Los dominios G01/S01-S08 siguen siendo el mapa funcional.

**Qué se retira**: los 7 proyectos de servicio sin implementación (4–10 líneas cada uno), ~20 de los 27 `.csproj`, y las tecnologías listadas como descartadas en §3.

**Base previa**: §3 y §4 estaban redactados como *"propuesta previa"* y *"propuesta interna, no exigencia"*, con Azure y Neo4j *"condicionados a ratificación/costo/ensayo"*. Esta enmienda resuelve esa condición con datos de costo; no revierte una decisión ratificada.

## 12. Herramientas y coordinación acordadas

El equipo utilizará Codex y Claude Code. AGENTS es el procedimiento común y CLAUDE es entrada de lectura, sin duplicar reglas. Ejecución secuencial y revisión humana; roles documentados no activan subagentes.

Aplicar el [acuerdo SDD](../../../docs_nexo/SDD_ALINEACION.md), [cobertura de requisitos](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [protocolo IA](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). La matriz de versiones precede al desarrollo; CI/CD y canal móvil se preparan desde primera vertical, mientras entrega cierra evidencia global. Mantener propuestas técnicas como tales hasta ratificar y resolver bloqueantes de la historia antes de Ready.

## Flujo Git del equipo (acuerdo 2026-11-02)

Aplicar [flujo Git](../../docs/flujo-git.md): ramas por tarea desde develop, PR a develop, candidato release/* y PR de release a main; reintegrar main a develop. main permanece principal. CI y PR obligatorios; revision independiente exigible al incorporar revisor real. No declarar Ready o aceptacion por publicar o fusionar documentacion.

## Trazabilidad de cambios Jira/Confluence (2026-11-02)

Aplicar [convencion de cambios](../../docs/trazabilidad-jira-confluence.md): rama de trabajo con clave Jira y PR con titulo/clave coincidentes, enlace al ticket y enlace obligatorio a la pagina Confluence del cambio. API y app conservan su autoridad SDD; Confluence no reemplaza contratos versionados. No inventar tickets, enlaces ni firmas; activar el check requerido solo con configuracion real y ejecucion verificada.

## Preparacion estructural antes de Ready

Cada incremento concreta dueño, rutas actuales/objetivo, entrada de contrato/decision, referencias permitidas y suites/evidencia en su plan y una tarea trazada a requisito/criterio. Ready espera esa preparacion documental; la construccion autorizada e integracion son posteriores. Los fundamentos poseen migracion de plantilla y fronteras de proyectos, calidad la transicion CI, y cada dominio reutiliza esas capacidades. No duplicar servicios por spec ni exigir cierre completo de una fase si solo se necesita una entrada concreta. Aplicar [orden backend](../../docs/orden-desarrollo-backend.md), sin ratificar proveedores por inferencia.
