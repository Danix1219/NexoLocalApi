# PLAN — nv-03

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir gates backend por riesgo: unit, integración, seguridad, datos NoSQL, contrato, análisis y evidencia; pipeline futuro con rutas verificadas.

Dependencias: nv-02.

## 2. Diseño técnico

API: evidencia por suite y propietario. App: modelos desde contrato cuando se autoricen generadores, cursores/versiones y estado de sincronización; no DTO manual divergente.

Autoridad: [contratos](../../../docs/contracts/README.md). [Acuerdo API–app](../../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. Clasificar 401/403, conflicto 409, validación 422, límite 429 y dependencia caída; confirmar semántica exacta en contrato antes de generar consumidores.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Cada gate indica propósito, entrada, herramienta por ratificar, alcance, salida/exit y responsable; 45%/80% son propuestas; mock, integración real y prueba de réplica no se confunden.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| R07 Pruebas | FR-407 → T-407 → AC-407 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Rutas, propietario y orden verificable 0.4.1

Dueño: Calidad por proyecto habilitado. Rutas objetivo: `.github/workflows/, .ci/, scripts/ y tests/ del propietario` (no creadas por este plan). Alcance propio: transicion de CI documental a CI de producto y dependencias de arquitectura verificables. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

T-951 / FR-951 / AC-951: Sustituir el bloqueo documental por CI real de producto. En el mismo PR que incorpora el primer csproj, conservar SDD/gobierno y agregar restore/build/test, contrato/seguridad y evidencia segun riesgo. Reemplazar el rechazo incondicional de manifiesto por una transicion explicita que exija los gates reales; no simplemente eliminarlo. Coordinar scaffold nv-00 sin ciclo de prerrequisitos. Orden local: T-002, T-950 → T-951 → T-003. Comprobacion esperada: Con proyecto existente, CI ejecuta comandos de rutas reales y falla al romper build/test o arquitectura; lint no puede ser el unico gate verde. Registrar resultados/limites sin afirmar ejecuciones aun pendientes.

## Evidencia parcial de scaffold autorizado (2026-11-03)

Estado corregido por indicacion del usuario: la solucion original nexolocal.Api.slnx contiene el proyecto original como G01 Gateway y cuatro proyectos S01 en services/identity/src. Dos hosts separados, cinco proyectos en total, sin logica de negocio. [Archivos y validacion real vigente](../../../docs/inicio-backend/correccion-base-microservicios.md). La propuesta paralela anterior fue retirada; no es la estructura activa.

La tarea propietaria de consolidacion y las de CI/referencias NO se marcan completas: codigo aun fuera del checkout Git, ensayo de imagenes Docker pendiente; los Dockerfiles de ambos hosts estan preparados, Ready/ratificacion y revision pertinentes pendientes; workflow de producto preparado sin ejecucion remota. El guard documental actual se mantiene; al consolidar csproj al checkout, la transicion a CI real debe ir en ese cambio. Esta evidencia parcial no ratifica proveedores ni contratos.

## Correccion solicitada: solucion original y microservicios

El usuario confirma G01 en el proyecto nexolocal.Api ya existente, y hosts propios para microservicios. La solucion unica sigue nexolocal.Api.slnx; el nombre paralelo anterior se retira. S01 usa services/identity/src/{Domain,Application,Infrastructure,Api}; no referencias de proyecto desde G01 a negocio S01. [Evidencia vigente](../../../docs/inicio-backend/correccion-base-microservicios.md) sustituye la organizacion del scaffold anterior. Las tareas de consolidacion/revision y ejecucion remota no se marcan completas por compilar localmente.

## Perfiles de arquitectura aplicables

Consultar [índice de propietarios](../../../docs/arquitectura/README.md) y las [políticas comunes](../../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
