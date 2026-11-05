# PLAN — nv-14

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir CI/CD, dos réplicas, TLS, CDN/observabilidad, ciclo crear/desplegar/eliminar y manifiesto; cerrar paquete académico sin inventar costo cero.

Dependencias: Preparación desde nv-00..04; cierre requiere nv-04..13 y decisiones D03..D08; nv-15 opcional.

## 2. Diseño técnico

Manifiesto por artefacto, contrato, evento y schemaVersion; scripts DB idempotentes, índices/restauración; cuentas, costos, llaves y responsables fuera del documento público. Landing es componente separado.

Autoridad: [contratos](../../../docs/contracts/README.md). [Acuerdo API–app](../../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. Reproducir instalación Play y compatibilidad API, logs sin PII, atención por dos réplicas, rollback, .rar limpio y scripts DB; métricas GA4 sin datos personales y QR final decodificado.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Expediente reúne dos réplicas/TLS/Redis/CDN/observabilidad, crear/desplegar/eliminar con entorno verificado, restauración y .rar sin secretos; Play/GA4/QR son evidencia de contraparte, no tareas de una API.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| I03 Observabilidad | FR-612 → T-612 → AC-612 | Delimitar I03 con OTLP/exportador, dependencias Todos los componentes y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |
| I04 Infraestructura y entrega | FR-613 → T-613 → AC-613 | Delimitar I04 con CLI e identidades federadas, dependencias Azure/GitHub/Play y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |
| R08 CI/CD | FR-408 → T-408 → AC-408 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R13 Nube y balanceo | FR-413 → T-413 → AC-413 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R15 Backend operativo | FR-415 → T-415 → AC-415 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Rutas, propietario y orden verificable 0.4.1

Dueño: Entrega y operacion de propietarios activos. Rutas objetivo: `deploy/, .github/workflows/, docs/operations/ y suites de cada servicio` (no creadas por este plan). Alcance propio: preparar desde fundamentos; destinos reales y dos replicas se verifican al desplegar. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Perfiles de arquitectura aplicables

Consultar [índice de propietarios](../../../docs/arquitectura/README.md) y las [políticas comunes](../../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
