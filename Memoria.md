# Memoria — API_NexoLocal

## Fase documental — adaptación al profesor (2026-11-02)
Estado: estructura y borradores documentales creados; sin implementación, compilación, suites de pruebas, CI o despliegue. El lint documental se registra por separado después de ejecutarlo.
Decisiones: adoptar el formato de API_SDD: specs/arquitectura/nv-XX/spec.md → plan.md → task.md → checklists/gates.md. Mantener requisitos Nexo, Flutter/microservicios/NoSQL y fuente única de contratos. Revisores por asignar.
Deuda: formalizar contratos del primer incremento, ratificar stack/costo/proveedores y convertir planes en rutas exactas cuando se creen proyectos. Ningún [x] o firma importado del profesor.
A evitar: confundir formato docente con obligación de SQL Server/Razor; llamar Borrador con evidencia a una fase sin pruebas de producto; volver a generar sdd/specs antiguo como si fuera el flujo activo.

## Verificación documental de la adaptación (2026-11-02)
Estado: `powershell -NoProfile -File scripts/check_sdd.ps1` devolvió exit 0 y SDD-OK: 16 specs; siete secciones, aprobación, tareas FR/NFR, enlaces relativos y 101 operaciones únicas con spec viva. No se ejecutaron build, suites ni CI de producto.
Decisiones: conservar `task.md` y `checklists/gates.md`, constitución única en `.specify/memory/` con enlace raíz. Plantilla PR documental preparada, sin remoto ni workflows creados.
Deuda: todas las tareas de producto y firmas permanecen pendientes. Asignación de EP a una fase es diseño, no contrato formal aprobado.
A evitar: interpretar SDD-OK como API implementada o evidencia de cumplimiento de todos los requisitos académicos.

## Reorganización de ubicación (2026-11-02)
Estado: traslado solicitado a `C:/Users/kevin/Downloads/API_NexoLocal`, fuera de requerimientos_nexolocal. Enlaces recalculados; validación posterior al traslado pendiente.
Decisiones: conservar ambos contextos como carpetas hermanas. Documentación funcional e histórica permanece en requerimientos_nexolocal.
Deuda: esto no crea repositorios Git ni implementación.
A evitar: ejecutar generadores antiguos que vuelvan a crear los proyectos dentro de la carpeta de requisitos.

## Redefinición SDD y coordinación API–app (2026-11-02)
Estado: documentación 0.2.0 refinada con fuentes de docs_nexo; planes, tareas, criterios y checklists por fase. Sin implementación ni aceptación de producto; validación final se registra por separado.
Decisiones: Codex y Claude Code sustituyen OpenCode como herramientas elegidas; entradas AGENTS/CLAUDE y traspaso secuencial. Autoridad de contratos API, consumo móvil; conservar las dos secuencias del profesor. Acuerdos comunes y cobertura en docs_nexo.
Deuda: ratificar decisiones D01–D08 pertinentes, asignar revisores humanos, formalizar contratos de la primera historia y concretar archivos/comandos de producto cuando se encargue. Skills investigadas y recomendadas, no instaladas.
A evitar: repetir responsabilidades del servidor como implementación Flutter, dar por Ready una fase por lint, afirmar costo cero/proveedores/SDK aprobados o marcar tareas sin evidencia real.

## Validación final de redefinición (2026-11-02)
Estado: validador local scripts/check_sdd.ps1 ejecutado con exit 0: 16 specs API y 15 móviles. Revisión adicional: 89 Markdown API, 80 app y 5 acuerdos comunes, sin fallos de enlaces/IDs/versiones; 101 EP coinciden entre inventario, Markdown fuente y fichas del XLSX de 116 hojas. Informe común: ../docs_nexo/SDD_VALIDACION.md.
Decisiones: registrar PASS solo documental. La revisión adicional fue una comprobación de sesión, no un script nuevo de producto. Las fuentes originales de docs_nexo se conservaron.
Deuda: revisión humana, decisiones D01–D08 pertinentes, contrato formal y pruebas de producto sin ejecutar. El resultado no concede Ready ni aceptación.
A evitar: presentar igualdad de IDs como compatibilidad completa de payloads o usar validación documental como prueba de APIs, Flutter, Android o nube.

## Aplicación constitucional e integridad estructural 0.3.0 (2026-11-02)
Estado: especificaciones, planes, tareas y checklists conciliados con constitución y mapas locales. Se detectó que T-006/T-007 estaban citadas pero ausentes; se corrigió y se añadieron tareas verificables, tabla FR/NFR→T→AC y AC-100 específico por propietario. Sin código de aplicación ni pruebas de producto.
Decisiones: API conserva 16 specs y concentra transversales en sus propietarios; app tiene 16 specs, incluida 016-sincronizacion-local para offline/cola/migraciones/conflictos/cambio de cuenta ya exigidos en fuente. Entrega móvil depende de esta aceptación. Codex/Claude secuenciales; no skills ni agentes instalados.
Deuda: D01–D08 pertinentes, responsable/revisor humano, contratos formales y rutas/comandos reales siguen pendientes antes de Ready/ejecución. Validación docente no cubre integridad semántica completa.
A evitar: citar tareas inexistentes, confundir cobertura de diseño con prueba ejecutada, duplicar autoridad de contratos o cargar funciones del servidor a Flutter. Informe vigente: ../docs_nexo/SDD_CIERRE_ESTRUCTURA.md; informe 0.2.0 conserva corrección explícita.

Comprobación adicional de sesión: 16 specs API con 172 tareas/140 requisitos/80 AC y 16 móviles con 174 tareas/142 requisitos/84 AC; cero fallos de enlaces, IDs citados, cobertura y ciclos locales en revisión API/copia móvil. No se añadió código de validador. Ejecución final del lint móvil y aplicación de copia se registra al aplicarla.

Verificación final tras aplicar: ambos scripts/check_sdd.ps1 devolvieron exit 0 y SDD-OK con 16 specs cada uno. Revisión adicional sobre carpetas finales: 0 fallos de enlaces/IDs/cobertura/ciclos locales; documentos móviles coinciden con copia revisada salvo registro real de Memoria. Informe docs_nexo/SDD_CIERRE_ESTRUCTURA.md. Sin build/tests/producto, firmas o Ready automáticos.

## Cobertura funcional y endpoints 0.4.0 (2026-11-02)
Estado: desglose documentado de 26 specs API y 25 móviles. Cada una de las 101 operaciones tiene FR/tarea/AC en spec primaria API; 92 tienen historia de consumo móvil y 9 exclusión directa interna/admin/infra. M01–M27, 13 componentes backend, W01, los 19 puntos académicos, seis tareas previas y H01–H12 tienen tarea/criterio propios por superficie pertinente. No producto implementado.
Decisiones: conservar IDs/métodos/rutas/propietarios y reasignar solo spec primaria para separar recuperación, catálogo, guardados/métricas, experiencias, interesados, notificaciones, datos/recuperación, expediente/landing, administración y consentimiento. Fases origen integran subcapacidades, sin volver a construir sus EP. No nuevo microservicio, endpoint ni pantalla inferida.
Deuda: esquemas exactos/compatibilidad/tipos, decisiones D01–D08, datos/cuentas/proveedores y revisión humana siguen pendientes antes de Ready/aceptación; opcionales/complementos se ratifican y no se presentan como obligaciones aprobadas.
A evitar: contar specs como prueba de cobertura semántica, equiparar documento a implementación, invocar internas/admin desde Flutter o duplicar casos de uso de spec primaria. Mapa vigente: ../docs_nexo/SDD_COBERTURA_FUNCIONAL.md; las asignaciones 0.3 son históricas.

Prevalidación adicional API/copia móvil: cero fallos de enlaces, referencias/duplicados, tareas, cobertura FR/NFR/AC y ciclos locales. Asignación única contrastada para 101 EP, 92 consumos y fuentes funcionales/académicas; 101 métodos/rutas coinciden con fuente 1.2.0. Aplicación y lint final se registran por separado.

Validacion local final 0.4.0: scripts/check_sdd.ps1, exit 0. SDD-OK: 26 specs; estructura/trazabilidad/enlaces documentales. Sin validacion de producto. Solo documental, sin producto ejecutado.

Cierre verificado 0.4.0: 26 specs API y 25 móviles; ambos scripts/check_sdd.ps1 exit 0. Revisión adicional final: 0 fallos; 101 operaciones primarias API, 92 consumos y 9 exclusiones móviles, 27 módulos/13 componentes/W01/19 puntos/seis previas/H01–H12 con asignación. Copia móvil aplicada coincide con revisión. Informe docs_nexo/SDD_COBERTURA_FUNCIONAL.md. FormalContract=null en las 101; producto, revisión humana y decisiones pertinentes pendientes.

## Preparación de repositorios y CI documental (2026-11-02)
Estado: destinos públicos NexoLocalApi/NexoLocalApp verificados vacíos. CI documental preparado y probado desde copias aisladas: 26 specs API y 25 app, lint/IDs/cobertura PASS. Contextos externos congelados y hasheados evitan depender del disco local o acceso privado a otro repo. No build/test de producto ni CD.
Decisiones: raíces iniciales API_NexoLocal y App_NexoLocal con SDD y futuro código; plantilla .NET separada no se mueve. Workflow SDD / docs con acción oficial fijada a SHA, contents:read, sin credenciales de despliegue; bloquea manifiesto de producto hasta añadir CI real. Dependabot solo para Actions; ruleset de PR/revisión/check listo para aplicar tras primera ejecución remota.
Deuda: revisión humana adicional, Ready/contratos de identidad, stack/SDK, raíz de implementación final y nube/Play; publicación y ejecución remota se informan por separado, no inferidas por validar local.
A evitar: aprobar producto con lint, almacenar credenciales, añadir CD ficticio o describir snapshots documentales como HEAD vivo de la contraparte.

## Estado de configuracion de repositorios (2026-11-02)
Estado: Git local main/origin preparado, sin commit/push. Actions remoto configurado y verificado: SHA obligatorio, acciones oficiales seleccionadas, token de lectura y sin aprobacion automatica de PR; squash merge y borrado de rama tras merge. CI documental probado en copias aisladas (26 API/25 app), mas prueba negativa que rechaza manifiesto de producto sin CI real.
Decisiones: publicar solo tras autorizacion explicita del payload publico revisable. Revision automatica rechazo exponer SDD/Memoria/instrucciones/referencias sin esa autorizacion; no se sorteo el rechazo. Inventario: docs_nexo/PUBLICACION_INICIAL_REVISABLE.md.
Deuda: autorizacion publica, primer CI remoto y ruleset, persona revisora distinta del autor, Ready/contratos/stack de identidad y nube/Play para CD. CI documental no equivale a DevSecOps ni CD completos.
A evitar: anunciar push, pipeline remoto o rama protegida sin evidencia; enviar credenciales o confundir configuracion preparada con producto desarrollado.

## Autorizacion de publicacion y trabajo individual (2026-11-02)
Estado: el usuario autorizo explicitamente publicar el contenido revisable completo en ambos repositorios publicos con la identidad Git configurada. Se levanta el pendiente de autorizacion registrado anteriormente; commit/push y CI se verifican despues.
Decisiones: Danix1219 sera el unico responsable por ahora. Exigir PR y CI, con cero aprobaciones obligatorias para permitir trabajo individual sin fingir revision independiente; sin bypass de checks.
Deuda: incorporar revisor independiente y aumentar aprobaciones a uno cuando exista; contratos/Ready/stack y CD siguen pendientes.
A evitar: interpretar la autorizacion de publicacion como aceptacion funcional o revision independiente.

## Publicacion y CI remoto verificados (2026-11-02)
Estado: contenido autorizado publicado en https://github.com/Danix1219/NexoLocalApi; primer workflow SDD documentation y job SDD / docs concluyeron success: https://github.com/Danix1219/NexoLocalApi/actions/runs/41548573146. Upstream local/remoto 0/0 verificado despues del push inicial.
Decisiones: proteger main con PR, conversaciones resueltas y SDD / docs emitido por GitHub Actions (integration_id 15368), rama actualizada y sin force-push/borrado; cero aprobaciones por trabajo individual. La activacion se ejecuta tras verificar CI del commit que registra esta evidencia; resultado administrativo final en docs_nexo/INICIO_DESARROLLO_Y_REPOS.md.
Deuda: no existe revision independiente; Ready, contratos formales/stack y CI/CD de producto pendientes. No se han ejecutado builds, pruebas funcionales ni despliegues.
A evitar: confundir CI documental exitoso con aceptacion funcional, seguridad completa o CD.

## Flujo de equipo acordado (2026-11-02)
Estado: el usuario aclara que incorporara equipo y acuerda feature/*→develop→release/*→main. develop creada desde main; configuracion se entrega por PR, no push directo a ramas compartidas. Resultado final de PR/CI/rulesets se registra en docs_nexo/FLUJO_GIT_EQUIPO.md.
Decisiones: main principal; ramas por tarea/spec, candidato por entrega, estabilizacion fix→release y reintegracion main/release→develop. Checks SDD / docs y Git / flow, PR y conversaciones resueltas; merge commit en promociones, sin force-push/bypass. Dependabot a develop; borrado automatico deshabilitado para no perder ramas antes de reintegrar.
Deuda: solo Danix1219 figura hoy como colaborador; al aceptar el primer revisor Write, subir aprobaciones de 0 a 1 en los tres rulesets de ambos repositorios antes del trabajo de equipo. CD/CI de producto y Ready permanecen pendientes.
A evitar: PR feature/develop directo a main, funcionalidades nuevas en release, olvidar reintegracion, borrar develop o presentar la fase transitoria como revision independiente. El acuerdo anterior de trabajo individual queda sustituido por este flujo de equipo.

## Preparacion Jira/Confluence (2026-11-02)
Estado: plantillas PR, convencion tipo/CLAVE-JIRA-descripcion, validador de metadatos y pruebas preparados en ambos repositorios en rama temporal local. Datos Jira/Confluence reales solicitados y aun pendientes; sin publicar ni activar reglas remotas nuevas. Resultado de validacion local se registra en docs_nexo/TRAZABILIDAD_JIRA_CONFLUENCE.md.
Decisiones: enlace Jira y pagina Confluence obligatorios tambien para bots/promociones/reintegraciones; titulo, clave principal y rama de trabajo coinciden. Workflow usa codigo de main de confianza y no consulta servicios ni ejecuta codigo del PR. No instalar ni afirmar integracion Jira automatica.
Deuda: clave/proyecto Jira, bases HTTPS, tarea/pagina reales para este cambio; renombrar rama temporal con tarea real, publicar por flujo aprobado, verificar check real y anadirlo a los seis rulesets. CI documental y aceptacion de producto siguen siendo distintos.
A evitar: enlazar portadas como documentacion, aceptar enlaces solo ocultos/comentarios, inventar tickets o publicar/activar un gate sin configuracion real.

## Estructura de arranque backend (2026-11-02)
Estado: inspeccionada solucion Visual Studio nexolocal.Api, proyecto net10.0 con WeatherForecast y Dockerfile; SDK locales 9.0.316/10.0.302. Mapa concreto docs/estructura-backend.md y README de enlace en la plantilla. No se mueven archivos, crean proyectos/paquetes o ejecutan builds/tests de producto.
Decisiones: objetivo de un solo checkout API con SDD y codigo; raiz Git existente se conserva. G01/S01-S08 por dominio; propuesta interna Domain/Application/Infrastructure/Api, solo S01 inicial y gateway segun vertical. Orden nv-00..03→nv-04, otras specs segun dependencias y transversales progresivos.
Deuda: ratificar stack/proveedores y cerrar contrato/Ready; consolidar plantilla con actualizacion de solucion/Docker y CI real en el mismo cambio. Jira/Confluence pospuestos por usuario, sin publicacion ni controles remotos nuevos.
A evitar: iniciar otro repo, un controller generico para todos los dominios, generar ocho servicios vacios, referencias/bases cruzadas o usar lint como prueba de producto.

Verificacion de esta fase: scripts/check_sdd.ps1 exit 0, 26 specs; git diff --check correcto y enlace README de la plantilla al mapa comprobado. No build/test de producto ni publicacion. Las modificaciones Jira/Confluence previas se conservaron sin activar.

## Integracion del mapa backend en el flujo SDD 0.4.1
Estado: 26 specs con spec/plan/task/checklist conciliados; 30 tareas nuevas (474 total), 56 requisitos nuevos (448 total) y 30 criterios nuevos (312 total). Tarea estructural previa a Ready en cada spec; traslado/referencias con dueño nv-01, transicion CI nv-03 y activacion Jira/Confluence nv-00. Ninguna casilla ejecutada ni estado Ready/aprobado. Informe docs_nexo/SDD_INTEGRACION_ESTRUCTURA_BACKEND.md.
Decisiones: conservar un solo checkout backend y primeras rutas por historia/propietario; dependencias entre fases se satisfacen con entradas concretas, no cerrando todo un dominio. Plantillas, constitucion documental 0.3.1 y guias integran el orden. Catalogo de 101 operaciones sin cambios; app y snapshots documentales mantienen revision propia.
Verificacion: lint 26 specs e integridad IDs/cobertura/ciclos locales/Ready correctos; ocho pruebas positivas/negativas OK. Wrapper CI documental completo PASS en copia aislada; no extraccion sobre repositorios vivos. Diff check correcto. No build/test de producto ni CI remoto nuevo.
Deuda: decisiones/contratos/revisor, scaffold/CI real en primer PR ejecutable; Jira/Confluence pospuestos por usuario, cambios sin publicar ni nuevas protecciones remotas.
A evitar: declarar cerradas brechas semanticas o producto por lint, ciclos Ready→construccion→Ready, duplicar scaffold en cada spec o borrar el bloqueo documental sin gates reales.

## Paquete de arranque nv-00 (2026-11-03)
Estado: primera spec nv-00 concretada en docs/inicio-backend/README.md, flujo de cambios/excepciones/traspaso, guion de evidencia, registro D01-D08 y matriz por tarea. nv-00 0.4.2; demas API 0.4.1. No nuevas tareas/producto ni casillas marcadas. Danix1219 registrado como revisor provisional designado anteriormente por el usuario, revision aun pendiente y sin independencia atribuida.
Decisiones: seleccion local de gobierno diferenciada de Ready formal/cierre global; no contrato HTTP propio en nv-00. Preparacion local no exige nube/SMS/producto ni inventar Jira/Confluence; construccion posterior requiere sus entradas/ratificaciones reales. Las obligaciones academicas no quedan aceptadas por preparar guiones.
Verificacion: lint e integridad 26 specs PASS (474 tareas/448 requisitos/312 criterios), ocho tests OK, wrapper completo en copia aislada PASS. Nueve fuentes presentes/hash coincidente, limites de lectura explicitos. Recorrido documental FR-100/T-020/AC-100 comprobado y diff check correcto. Sin build/test de producto ni refresco/publicacion remota.
Deuda: revision real y decisiones tecnicas pertinentes antes de Ready/scaffold, entrevistas/evidencias/referencias docentes y responsables para cierre, datos Jira/Confluence pospuestos. Plantilla Visual Studio sin mover ni modificar por este paquete.
A evitar: pedir de nuevo quien revisara cuando Danix1219 ya fue designado, firmar en su nombre, usar un hash como revision de contenido, tratar todo D01-D08 como bloqueo del analisis o convertir un subset en cierre total de la spec.

## Limpieza de herramientas IA (2026-11-03)
Estado: retirada la carpeta .opencode del backend por encargo explicito; referencias activas de README/AGENTS/CLAUDE/mapa/constitucion/nv-00 alineadas con Codex y Claude Code. Guia local docs/herramientas-ia.md.
Decisiones: Codex usa AGENTS como entrada comun; CLAUDE remite a esa autoridad. No copiar perfiles ajenos ni generar carpetas de configuracion ficticias. Los registros historicos y snapshots congelados conservan su revision original.
Deuda: carga efectiva en cada cliente y skills se verifican cuando se usen; Jira/Confluence y publicacion siguen pospuestos.
A evitar: reglas duplicadas, atribuir herramientas/agentes instalados por existir un README o reescribir la historia de publicacion para borrar referencias docentes.

Verificacion de limpieza IA: lint 26 specs e integridad (474 tareas/448 requisitos/312 criterios) PASS; diff check correcto. Inventario de fuentes conserva hash anterior/motivo para la constitucion actualizada. Solo cambios locales, sin publicacion.

## Scaffold fisico autorizado backend (2026-11-03)
Estado: el usuario confirma "Crear carpetas y proyectos base, sin logica de negocio". Se crea NexoLocal.Backend.slnx en source/repos/nexolocal.Api con cuatro proyectos net10.0 Identity: Domain, Application, Infrastructure y Api, SDK 10.0.302 fijado, entradas AGENTS/CLAUDE, README, normas de build, verificador y workflow local de CI. Plantilla/solucion WeatherForecast originales conservadas con hashes iguales. Evidencia docs/inicio-backend/base-fisica.md.
Decisiones: codigo fisico en ubicacion solicitada, sin segundo .git; checkout Git SDD sigue separado hasta consolidacion revisada. Solo host vacio, sin entidades/endpoints/paquetes de aplicacion ni proveedores. No crear gateway/S02-S08 vacios, negocio o Docker sin su incremento. Encargo tecnico no equivale a Ready funcional o ratificacion total D01.
Verificacion: restore con feed local vacio/packs instalados PASS, build Release 0 errores/0 advertencias, arquitectura PASS y siete pruebas del verificador OK. Host inicia por loopback, GET / devuelve 404 esperado y se detiene. No tests de negocio/DB/SMS, Docker o CI remoto ejecutados.
Deuda: consolidar codigo/solucion/CI en checkout y actualizar Docker/transicion de guard en un PR posterior; Jira/Confluence pospuestos, sin publicacion/rulesets nuevos. Contratos, ratificaciones y revision funcional siguen pendientes.
A evitar: ejecutar solucion WeatherForecast para validar la base nueva, llamar producto al host vacio, confundir pruebas del verificador con pruebas de negocio o copiar .git al directorio de implementacion.

## Correccion de estructura por indicacion del usuario (2026-11-03)
Estado: el usuario rechaza la solucion paralela y confirma G01 en el proyecto original nexolocal.Api, con hosts propios por microservicio. Se retira NexoLocal.Backend.slnx; unica solucion nexolocal.Api.slnx con G01 existente y S01 separado en services/identity/src/{Domain,Application,Infrastructure,Api}. Se corrigen rutas en las 26 specs/planes y guia actual. Informe docs/inicio-backend/correccion-base-microservicios.md; informe previo rotulado historico.
Decisiones: mantener nombre/ruta del proyecto original, agrupado como Gateway. El IDE bloqueo el traslado de su carpeta; no se cerro ni se creo otro host para sortearlo. Se retiraron WeatherForecast/paquetes de ejemplo sin uso, conservando copias textuales; Dockerfiles independientes y CI apuntan a la solucion original. No instalar YARP/proveedores ni mezclar negocio de servicios en G01.
Verificacion: restore cinco proyectos con feed local vacio PASS; build Release 0 errores/0 advertencias. Grafo de proyectos PASS y ocho pruebas del verificador OK, incluida prohibicion Gateway→Identity.Application. Publish local independiente de ambos hosts, sin DLL Identity en G01; dos hosts simultaneos responden 404 esperado y procesos propios detenidos. No proxy, negocio, imagenes Docker o pipeline remoto ensayados.
Deuda: contratos/proveedores/Ready funcional y prueba Docker, consolidacion codigo/SDD y transicion de CI/guard en PR; Jira/Confluence pospuestos. Ninguna tarea se marca aprobada o cerrada automaticamente.
A evitar: crear otra solucion cuando el usuario ya tiene una, sustituir microservicios por capas globales, llamar proxy funcionando a un host vacio, recrear todos los servicios sin historias o conservar rutas de la solucion descartada como instrucciones activas.

## Perfiles de arquitectura por propietario (2026-11-03)
Estado: definidos docs/arquitectura/README.md, políticas comunes, nueve perfiles G01/S01–S08 y registro perfiles.json. Cada perfil concreta carpetas, patrones/condiciones, persistencia, puertos, integración, operaciones existentes y comprobaciones. Nueve specs propietarias incorporan FR-960/NFR-960/AC-960/T-960 antes de Ready; las restantes enlazan los perfiles pertinentes. No se crea código ni se instalan dependencias en este incremento.
Decisiones: G01 conserva nexolocal.Api; servicios con hosts y datos propios. Puertos/adaptadores y casos por funcionalidad; DDD según complejidad, sin exigir frameworks. S04 máquina de estados/concurrencia/outbox; S05 historial durable/deduplicación; S07 worker/reintentos; S06 decisión vinculada al contenido; S08 proyección condicional. Patrones de coordinación y almacenamiento quedan sujetos a capacidad real, contrato y decisiones bloqueantes.
Verificación: lint 26 specs e integridad 483 tareas/466 requisitos/321 criterios PASS; ocho pruebas del validador PASS. Registro de nueve perfiles cubre exactamente las 101 operaciones del inventario, incluyendo dos transversales, sin reasignaciones ni duplicados. Sin pruebas de producto en este incremento.
Deuda: contratos formales, decisiones técnicas concretas y revisión humana por historia; solo G01/S01 tienen base física. Proveedores, Docker ejecutado, consolidación Git y CI remoto siguen pendientes. Jira/Confluence pospuestos; sin publicación nueva.
A evitar: crear todos los servicios vacíos, patrones sin problema concreto, compartir dominio/datos, reintentar escrituras sin idempotencia o interpretar diseño como implementación aprobada.

## Base física ampliada de todos los microservicios (2026-11-03)
Estado: por encargo explícito posterior se amplía el scaffold a S01–S08. Solución única nexolocal.Api.slnx, G01 original conservado; 33 proyectos, nueve hosts HTTP y Worker S07 separado. S01–S07 con cuatro capas; S08 con Application/Infrastructure/Api, sin Domain artificial. Carpetas por capacidad/puertos/adaptadores, README y tests reservados. Informe docs/inicio-backend/base-todos-microservicios.md; inventario físico scaffold.json. Spec nv-01 incorpora FR-961/AC-961/T-961; ningún cierre automático.
Decisiones: el nuevo encargo permite crear todas las bases antes de las historias funcionales, reemplazando el límite previo de G01/S01. Sin clases de negocio, endpoints, proveedores o PackageReference nuevos; Worker usa hosting del framework compartido existente. S08 se compila pero no se habilita en G01. Patrones tienen ubicación definida, no comportamiento implementado. No crear otra solución ni repositorio.
Verificación: restore local de 33 proyectos PASS; build Release 0 errores/0 advertencias; arquitectura PASS y once pruebas del verificador OK. Smoke simultáneo: nueve hosts responden 404 esperado, Worker activo y procesos propios detenidos. Diez Dockerfiles con orígenes COPY existentes; imágenes no ensayadas. Lint 26 specs e integridad 484 tareas/467 requisitos/322 criterios PASS; ocho pruebas del validador OK.
Deuda: contratos/decisiones/Ready/revisión por historia, implementaciones y tests funcionales de patrones, DB/eventos/proveedores, build de imágenes, consolidación Git/SDD y CI remoto. Jira/Confluence siguen pospuestos; no commits ni publicación nueva en esta fase.
A evitar: confundir scaffolding con microservicios funcionales, afirmar entrega exactamente una vez, usar caché como autoridad de revocación, activar S08 por disponer de proyecto o añadir dependencias entre propietarios.

## Revisión de arquitectura según casos de uso (2026-11-03)
Estado: el usuario solicita revisar y aplicar arquitectura interna por responsabilidad. Se reduce la solución original de 33 a 28 proyectos; diez hosts se conservan. Perfiles/registro/planes y guías se concilian. Decisión docs/arquitectura/seleccion-arquitecturas.md; evidencia docs/inicio-backend/revision-arquitecturas.md. No se agrega lógica de negocio ni proveedores.
Decisiones: S01 hexagonal con reglas de sesión; S02 slices Features; S03 módulos funcionales Modules; S04 DDD/hexagonal para transiciones y asignación; S05 Core durable y adaptadores tiempo real; S06 Evaluation/pipeline y estrategias; S07 Delivery compartido por API/Worker, orientado a eventos; S08 Projections reconstruibles, condicional. Adapters por dueño y núcleos sin dependencias técnicas. Políticas comunes no obligan a cuatro capas. Eficiencia requiere mediciones; no se declara mayor velocidad por renombrar o reducir proyectos.
Verificación: restore local 28 proyectos PASS; build Release 0 errores/0 advertencias; grafo PASS, catorce pruebas del verificador PASS. Smoke nueve HTTP con 404 esperado y Worker activo; procesos propios detenidos. Diez Dockerfiles revisados estáticamente sin build; perfiles físicos/SDD concuerdan y 101 operaciones se conservan. Lint 26 specs/integridad 484 tareas/467 requisitos/322 criterios PASS y ocho pruebas del validador PASS.
Deuda: implementación y pruebas de reglas/patrones, límites internos entre módulos S03, contratos/Ready/revisión, medición de rendimiento, proveedores/DB/broker, imágenes, consolidación Git y CI remoto. Jira/Confluence siguen pospuestos, sin publicación. Copia de estructura previa en artifacts para reversión local fuera de solución.
A evitar: imponer capas globales, arquitecturas distintas por apariencia, confundir estructura con patrones funcionando, CQRS físico/event sourcing sin necesidad o claims de eficiencia sin baseline.

## Consolidación y publicación solicitadas (2026-11-03)
Estado: el usuario encarga subir todos los cambios a sus repositorios y la solución al backend. La solución original y 28 proyectos se consolidan en API_NexoLocal con SDD, sin copiar .git/.vs/bin/obj/artifacts/secretos; fuente temporal conservada. Se prepara chore/publicacion-base-backend y móvil su rama propia. El encargo autoriza publicación de ramas aun con Jira/Confluence pospuestos; no inventar metadatos ni abrir/fusionar PR incompleto.
Decisiones: backend CI real de scaffold conectado a SDD mediante workflow reutilizable; docs espera restore/build/arquitectura/smoke. Guard verifica los 28 manifiestos previstos y rechaza pubspec; no se elimina para obtener un verde. Mantener actualización checkout ya fusionada por el equipo en origin/develop. Snapshots compartidos se refrescan explícitamente con hashes y documentos de ambas partes; no se crea repositorio para docs_nexo.
Deuda: confirmar publicación y runs remotos después del push, Jira/Confluence, revisión independiente y contratos/Ready; ninguna prueba de negocio, despliegue o proveedor nuevo. Publicar no equivale a aceptación funcional.
A evitar: subir compilados/secretos, usar main para saltar PR, sobrescribir avance remoto, publicar el checkout separado de código como otro repositorio o afirmar CD activo.

Verificación previa a publicación desde el checkout consolidado: restore local y Release build PASS, 0 errores/0 advertencias; arquitectura y catorce pruebas PASS; smoke nueve HTTP/Worker PASS con procesos propios detenidos. Ocho pruebas del validador SDD y 39 de gobierno PASS. Wrapper SDD completo en copia aislada PASS para API (26 specs/484 tareas/467 requisitos/322 criterios) y app (25 specs). Se refrescan solo 33/38 referencias documentales ya públicas; exportación masiva adicional de PDFs/XLSX rechazada automáticamente y excluida. Push y CI remoto se verifican después del commit, sin asumir éxito.

Publicación verificada (2026-11-03): rama chore/publicacion-base-backend enviada a https://github.com/Danix1219/NexoLocalApi; primer HEAD publicado 1443d64. Push exitoso, upstream configurado, árbol limpio y divergencia 0/0 comprobados. Se preserva origin/develop mediante merge sin conflictos. GitHub inició CI: https://github.com/Danix1219/NexoLocalApi/actions/runs/41691249227; en curso al registrar esta evidencia, sin afirmar aprobación anticipada. No PR/merge a ramas compartidas por falta de metadatos Jira/Confluence reales.

## Mapa de specs → capacidad, operaciones y dueño de datos (2026-11-05)
Estado: índice de lectura generado a partir de `specs/arquitectura/nv-*/spec.md` (sección «Asignación primaria vigente 0.4.0») y `plan.md`. No cambia alcance, no ratifica proveedores ni crea operaciones: solo resume qué resuelve cada incremento para no abrir specs una por una. Sigue vigente que todas están en Borrador documental.
Decisiones: la tabla es derivada, no autoridad; ante discrepancia manda `docs/contracts/inventario.json` y la spec. Los EP listados son los primarios del incremento; las operaciones reasignadas aparecen en la columna de notas de la spec origen, no aquí. Dueños marcados «(inferido)» salen del plan, no de una línea `Propietario:` explícita.
Deuda: fichas de operación, contratos formales y Ready humano siguen pendientes; D01 mantiene sin ratificar MongoDB/Atlas, Redis, YARP y proveedores.
A evitar: usar esta tabla como contrato, como cobertura de requisitos o como prueba de que un endpoint existe; duplicar un EP en otra spec porque la tabla lo resuma distinto.

| Spec | Qué hace | Dueño de datos | Operaciones primarias | Superficie de maqueta |
|---|---|---|---|---|
| nv-00 | Constitución y gobierno: fuentes, decisiones, responsables, versiones | Transversal | — | — |
| nv-01 | Fundación: servicios, entornos, configuración sin secretos, base física | Transversal | EP-089, EP-090 | — |
| nv-02 | Molde de incremento, contratos de frontera, matriz de versiones, envelope de eventos | Transversal | — | — |
| nv-03 | Gates de calidad y DevSecOps: unit, integración, seguridad, NoSQL, contrato, evidencia | Transversal | — | — |
| nv-04 | Identidad mínima: registro por celular, OTP, contraseña, sesión/refresh, aislamiento | S01 (inferido) | EP-001–005, EP-007, EP-008, EP-073, EP-101 | 7.1–7.3 |
| nv-05 | Moderación previa a persistir y medios privados/públicos, reportes y decisiones | S06 (inferido) | EP-075–079, EP-087, EP-099 | fotos de 7.10, 7.16 |
| nv-06 | Perfiles persona/negocio, catálogo, cobertura/radio, preferencias y edición | S02 (inferido) | EP-016–024 | 7.3 pro, 7.8, 7.11 |
| nv-07 | Búsqueda, mapa, sugerencias, comparación y feed público con caché/cursor | S02 + G01 (inferido) | EP-025–030 | 7.4, 7.6, 7.9, §9, §10 |
| nv-08 | Trabajos, experiencias declaradas, menciones y guardados | S03 (inferido) | EP-031–035 | 7.10, 7.12 |
| nv-09 | Solicitudes, intereses y consulta de interesados por propietario | S04 (inferido) | EP-047–052 | 7.7, 7.15 interesados |
| nv-10 | Pedidos: transiciones, concurrencia/asignación, contacto por consentimiento, resolución | S04 (inferido) | EP-058–065, EP-088 | 7.15 |
| nv-11 | Conversaciones, mensajes, bloqueos, ticket y avisos en vivo | S05 (inferido) | EP-066–072, EP-074 | 7.16 |
| nv-12 | Notificaciones y eliminación distribuida completa (saga de borrado) | S07 + transversal (inferido) | EP-012, EP-013 | §8, 7.13 eliminar cuenta |
| nv-13 | ML y asistencia generativa evaluables | Por ratificar | EP-082 | 7.10 (IA/ML) |
| nv-14 | Infraestructura, landing, DevSecOps y publicación | Gestión | — | — |
| nv-15 | Proyección de relaciones y lectura del grafo (opcional) | S08 | EP-098 | — |
| nv-16 | Cuenta, recuperación de contraseña y listado/revocación de sesiones | S01 | EP-006, EP-009–011 | 7.2, 7.13 |
| nv-17 | Catálogo y administración de categorías | S02 | EP-014, EP-015 | §4 catálogos |
| nv-18 | Reacciones, guardados e indicadores | S03 | EP-036–040, EP-045, EP-046 | 7.4 feed, 7.12 |
| nv-19 | Experiencias, menciones y reputación (estrellas, promedio) | S03/S02/S04 | EP-041–044, EP-093–095 | 7.8 opiniones, 7.15 calificación |
| nv-20 | Intereses y candidatos (orden, preselección) | S04 | EP-053–057 | 7.15 interesados |
| nv-21 | Dispositivos y notificaciones push | S07 | EP-083–086 | §8 |
| nv-22 | **Datos: creación idempotente, validadores/índices, TTL, migración, respaldo/restauración, supresión** | Transversal por propietario | — | — |
| nv-23 | Landing y expediente de evaluación | Gestión/landing | — | — |
| nv-24 | Administración de confianza | S06 | EP-080, EP-081 | 7.8 señales |
| nv-25 | Consentimiento y políticas de contacto (teléfono, domicilio) | S02/S04/S05 | EP-091, EP-092, EP-096, EP-097, EP-100 | 7.15, 7.16 copiar número |

### Base de datos (MongoDB/Atlas)
No hace falta una spec nueva: el dueño documental de la base es **nv-22 — Operación de datos, migraciones y recuperación** (scripts idempotentes, llaves/índices/validadores, TTL, migración, respaldo/restauración y supresión; FR-414 atiende el requisito académico 14 NoSQL). Lo complementan **nv-01** (configuración/cadena por servicio sin secretos) y el doc vivo [modelo-datos.md](docs/modelo-datos.md) con colecciones e invariantes por propietario. El proveedor concreto (Atlas, tier, transacciones, Redis) sigue en D01 sin ratificar: [registro de decisiones](docs/decisions/registro.md).

## Separacion de specs, conexion a datos y primeros endpoints (2026-11-05)

Estado: `specs/` separada en [arquitectura/](specs/arquitectura/) (26 incrementos nv-00..nv-25, capacidad y gobierno, sin endpoint nuevo) y [endpoints/](specs/endpoints/) (ep-01..ep-07, una spec por operacion HTTP con la misma estructura `spec.md → plan.md → task.md → checklists/gates.md`). S01 Identity tiene conexion real configurable a MongoDB Atlas con la cadena fuera del repositorio. Lint documental sin regresion: 281 enlaces rotos, los mismos de antes, todos hacia las carpetas hermanas ausentes `docs_nexo`, `App_NexoLocal` y `API_NexoLocal`. Integridad: 33 specs, 533 tareas, 530 requisitos, 357 criterios. Arquitectura de referencias: 28 proyectos conformes.

Decisiones: el identificador de cuenta es el correo, no el telefono; se corrigieron EP-001, EP-003, EP-004 y EP-006 en el inventario conservando sus rutas. La conexion de S01 se autoriza por encargo explicito y no ratifica D01 ni D04; detalle en el [registro de decisiones](docs/decisions/registro.md) y en [configuracion y secretos](docs/configuracion-secretos.md).

Deuda: no hay build ni arranque verificados, el SDK 10.0.302 de `global.json` no esta instalado en este equipo; `MongoDB.Driver 3.1.0` no se ha restaurado. Los otros ocho hosts siguen sin conexion. Contratos formales de los siete endpoints, fichas de operacion y revisor humano siguen pendientes. Renombrar `/auth/otp*` a `/auth/email/*` sigue sin decidir.

Deuda: ninguna de las 533 tareas esta ejecutada; el estado de las 33 specs sigue siendo Borrador documental.

A evitar: tratar `GET /health/ready` como operacion de negocio o consumirla desde la app; escribir la cadena de Atlas en `appsettings.json` o en un `.env` versionado; crear un noveno host o una base por spec; dar por ratificado Atlas, Redis o YARP porque exista el paquete y la conexion.

| Spec de endpoint | Operaciones | Dueño | Capacidad origen |
|---|---|---|---|
| `ep-01` (retirada) Pantalla principal | EP-030 `GET /feed` | S03 | nv-07 |
| `ep-02` (retirada) Registro con correo | EP-003 `POST /auth/register` | S01 | nv-04 |
| `ep-03` (retirada) Verificacion de correo | EP-001 `POST /auth/otp`, EP-002 `POST /auth/otp/verify` | S01 | nv-04 |
| `ep-04` (retirada) Inicio de sesion | EP-004 `POST /auth/login` | S01 | nv-04 |
| `ep-05` (retirada) Continuidad y cierre de sesion | EP-005 `POST /auth/refresh`, EP-007 `POST /auth/logout` | S01 | nv-04 |
| `ep-06` (retirada) Cuenta propia | EP-008 `GET /me` | S01 | nv-04 |
| `ep-07` (retirada) Recuperacion de contraseña | EP-006 `POST /auth/password-reset` | S01 | nv-16 |

Orden de construccion: ep-03 → ep-02 → ep-04 → ep-05 → ep-06 → ep-07. ep-01 es independiente y no exige sesion.

## Implementacion de los primeros 7 endpoints con Atlas real (2026-11-05)

Rama: `feature/Johann-arquitectura` desde `develop`.

Estado: los siete endpoints estan implementados y comprobados contra el cluster Atlas real.
- S01 Identity (ep-02 a ep-07): dominio `accounts`/`sessions`/`otp_challenges`/`otp_proofs`, `AuthService` sobre puertos, adaptadores Mongo, PBKDF2-SHA256 con 210 000 iteraciones, tokens opacos guardados solo como SHA-256, indices unicos y TTL creados de forma idempotente al arrancar, limite por IP de 60 peticiones por minuto y `GET /health/ready`.
- S03 Community (ep-01): `GET /feed` con filtros de zona y categoria, cursor opaco de fecha mas id y solo DTO publico.
- Verificacion ejecutada: 35 de 35 comprobaciones de [scripts/probe_endpoints.py](scripts/probe_endpoints.py) contra Atlas, mas paginacion del feed por cursor, exclusion de borradores y cursor invalido tolerado. Los datos de prueba se borraron al terminar.
- Compilacion verificada en un proyecto desechable net9.0 con 0 errores y 0 advertencias, porque este equipo no tiene el SDK 10.0.302 que exige `global.json`.

Decisiones: la cadena de Atlas vive en el almacen de secretos del usuario (`nexolocal-identity`, `nexolocal-community`), nunca en el repositorio. Sigue sin ratificar D01 (cluster, tier, transacciones, Redis, YARP) y D04 cambia de SMS a proveedor de correo, aun sin elegir.

Tres defectos encontrados y corregidos durante la verificacion:
1. El replay de un refresh rotado no revocaba la familia, porque tras rotar el hash antiguo ya no se encontraba. La sesion ahora guarda `previousRefreshTokenHash` y el replay revoca la familia completa.
2. Un cuerpo JSON invalido devolvia 500 en Development. `ThrowOnBadRequest = false` lo deja en 400 en todos los entornos.
3. El cursor del feed combinaba `RoundtripKind` con `AdjustToUniversal`, lo que lanzaba al pedir la segunda pagina.

Deuda: sin proveedor de correo, el codigo de verificacion solo se escribe en el registro y unicamente en Development; fuera de Development no se entrega nada y queda anotado. No hay proyectos de prueba en la solucion, asi que la comprobacion vive en un script y no en CI. El token de acceso es opaco y se valida por lectura en base, no JWT: cambiarlo exige decision de contrato. Los otros siete hosts siguen sin conexion. El estado de las specs sigue siendo Borrador documental porque el lint exige ese texto literal; la evidencia de ejecucion esta aqui y en el registro de decisiones, no en la spec.

A evitar: subir la cadena de conexion a `appsettings.json`, a un `.env` versionado o a un commit; tratar el script de sonda como aceptacion de producto o como sustituto de los gates de nv-03; usar `/health/ready` desde la app; reutilizar la base de un propietario desde otro host.

## SDK 10 instalado y solucion compilando (2026-11-05)

Estado: la solucion completa compila con **0 errores y 0 advertencias** y los nueve proyectos tocados corren desde los hosts reales. Verificacion repetida sobre los hosts de `launchSettings` (S01 en 127.0.0.1:5188, S03 en 127.0.0.1:5190): 35 de 35 comprobaciones de la sonda contra Atlas, mas la paginacion del feed. Datos de prueba eliminados.

Decisiones: se instalo el SDK **10.0.403** y `global.json` pasa de `10.0.302` a `10.0.403`; `rollForward: latestPatch` no acepta otra banda de caracteristicas, por eso se fija la version instalada. `MongoDB.Driver` sube de 3.1.0 a **3.13.0**: la 3.1.0 arrastraba `Snappier 1.0.0` con vulnerabilidad alta (GHSA-pggp-6c3x-2xmx) y `SharpCompress 0.30.1` con una moderada; con 3.13.0 el build queda sin avisos. Las rutas llevan el prefijo `/api/v1` conforme al indice de endpoints, salvo `/health`.

Deuda: Visual Studio 17.14 no compila `net10.0` (NETSDK1209, exige 18.0+). Hoy se compila y ejecuta por CLI; si el equipo necesita compilar dentro de la IDE hay que decidir entre actualizar Visual Studio o bajar el objetivo, y eso toca D01. La version exacta del SDK sigue sin ratificar: `global.json` refleja lo instalado en este equipo, no un acuerdo de equipo.

A evitar: suponer que el resto del equipo puede abrir la solucion en VS 17; dar por ratificado el runtime porque `global.json` ya apunte a una version instalada.

## nv-00 ejecutada: una API modular sustituye a los microservicios (2026-11-06)

Rama: `feature/Johann-arquitectura`, reconstruida sobre `develop`. La rama anterior no se perdio: se habia mergeado en los PR #7 y #9 y se borro despues, que es el flujo acordado.

Estado: la arquitectura de nv-00 queda construible y los restos de microservicios estan retirados.

- **Solucion**: `src/Nexo.sln` con 28 proyectos (20 en `src/`, 8 en `tests/`). Tres dominios de cinco capas (`Identity`, `Discovery`, `Engagement`), `Screens`, los bloques `Nexo.Common` y `Nexo.Outbox`, y un unico `Nexo.Host`. `dotnet build` → **0 errores, 0 advertencias**.
- **Dos roles, un binario**: `NEXO_ROLE=api` escucha y responde `/health/live` 200; `NEXO_ROLE=worker` corre sin abrir ningun puerto. Verificado por PID.
- **Degradacion**: con Mongo y Redis caidos, `/health/ready` responde **503**, no 200 por omision.
- **Retirado** con `git rm`, historial conservado: `services/` completo (8 carpetas, 23 `.csproj`), `nexolocal.Api/`, `nexolocal.Api.slnx`, `scripts/check_architecture.py` y `scripts/Test-ScaffoldHosts.ps1`. `architecture-selection.json` archivado. El codigo util no se perdio: Identity vive en `src/Domains/Identity`, el feed de community en `Discovery`, `MongoOptions` y el limitador de Johann en `Nexo.Common`.
- **Entorno local**: `docker-compose.yml` con MongoDB en replica, Redis y Azurite, mas `scripts/mongo-init.js` que crea las tres bases de forma idempotente.
- **Despliegue**: `deploy-azure.yml` pasa de un contenedor por servicio a un plan B1 con dos instancias y el worker como WebJob continuo en el mismo plan.
- **Pruebas**: 59 unitarias en verde (Common 16, Identity 29, Architecture 7, Discovery 7), incluido el caso negativo de fronteras con `Nexo.Fixtures.BadDiscovery`.
- **Secretos**: escaneo de los 299 archivos rastreados, 0 hallazgos.

Decisiones ratificadas por esta spec: **D-1** una API modular en lugar de 8 microservicios (~26 USD/mes contra ~208), **D-2** sin grafo, MongoDB con `$graphLookup`, **D-3** tres motores NoSQL y un solo despliegue. Detalle, inventario y calculo de costo en el [registro](docs/decisions/registro.md).

Defecto encontrado y corregido al verificar: los bootstraps de indices de Identity y Discovery esperaban el timeout de Mongo dentro de `StartAsync`, lo que retrasaba ~30 s que el host escuchara. Ahora corren en segundo plano; el host responde a los 12 s.

Deuda abierta: **AC-005, AC-006 y AC-007 positivo siguen sin evidencia** porque Docker Desktop no estaba levantado, y las 11 pruebas de `Nexo.Integration` fallan por eso, no por el codigo. El limite de conexiones del cluster M0 sigue sin confirmar en el panel, asi que `maxPoolSize = 40` es provisional. El costo de ~26 USD cubre solo el computo: el Redis hospedado añadiria del orden de 16 USD y aun no esta elegido. `Nexo.Engagement.Unit` sigue vacio. La enmienda **E-001 sigue siendo propuesta**: nada de esto esta aprobado hasta que Johann la revise y firme.

A evitar: marcar los AC pendientes como cumplidos sin levantar Docker; tratar los ~26 USD como costo total del sistema; dar por aprobada E-001 porque el codigo ya exista.

## nv-00: los 10 AC con evidencia ejecutada (2026-11-06)

Con Docker levantado se cerraron los tres criterios que faltaban. **70 de 70 pruebas en verde**: Common 16, Identity 29, Architecture 7, Discovery 7, Integration 11.

- **AC-005**: una escritura de un dominio no aparece en la base de otro, contra MongoDB real. Las tres bases las crea `scripts/mongo-init.js` de forma idempotente.
- **AC-006**: un `jti` revocado en la instancia A es rechazado por la B, contra Redis real; con el contador de rate limit compartido y la idempotencia de un reintento que cae en la otra instancia.
- **AC-007**: readiness responde 200 con el entorno arriba y **503** con Redis caido; liveness sigue en 200 en ambos casos.

Documentacion de la API repuesta: OpenAPI nativo de .NET 10 mas Scalar en `/scalar/v1`, solo fuera de produccion. **Sin CORS a proposito**: la app es nativa y CORS solo lo aplica un navegador; al aparecer Flutter Web o la landing se añade una lista de origenes, nunca `AllowAnyOrigin`.

Tres defectos mas encontrados al verificar y corregidos: el feed de Discovery estaba en `/api/v1/feed` en vez de `/v1/discovery/feed`, contra la seccion 3 de la spec; el contrato entregado al equipo movil citaba las rutas de la arquitectura anterior y quedo desfasado por completo; y `mongo-init.js` solo creaba la primera base porque `target._domain` choca con las propiedades internas de mongosh.

Deuda que queda: el OpenAPI documenta las rutas pero **no las respuestas**: solo `200 OK` sin cuerpo, sin 400, 401, 409 ni 429 y sin esquema. Para que el equipo movil genere su cliente desde ahi hacen falta los `Produces<T>` en los nueve endpoints; es trabajo fuera de nv-00. Siguen pendientes el limite de conexiones real de M0, la eleccion del Redis hospedado y su costo, y `Nexo.Engagement.Unit` vacio.

**E-001 sigue siendo propuesta.** Los diez AC tienen evidencia ejecutada, pero ninguna casilla de tarea se marca y nadie firma hasta que Johann revise la enmienda.

## Retirada de las operaciones de negocio (2026-11-06)

Estado: el backend queda **sin endpoints de negocio**. El usuario determino que las operaciones publicadas no eran las correctas, asi que se retiraron el dominio Identity, el feed de Discovery, los endpoints sin implementacion de Engagement y sus 36 pruebas. La **arquitectura de nv-00 sigue intacta**: 28 proyectos, cinco capas por dominio, los cuatro grupos de ruta listos, bloques comunes, Redis, tres bases y sondas. `dotnet build` en 0/0 y 34 pruebas en verde (Common 16, Architecture 7, Integration 11).

Decisiones: la retirada **contradice FR-008 y deja AC-008 sin objeto**, y asi queda escrito en gates.md y en el [registro](docs/decisions/registro.md). No se marca como cumplido lo que se anulo. El contrato del equipo movil se archivo con un aviso de no implementar contra el, porque describia operaciones que ya no existen.

Deuda: definir **que operaciones van primero** antes de volver a escribir codigo; ese fue el error que se esta deshaciendo. Las specs `ep-01` a `ep-07` siguen en el repositorio describiendo lo retirado y quedan obsoletas hasta rehacerse. El inventario de operaciones tampoco se ha revisado contra esta decision.

A evitar: reimplementar los mismos endpoints a partir de las specs `ep-*` sin antes acordar el conjunto correcto; dar por buena una operacion porque exista su ficha en el inventario.

## Correo por MailKit y host unificado (2026-11-06)

Estado: el codigo de verificacion **se envia de verdad**. `SmtpEmailSender` (MailKit) y
`IdentityOutboxHandler` convierten `identity.email_code_requested` en un correo; probado contra
Mailtrap sandbox: `POST /v1/identity/email/verification` devuelve 202 y el outbox queda en
0 pendientes y 3 procesados. `dotnet build` en 0/0.

Decisiones: **se retiro la separacion `NEXO_ROLE=api|worker`**. El host es un solo proceso que
atiende HTTP y despacha el outbox. Motivo del usuario: no hay forma comoda de arrancar dos
instancias del mismo proyecto desde Visual Studio, porque `launchSettings.json` pisa la variable
de entorno con la del perfil. Con dos instancias en el plan no hay envios duplicados: el reclamo
del outbox es un `UpdateOne` condicional que comprueba `ModifiedCount == 1`.

Tambien se descarta el mensaje cuyo codigo ya vencio: un fallo de SMTP se reintentaba cada cinco
segundos sin tope ni espera creciente, de modo que un correo inutil consumia cuota para siempre.

Deuda: **esto contradice nv-00**, que describe los dos roles como decision gobernada
(`spec.md`, `plan.md`, `task.md`, `gates.md`, la constitucion y `estructura-backend.md`).
Corresponde una enmienda; no se redacto. El costo aceptado es que un envio SMTP lento ocupa el
proceso que atiende peticiones. Mailtrap sandbox **no entrega** correos: para produccion falta un
SMTP real. Sigue sin devolver tokens `POST /v1/identity/accounts` y no hay pruebas de 01-auth.

A evitar: dar por hecho que el correo sale en local sin mirar el log del despachador; el endpoint
responde 202 aunque el envio falle despues, que es justamente el proposito de la cola.
