# PLAN — nv-01

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Delimitar G01/S01-S07, propietarios de datos, entornos y configuración; preparar primera vertical sin construir todos los servicios.

Dependencias: nv-00.

## 2. Diseño técnico

API: inventario de bases por servicio y configuración sin secretos. App: zona pública, preferencias, sesión y caché privada separadas; token fuera de SQLite.

Autoridad: [contratos](../../../docs/contracts/README.md). [Acuerdo API–app](../../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. Documentar arranque, configuración faltante y fallo de dependencia del backend, identidades internas y propietarios sin afirmar proyecto creado; la UI GPS corresponde a app.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Cada comando crítico tiene frontera transaccional local; evento versionado incluye eventId, sourceVersion y correlación mínima, sin PII; reentrega, fuera de orden, cola de error y reconciliación tienen comportamiento definido.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-089 | FR-289 → T-289 → AC-289 | Actor Infraestructura interna; datos `outbox_inbox`; No consultar todas las dependencias en liveness Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-090 | FR-290 → T-290 → AC-290 | Actor Infraestructura interna; datos `outbox_inbox`; Comprobar dependencias indispensables del servicio, no datos de usuario Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| G01 Entrada y composición | FR-601 → T-601 → AC-601 | Delimitar G01 con REST y upgrade WebSocket, dependencias S01-S08 (S08 condicionado) y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |
| I01 Eventos e idempotencia | FR-610 → T-610 → AC-610 | Delimitar I01 con Eventos JSON versionados, dependencias Todos los propietarios y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |
| R03 Cinco componentes del proyecto | FR-403 → T-403 → AC-403 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Mapa concreto de implementacion backend (2026-11-02)
Seguir [estructura y orden backend](../../../docs/estructura-backend.md) para ubicar G01/S01-S08, capas, pruebas y la consolidacion de la plantilla Visual Studio. Precisa T-006/T-020 y la preparacion de T-002; no marca esas tareas completas ni Ready. La plantilla net10.0 existe fuera del checkout; no se ha reorganizado ni compilado. Incorporar solo la primera vertical y actualizar solucion/Docker/CI juntos al ejecutar el cambio autorizado.

## Rutas, propietario y orden verificable 0.4.1

Dueño: G01/S01 y fundamento comun. Rutas objetivo: `nexolocal.Api/, services/identity/src/, services/identity/tests/ y solucion raiz` (no creadas por este plan). Alcance propio: consolidacion de plantilla, capas, referencias, lanzamiento y Docker de primera vertical. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

T-951 / FR-951 / AC-951: Consolidar la primera plantilla en el checkout backend. Inventariar/comparar plantilla y elegir archivos reutilizables; reutilizar el proyecto original nexolocal.Api como G01 y mantener S01 con host/capas/pruebas propios al repositorio API existente, sin otro .git. Actualizar solucion, namespaces, referencias, contexto/rutas Docker y lanzamiento en el mismo cambio. Mantener origen hasta comprobar destino. Orden local: T-002, T-950 → T-951 → T-003. Comprobacion esperada: Archivo de origen/destino trazado, sin .vs/bin/obj/*.user/secretos; solucion y contenedor apuntan al host real; build/arranque se documentan despues de configurar CI real de nv-03. No exige completar toda nv-03 antes de crear el scaffold que esa fase valida.

T-952 / FR-952 / AC-952: Comprobar limites entre proyectos y servicios. Verificar Gateway sin referencias a proyectos de negocio; dentro de cada servicio, núcleo sin dependencias técnicas y adaptadores/hosts según el perfil vigente de cada dueño; S01/S04 conservan Domain/Application/Infrastructure y los restantes usan su división específica; impedir referencia a proyectos/datos ajenos. Definir y ejecutar la comprobacion con las rutas realmente creadas. Orden local: T-951 → T-952 → T-003. Comprobacion esperada: Dependencias permitidas pasan y una referencia cruzada prohibida se rechaza en comprobacion reproducible; cada proyecto tiene dueño y rollback del traslado comprobable, sin generar ocho servicios vacios.

## Evidencia parcial de scaffold autorizado (2026-11-03)

Estado corregido por indicacion del usuario: la solucion original nexolocal.Api.slnx contiene el proyecto original como G01 Gateway y cuatro proyectos S01 en services/identity/src. Dos hosts separados, cinco proyectos en total, sin logica de negocio. [Archivos y validacion real vigente](../../../docs/inicio-backend/correccion-base-microservicios.md). La propuesta paralela anterior fue retirada; no es la estructura activa.

La tarea propietaria de consolidacion y las de CI/referencias NO se marcan completas: codigo aun fuera del checkout Git, ensayo de imagenes Docker pendiente; los Dockerfiles de ambos hosts estan preparados, Ready/ratificacion y revision pertinentes pendientes; workflow de producto preparado sin ejecucion remota. El guard documental actual se mantiene; al consolidar csproj al checkout, la transicion a CI real debe ir en ese cambio. Esta evidencia parcial no ratifica proveedores ni contratos.

## Correccion solicitada: solucion original y microservicios

El usuario confirma G01 en el proyecto nexolocal.Api ya existente, y hosts propios para microservicios. La solucion unica sigue nexolocal.Api.slnx; el nombre paralelo anterior se retira. S01 usa services/identity/src/{Domain,Application,Infrastructure,Api}; no referencias de proyecto desde G01 a negocio S01. [Evidencia vigente](../../../docs/inicio-backend/correccion-base-microservicios.md) sustituye la organizacion del scaffold anterior. Las tareas de consolidacion/revision y ejecucion remota no se marcan completas por compilar localmente.

## Refinamiento de patrones del propietario

T-960 atiende FR-960/NFR-960 y AC-960 con el [perfil G01](../../../docs/arquitectura/servicios/g01-gateway.md). Orden: T-950 → T-960 → T-001. Registrar para la historia patrón/motivo, capas/puertos, frontera atómica, contrato, concurrencia y recuperación, suites y evidencia esperada. Las carpetas objetivo del perfil no son proyectos creados. [Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) aplican según la superficie; S08 permanece condicional.

## Base física ampliada por encargo explícito

El usuario autoriza el scaffold de todos los propietarios, además de G01/S01. [Estado y evidencia](../../../docs/inicio-backend/base-todos-microservicios.md). Proyectos/carpetas según perfil y host independiente; no equivale a implementación de patrones de negocio, Ready o aceptación. S08 permanece condicional; sin proveedores nuevos ni publicación. Consolidación Git/CI real y revisión siguen pendientes.

T-961 atiende FR-961/NFR-960 y AC-961 como ampliación autorizada del scaffold. Depende de T-960; no obliga al resto de historias a esperar implementación de todos los dominios. Comprobación: restore/build solución original, verificador y pruebas de referencias, smoke de hosts. Evidencia en el informe de base ampliada; revisión humana y consolidación Git/CI no se dan por realizadas.
