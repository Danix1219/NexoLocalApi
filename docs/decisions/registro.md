# Registro operativo de decisiones backend

Fecha de preparacion: 2026-11-03. Pertenece a nv-00. La lista D01-D08 del [acuerdo compartido](../../../docs_nexo/SDD_ALINEACION.md) conserva su significado; este registro concreta el impacto backend, evidencia y salida de cada decision. No altera decisiones moviles ni ratifica proveedores. Al resolver una decision, actualizar ambas referencias en el mismo cambio si cambia su significado.

## Hechos y acuerdos que pueden reutilizarse

| Evidencia o acuerdo | Estado observado/documentado | Limite |
|---|---|---|
| Backend por dominios G01/S01-S07 | Constitucion y mapa vigentes; S08 condicionado | Arquitectura interna hexagonal y paquetes siguen propuesta |
| Flutter consume APIs | Decision de producto documentada | No acceso a bases del servidor |
| Plantilla de Visual Studio net10.0 | Inspeccion previa de csproj/Program/Dockerfile registrada | No build/producto aceptado; .NET/YARP no se ratifican por archivo existente |
| Repositorios Danix1219/NexoLocalApi y NexoLocalApp | Publicacion/reglas/CI registrados en informe de equipo | No se ha refrescado estado remoto en esta fase |
| feature→develop→release→main y reintegracion | Acuerdo explicito del usuario | Danix1219 designado como revisor provisional; revisor independiente del equipo se incorporara despues; no inventar aprobacion independiente |
| Jira y pagina Confluence obligatorios | Politica local preparada | Datos reales y activacion pospuestos por el usuario; no publicar estos cambios hasta completarlos |
| Codex y Claude Code | Herramientas acordadas | Esta fase la realiza Codex; no hay ejecucion Claude ni agentes delegados por afirmar roles |

## Decisiones abiertas y puerta que bloquean

| ID | Confirmado o disponible | Falta resolver | Responsable/ratificador | Puerta |
|---|---|---|---|---|
| D01 | Plantilla net10.0 y propuesta de servicios/capas | Ratificar runtime/SDK de equipo, YARP, MongoDB/Redis, eventos y versiones exactas necesarias para la primera vertical | Arquitectura/equipo por identificar | Ready tecnico del primer incremento ejecutable; no preparacion documental |
| D02 | Flutter definido y separacion API/cliente | Decisiones de estado/navegacion/SQLite/almacenamiento seguro en su contexto | Arquitectura movil por identificar | Ready movil; backend solo necesita contrato de frontera acordado |
| D03 | Requisito dos replicas/TLS/observabilidad/CDN | Nube, region, presupuesto, cuentas/roles y prueba de costo | Infraestructura/producto por identificar | Provisionamiento y aceptacion de entrega, no escritura de specs |
| D04 | OTP/moderacion forman parte del alcance | Proveedor, cuotas, credenciales seguras, TTL/limites y comportamiento ante fallo | Identidad/confianza por identificar | Integracion y aceptacion real; una simulacion no firma SMS/moderacion |
| D05 | Privacidad, consentimiento y borrado requeridos | Titular/contacto, retencion/backups y textos/estados de consentimiento | Producto/privacidad por identificar | Formalizar y aceptar flujos de datos sensibles; no inventar politicas |
| D06 | ML/GenAI requeridos por alcance academico | Caso, dueño, dataset/modelo/metricas/proveedor y evaluacion | Equipo ML/producto por identificar | nv-13; no obliga a implementar IA antes de identidad |
| D07 | URLs GitHub y flujo de ramas acordados | Integrantes/revisor real, carpeta compartida, Play y datos Jira/Confluence | Gestion/equipo por identificar; Danix1219 titular GitHub conocido | Publicacion conforme al acuerdo y paquete/canal movil |
| D08 | Documentos fuente y cobertura preparados | Evidencias de entrevistas, referencias docentes exactas/commit y responsables de ratificacion | Producto/docente/equipo por identificar | Cierre de criterios correspondientes; no afirmar revision de fuentes ausentes |

## Como registrar una resolucion

Crear un ADR en docs/decisions/ con ID libre cuando exista una decision concreta que resolver. Campos: ID D relacionado, contexto y fuente/revision, alternativas, propuesta, motivo, efectos en specs/contratos/datos/app/CI, riesgos, estado, decisor/revisor reales, fecha y evidencia de ratificacion. No escribir un nombre/fecha de aprobacion por haber generado el ADR. Ninguna opcion se vuelve elegida por encabezar una lista.

Si se difiere una decision: registrar motivo, alcance que SI puede continuar, puerta que permanece bloqueada, responsable pendiente y condicion de reapertura. Una excepcion ademas necesita mitigacion y vencimiento/condicion de caducidad ratificados; sin ellos no se considera autorizada. No abrir por excepcion acceso entre bases ni publicar secretos.

## Diferimiento existente

Jira/Confluence: usuario indica que proporcionara datos mas adelante. Puede continuar la preparacion documental local; no se inventan URLs/tickets, no se activa un check con configuracion vacia y no se publica el nuevo payload pendiente. Tarea de activacion nv-00 conserva estado pendiente. Este diferimiento no ratifica D01-D08 ni acepta funcionalidad.

## Encargo posterior de base fisica (2026-11-03)

El usuario confirma: "Crear carpetas y proyectos base, sin logica de negocio". Se implementa estructura tecnica net10.0 con SDK local 10.0.302 para cuatro proyectos Identity, conforme al target de la plantilla. Ese encargo autoriza el scaffold; no se registra como ratificacion de YARP, MongoDB/Redis, SMS, eventos, nube o diseño funcional. D01 conserva sus decisiones pendientes y Ready funcional sigue abierto. Codigo local fuera del checkout Git por ubicacion solicitada; consolidacion/publicacion aun no realizadas.

## Correccion explicita del usuario: G01 y solucion original (2026-11-03)

El usuario rechaza la solucion paralela y confirma que el proyecto existente nexolocal.Api es Gateway G01; cada microservicio tendra su host. Se conserva nexolocal.Api.slnx, G01 no referencia proyectos de Identity y S01 usa services/identity/src/{Domain,Application,Infrastructure,Api}. [Evidencia vigente](../inicio-backend/correccion-base-microservicios.md). Esta decision aclara estructura/rol, no ratifica proveedores o negocio.

## Encargo de conexion a datos y canal de verificacion (2026-11-05)

El usuario encarga: implementar la conexion con MongoDB Atlas con variables en secretos, separar specs de arquitectura y specs de endpoint, y definir los primeros siete endpoints del usuario que solicita servicio con verificacion por correo en lugar de telefono.

Efectos registrados:

- S01 Identity incorpora `MongoDB.Driver` y conexion configurable por secreto, con validacion al arranque y `GET /health/ready`. Detalle y claves en [configuracion y secretos](../configuracion-secretos.md). Este encargo autoriza la conexion del propietario S01; **no ratifica D01**: cluster, region, tier, transacciones, Redis y YARP siguen pendientes, igual que los otros ocho hosts.
- El identificador de cuenta pasa de telefono a correo. Se actualizan en el [inventario](../contracts/inventario.json) los campos de EP-001, EP-003, EP-004 y EP-006 (proposito, actor, entradas y regla). Las rutas `/auth/otp` y `/auth/otp/verify` se conservan; renombrarlas a `/auth/email/*` es una decision de contrato aun no tomada. D04 (proveedor de envio) sigue abierta y cambia de SMS a correo.
- `specs/` queda separada en `specs/arquitectura/` (26 incrementos de capacidad y gobierno) y `specs/endpoints/` (una spec por operacion). Los validadores `scripts/check_sdd.ps1`, `scripts/check_sdd_integrity.py` y `.ci/verify-sdd.ps1` recorren ambas subcarpetas y aceptan el patron `ep-NN`.

Limite: no hay build ni arranque verificados en este checkout (el SDK 10.0.302 que exige `global.json` no esta instalado). Ninguna tarea queda marcada como ejecutada y Ready sigue abierto.

## Ejecucion de los primeros siete endpoints (2026-11-05)

El usuario entrega la cadena de conexion de su cluster Atlas y encarga implementar ya los siete endpoints. Se ejecuta en `feature/Johann-arquitectura` desde `develop`.

- La cadena queda en el almacen de secretos del usuario para `nexolocal-identity` y `nexolocal-community`. No se versiona y no aparece en ningun archivo del repositorio.
- S01 implementa EP-001 a EP-008 con correo como identificador; S03 implementa EP-030. Comprobado contra Atlas: 35 de 35 verificaciones de [la sonda](../../scripts/probe_endpoints.py) mas la paginacion del feed. Datos de prueba eliminados.
- Esta ejecucion **no ratifica D01**: cluster, tier, region, transacciones, Redis y YARP siguen pendientes. D04 pasa de proveedor SMS a proveedor de correo, todavia sin elegir; mientras tanto el codigo solo se registra en Development.
- Decision de implementacion pendiente de contrato: el token de acceso es opaco y se valida leyendo la sesion en base, no un JWT firmado. Cambiarlo a JWT afecta al contrato de frontera con la app.
- Limite verificado: el equipo no tiene el SDK 10.0.302 de `global.json`; la compilacion se verifico en un proyecto desechable net9.0 y los hosts se ejecutaron desde el, no desde la solucion.

## SDK y driver de datos (2026-11-05)

El usuario intenta compilar en Visual Studio 17.14 y obtiene NETSDK1209 mas el aviso de `global.json`. Elige, entre las opciones planteadas, instalar el SDK 10 y trabajar por CLI sin cambiar el objetivo `net10.0`.

- Instalado SDK **10.0.403**; `global.json` pasa a esa version porque `rollForward: latestPatch` no cruza bandas de caracteristicas. No ratifica la version de equipo de D01: refleja lo instalado en este equipo.
- `MongoDB.Driver` 3.1.0 → **3.13.0** por vulnerabilidad alta transitiva en `Snappier 1.0.0` y moderada en `SharpCompress 0.30.1`. Tras el cambio el build queda en 0 avisos.
- Rutas con prefijo `/api/v1` conforme a [el indice de endpoints](../endpoints.md); `/health` queda fuera, como indica ese mismo documento.
- Limite abierto: Visual Studio 17.14 no compila `net10.0`. Actualizar la IDE a 18.0+ o bajar el objetivo es una decision de D01 aun no tomada; mientras tanto la compilacion y ejecucion son por CLI.

## Inventario de retiro nv-00 (2026-11-05)

T-001 se ejecuta antes de borrar nada. Conteo de archivos versionables (sin `bin/` ni `obj/`). El motivo comun es la enmienda E-001: un solo despliegue modular sustituye los 8 microservicios; el codigo vacio no se conserva como si fuera producto.

| Elemento | Archivos | Lineas | Motivo |
|---|---:|---:|---|
| `services/directory` | 24 | 102 | Scaffold sin implementacion; su capacidades pasan a `Discovery` |
| `services/community` | 29 | 427 | Scaffold sin implementacion; `Post`/`FeedModel` y `CommunityMongo` se revisan para migrar a `Discovery` (deuda) |
| `services/orders` | 34 | 149 | Scaffold sin implementacion; pasa a `Engagement` |
| `services/messaging` | 27 | 108 | Scaffold sin implementacion; pasa a `Engagement` |
| `services/trust` | 26 | 106 | Scaffold sin implementacion; pasa a `Engagement` + adaptador de moderacion |
| `services/notifications` | 29 | 140 | Scaffold sin implementacion; pasa a `Engagement` + worker |
| `services/relations` | 22 | 98 | Scaffold sin implementacion; S08 retirado por D-2 (`$graphLookup`) |
| `services/identity` | 48 | 1316 | **No se borra: se mueve** a `src/Domains/Identity` (FR-008) |
| `nexolocal.Api` (G01) | 6 | 27 | Host vacio; G01 se absorbe en `Screens` + `Nexo.Host` |
| `nexolocal.Api.slnx` | 1 | 71 | Solucion de microservicios; se sustituye por `src/Nexo.sln` |
| `docs/architecture-selection.json` | 1 | - | Fijaba 8 arquitecturas internas distintas; queda archivado en `artifacts/` |
| `.github/workflows/deploy-azure.yml` | 1 | 83 | Despliegue a Container Apps por servicio; se reescribe a App Service B1 con dos instancias |

Decisiones ratificadas por esta ejecucion: D-1 API modular por dominios; D-2 sin grafo (MongoDB `$graphLookup`); D-3 tres motores NoSQL en un despliegue.

Bloqueo registrado: los cambios sin commitear de Johann (`services/identity/src/Api/Program.cs`, `NexoLocal.Identity.Infrastructure.csproj`, `Cache/RedisRateLimiting.cs`) se conservan al mover el proyecto. El encargo del usuario autoriza ejecutar sin commit; no se firma T-011 como cerrada hasta que exista commit y revision.

## nv-00 T-001 — Inventario previo a la retirada (2026-11-06)

Medido antes de borrar nada, con `find` excluyendo `bin/` y `obj/`. Es la entrada de FR-009 y AC-009.

### Proyectos de servicio sin implementacion

| Ruta | Archivos `.cs` | Lineas | `.csproj` | Motivo de retirada |
|---|---|---|---|---|
| `services/directory` | 1 | 4 | 3 | Andamiaje vacio; su dominio pasa a `Nexo.Discovery.*` |
| `services/messaging` | 1 | 4 | 3 | Andamiaje vacio; su dominio pasa a `Nexo.Engagement.*` |
| `services/orders` | 1 | 5 | 4 | Andamiaje vacio; su dominio pasa a `Nexo.Engagement.*` |
| `services/relations` | 1 | 4 | 3 | Retirado por D-2: sin grafo, `$graphLookup` sobre MongoDB |
| `services/trust` | 1 | 4 | 3 | Andamiaje vacio; su dominio pasa a `Nexo.Engagement.*` |
| `services/notifications` | 2 | 10 | 4 | Andamiaje vacio; su dominio pasa a `Nexo.Engagement.*` + worker |
| `services/community` | 3 | 363 | 3 | **Tiene codigo real**: migrado a Discovery antes de borrar |
| **Total retirado** | **10** | **394** | **23** | |

### Codigo que se conserva, no se borra

| Origen | Destino | Lineas |
|---|---|---|
| `services/identity` (14 archivos, 1361 lineas) | `src/Domains/Identity/Nexo.Identity.*` | 1237 en el destino |
| `services/identity/src/Infrastructure/Persistence/MongoOptions.cs` | `src/BuildingBlocks/Nexo.Common/Mongo/MongoOptions.cs` | compartido por los tres dominios |
| `services/identity/src/Infrastructure/Cache/RedisRateLimiting.cs` | `src/BuildingBlocks/Nexo.Common/Redis/SharedRateLimiter.cs` | trabajo de Johann aprovechado en FR-006 |
| `services/identity/src/Api/Program.cs` | `src/Host/Nexo.Host/Program.cs` | un host con dos roles |
| `services/community` feed (`FeedModel`, `MongoFeedReader`, `PostCollection`) | `Nexo.Discovery.Application/Feed` e `Infrastructure/Feed` | las 363 lineas utiles migradas |

La diferencia de 1361 a 1237 lineas no es codigo perdido: `MongoOptions`, `RedisRateLimiting` y `Program.cs` salieron del dominio hacia los bloques comunes y el host.

### Hosts y solucion

| Elemento | Motivo |
|---|---|
| `nexolocal.Api/` (gateway G01) | Sustituido por `src/Host/Nexo.Host`: con un solo despliegue no hay nada que enrutar entre procesos |
| `nexolocal.Api.slnx` | Sustituida por `src/Nexo.sln`, con 28 proyectos de la nueva estructura |

### Documentos y validadores

| Elemento | Destino | Motivo |
|---|---|---|
| `docs/architecture-selection.json` | Se archiva en `docs/decisions/archivo/` | Definia 8 arquitecturas internas distintas; FR-001 fija **una** |
| `scripts/check_architecture.py` | Se retira | Validaba la politica de referencias de los 28 proyectos viejos; lo sustituye NetArchTest en `tests/Nexo.Architecture.Tests` (T-006, AC-004) |
| `scripts/Test-ScaffoldHosts.ps1` | Se retira | Comprobaba los 9 hosts del andamiaje anterior; ya no existen |
| `.ci/product-ci.json` y `backend-base.yml` | Se reapuntan | Referencian `nexolocal.Api.slnx` |

**Nada borrado en este punto.** La retirada se ejecuta en T-012 y se confirma mas abajo.

## nv-00 T-012 — Retirada ejecutada (2026-11-06)

Ejecutada con `git rm`, asi que el historial de cada archivo se conserva y es recuperable.

| Retirado | Resultado |
|---|---|
| `services/` completo (8 carpetas, 23 `.csproj`) | Eliminado. Identity y el feed de community viven en `src/Domains/` |
| `nexolocal.Api/` (gateway G01) | Eliminado. Lo sustituye `src/Host/Nexo.Host` |
| `nexolocal.Api.slnx` | Eliminada. La solucion es `src/Nexo.sln` con 28 proyectos |
| `scripts/check_architecture.py` | Retirado. Lo sustituye NetArchTest en `tests/Nexo.Architecture.Tests` |
| `scripts/Test-ScaffoldHosts.ps1` | Retirado. Comprobaba los 9 hosts del andamiaje anterior |
| `docs/architecture-selection.json` | Archivado en [docs/decisions/archivo/](archivo/architecture-selection.json) |

Reapuntados para que el build no quede roto: `.ci/product-ci.json` (`solution` → `src/Nexo.sln`) y `.github/workflows/backend-base.yml` (restore, build y pruebas sobre `src/Nexo.sln`; las fronteras las valida NetArchTest).

Verificacion: `dotnet build src/Nexo.sln` → **0 errores, 0 advertencias** despues de la retirada. Quedan exactamente 28 `.csproj`, los de la nueva estructura.

## nv-00 T-015 — Costo del despliegue B1 con dos instancias (2026-11-06)

`deploy-azure.yml` deja de desplegar un contenedor por servicio en Container Apps y pasa a **un** App Service plan con **una** app y **dos** instancias, con el rol worker como WebJob continuo en el mismo plan.

| Concepto | Cantidad | Costo mensual estimado |
|---|---|---|
| App Service plan B1 Linux (1 vCPU, 1.75 GB) | 2 instancias | ~26 USD |
| Worker, WebJob continuo en el mismo plan | 1 | 0 USD |
| MongoDB Atlas M0 | 1 cluster, 3 bases | 0 USD |
| **Total del plan** | | **~26 USD/mes** |

Contra la alternativa descartada: un plan por servicio para 8 servicios rondaba los 208 USD/mes, por encima de los 100 USD de credito (D-1).

**Limite honesto del calculo**: los ~26 USD cubren el plan de computo. El hospedaje de Redis **no** esta incluido y FR-006 depende de el: Azure Cache for Redis Basic C0 añade del orden de 16 USD/mes, lo que llevaria el total cerca de 42 USD. Sigue dentro del credito, pero la cifra de 26 USD de la enmienda E-001 es solo el computo. Elegir el Redis concreto y su costo queda pendiente de decision.

**Sin desplegar**: el workflow solo corre en `push` a `main` y el despliegue real necesita encargo aparte, mas las variables `AZURE_PLAN_NAME`, `AZURE_WEBAPP_NAME`, `AZURE_RESOURCE_GROUP` y las credenciales OIDC.

## Retirada de las operaciones de negocio (2026-11-06)

El usuario indica que los endpoints publicados **no son los que corresponden**: el conjunto de operaciones estaba mal elegido, no es lo que la aplicacion necesita primero. Decide retirarlos por completo, incluido el dominio y sus pruebas.

### Que se retiro

| Elemento | Detalle |
|---|---|
| Identity | `AuthService`, `Ports`, `Account`, `Session`, `EmailChallenge`, los cuatro archivos de endpoints, adaptadores de Mongo, el emisor de correo y `Secrets` |
| Discovery | `FeedModel`, `Post`, `IFeedReader`, `MongoFeedReader`, `PostCollection` y los 14 archivos de endpoints |
| Engagement | Los 11 archivos de endpoints sin implementacion |
| Pruebas | 29 de `Nexo.Identity.Unit` y 7 de `Nexo.Discovery.Unit` |
| Contrato del equipo movil | Archivado con aviso en [archivo/](archivo/contrato-primeros-7-endpoints-retirado.md); describia operaciones que ya no existen |

### Que se conserva

La **estructura de nv-00 intacta**: los 28 proyectos, las cinco capas por dominio, los grupos `/v1/identity`, `/v1/discovery`, `/v1/engagement` y `/v1/screens` listos para colgar operaciones sin tocar el host, los bloques comunes, Redis, las tres bases y las sondas. `dotnet build` sigue en **0 errores y 0 advertencias** y quedan **34 pruebas en verde**: Common 16, Architecture 7 e Integration 11.

### Conflicto con nv-00, declarado

Esta retirada **contradice FR-008** (*"conservar el dominio Identity existente sin reescribir su logica"*) y deja **AC-008 sin objeto**: ya no hay pruebas de Identity que conservar. No se disimula: FR-008 protegia un codigo que se da por no valido, y una decision posterior del responsable del producto lo sustituye. Si nv-00 se cierra formalmente, debe hacerlo con AC-008 marcado como **anulado por decision**, no como cumplido.

El codigo retirado sigue recuperable en el historial de Git; esta retirada se hizo con `git rm`.

### Pendiente

Las specs `specs/endpoints/ep-01` a `ep-07` **siguen en el repositorio y describen las operaciones retiradas**. Quedan obsoletas hasta que se definan las correctas: no implementarlas tal cual. El [inventario de operaciones](../contracts/inventario.json) tampoco se ha revisado contra esta decision.
