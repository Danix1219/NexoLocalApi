# PLAN — nv-00

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Gobierno de fuentes, decisiones, responsables y versiones previas al desarrollo; Codex y Claude Code con traspaso secuencial.

Dependencias: Ninguna; inicia el gobierno.

## 2. Diseño técnico

Sin datos de negocio: registrar fuente/versión, requisito, decisión y evidencia; ninguna firma automática.

Autoridad: [contratos](../../docs/contracts/README.md). [Acuerdo API–app](../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. Reconstruir una tarea documental desde fuente hasta checklist y distinguir propuesta de ratificación.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Ninguna decisión propuesta ni excepción puede figurar aceptada sin nombre/fecha reales; un cambio de alcance enlaza la spec y su impacto en contratos y app.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| R02 Evaluación interactiva y con prompts | FR-402 → T-402 → AC-402 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R05 SDD y DevSecOps | FR-405 → T-405 → AC-405 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R06 Desarrollo con agentes | FR-406 → T-406 → AC-406 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R11 Matriz de versiones | FR-411 → T-411 → AC-411 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R12 MCP opcional | FR-412 → T-412 → AC-412 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R18 GitHub/coordinación | FR-418 → T-418 → AC-418 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| PRE1 Entrevistas | FR-451 → T-451 → AC-451 | Fuente: lineamientos Móvil v2, página 4. Artefacto común no se duplica como dos entrevistas o dos hojas incompatibles. |
| PRE2 Descripciones separadas | FR-452 → T-452 → AC-452 | Fuente: lineamientos Móvil v2, página 4. Artefacto común no se duplica como dos entrevistas o dos hojas incompatibles. |
| PRE3 Componentes en hoja de cálculo | FR-453 → T-453 → AC-453 | Fuente: lineamientos Móvil v2, página 4. Artefacto común no se duplica como dos entrevistas o dos hojas incompatibles. |
| PRE4 CRUD y funciones por endpoint | FR-454 → T-454 → AC-454 | Fuente: lineamientos Móvil v2, página 4. Artefacto común no se duplica como dos entrevistas o dos hojas incompatibles. |
| PRE5 Referencias docentes | FR-455 → T-455 → AC-455 | Fuente: lineamientos Móvil v2, página 4. Artefacto común no se duplica como dos entrevistas o dos hojas incompatibles. |
| PRE6 Estructura SDD | FR-456 → T-456 → AC-456 | Fuente: lineamientos Móvil v2, página 4. Artefacto común no se duplica como dos entrevistas o dos hojas incompatibles. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Rutas, propietario y orden verificable 0.4.1

Dueño: Gobierno. Rutas objetivo: `specs/, docs/decisions/, .github/ y .specify/` (no creadas por este plan). Alcance propio: responsables, decisiones, revisiones y trazabilidad de cambios; sin mover codigo. Leer [mapa de carpetas](../../docs/estructura-backend.md) y [secuencia SDD](../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

T-951 / FR-951 / AC-951: Activar gobierno de cambios con datos reales. Completar tarea Jira, pagina Confluence y politica real, publicar por develop→release→main y verificar check de metadatos antes de exigirlo en seis rulesets. Mientras el usuario posponga esos datos, registrar pendiente de publicacion sin bloquear el analisis local ni inventar enlaces. Orden local: T-002, T-950 → T-951 → T-003. Comprobacion esperada: Evidencia de PR/CI y lectura remota demuestra la activacion; antes de ello el estado permanece preparado, no activo.

## Paquete ejecutable de preparacion documental nv-00 (0.4.2)

Entrada principal: [inicio nv-00](../../docs/inicio-backend/README.md). Archivos preparados actuales y de mantenimiento: docs/decisions/registro.md, docs/inicio-backend/README.md, flujo-cambios.md, evidencia-nv00.md, guion-evidencias.md e inventario-fuentes.json en esa carpeta; cuatro piezas de nv-00 y Memoria. No hay cambios de csproj o Docker en esta seleccion.

T-006 contrasta alcance/fuentes y decide que falta; T-020 usa registro y flujo para diferenciar propuesta/ratificacion/excepcion; T-950 concreta estas rutas y la salida para fundacion. T-001 requiere revision humana real antes de Ready; contratos HTTP propios son N/A justificado solo para nv-00. Los documentos se pueden refinar antes de Ready y permanecen borradores.

T-002/T-010 consolidan documentos de la seleccion cuando corresponda; tareas de obligaciones contrastan su evidencia por separado. T-003 conserva espera de activacion para el cierre GLOBAL, no impide el refinamiento documental o entregar entradas parciales no aceptadas a nv-01/nv-02. T-004 revisa privacidad/secretos y T-007/T-005 concilian evidencia/limites. T-951 no se ejecuta mientras el usuario posponga datos reales.

Validacion local: lint, integridad y ocho pruebas del verificador desde la raiz segun README de arranque. Inventario de fuentes registra presencia/hash y alcance de lectura, no respuestas de entrevista ni revision del contenido binario. Resultado real se incorpora a evidencia/Memoria; no convertir PASS en ratificacion.

## Perfiles de arquitectura aplicables

Consultar [índice de propietarios](../../docs/arquitectura/README.md) y las [políticas comunes](../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
