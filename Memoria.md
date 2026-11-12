# Memoria — API_NexoLocal

## Implementación local de 02-cuenta (2026-11-09)

Estado: ocho operaciones vigentes implementadas en Identity, rama `feature/02-cuenta`. Se conserva
la auditoría anterior y los cambios IDE preexistentes; sin commit, push, despliegue ni datos reales.
GET/PATCH cuenta, modo, móvil declarado, registro/baja de dispositivos y solicitud/cancelación de
baja local. La antigua confirmación SMS sigue retirada. Alcance funcional en nv-04; saga global
permanece en nv-12. Contrato y rollback en `specs/endpoints/02-cuenta/implementacion.md`.

Decisiones: sin L1 para la cuenta propia; proyección sin hash. Se reutiliza IDeviceRegistry para
registro/borrado/logout. Cambio+revisión+eventos de cuenta atómicos en Mongo, relay idempotente al
outbox y auditoría persistida con cadena por cuenta. auth_epoch invalida JWT/refresh antiguos;
gracia con recovery_only, restringido hasta renovar después de cancelar. Modo no emite tokens.
La política user se aplica tras autenticar y el contador compartido particiona por cuenta.

Verificación final ejecutada con SDK 10.0.302: build Release --no-restore -m:2, cero errores y
advertencias; dotnet format src/Nexo.sln --verify-no-changes --no-restore PASS. Suite completa
con NEXO_DISABLE_DOTENV=1 y --no-build: 96 correctas, cero fallidas/omitidas (16 Common,
7 arquitectura, 55 seguridad incluidas 40 nuevas, 18 integración). Resultados locales en
`artifacts/cuenta-verified-results`. Las tres suites Unit vacías no se cuentan como cobertura.
Mongo/Redis reales de Testcontainers; solo HTTP de moderación y captura de avisos son dobles.
SDD e integridad: 26 specs, 484 tareas, 468 requisitos, 323 criterios; ocho pruebas del validador.
No se firman casillas de aceptación humana ni aprobación productiva.

Deuda: ContentSafety no está configurado en el .env local; cambio de nombre devuelve 503 antes
de guardar. El registro de avatar exige propietario, approved y purpose avatar, pero su carga y
aprobación pertenece a nv-05. nv-12 aún debe ejecutar anonimización, medios externos y efectos
sobre pedidos/perfiles; no afirmar borrado global por aceptar una solicitud. Avisos SMTP y
auditoría local tienen consumidor; eventos de otros dominios requieren integración y reentrega
según su dueño. Persisten deuda de cambio efectivo de correo y retención de códigos del outbox.

A evitar: nuevos proyectos/capas o referencias entre dominios, falsear moderación aprobada,
tratar el móvil como verificado, devolver conteos inventados o restaurar acceso de JWT antiguos.
No declarar Gmail/Content Safety/Azure/Flutter aceptados por las pruebas locales.

## Auditoría de endpoints y conciliación de correo (2026-11-08)

Estado: revisión del clon actual en `fix/auditoria-identidad-email`, sin commit, push ni despliegue.
Siete rutas HTTP de autenticación implementadas; ocho operaciones al separar password/refresh
en `/connect/token`. Cuenta, Discovery, Engagement y Screens no tienen operaciones registradas.
El índice `docs/endpoints.md` separa implementación de la línea base histórica de 101 EP.

Decisiones: usuario confirma Gmail vía SMTP/MailKit, sustituyendo SMS; móvil opcional declarado,
sin afirmar control del número. Fichas conciliadas con `emailVerified` y consentimiento para llamadas.
Confirmación SMS retirada del diseño, enlaces históricos conservados. `changeEmail` exige sesión;
la ruta pública rechaza `resetPassword`, que entra por recuperación. SDK fijado en 10.0.302 por
autorización expresa del usuario; net10.0 conservado. Host unificado conciliado con constitución.
Modo futuro no emite tokens: respuesta indica renovar en `/connect/token`. Idempotencia de bajas
y dispositivos se aclara como efecto estable, sin prometer respuestas idénticas al repetir.

Verificación ejecutada: build Release con cero errores/advertencias; format verify-no-changes PASS;
56 pruebas correctas, cero fallidas/omitidas (16 Common, 7 arquitectura, 15 seguridad, 18 integración).
NEXO_DISABLE_DOTENV=1 omite incluso el archivo de carpeta padre; suites con Mongo/Redis efímeros.
Wrapper SDD ejecutado en copia aislada con carpeta API_NexoLocal: 26 specs, traza e integridad PASS,
8 pruebas del validador PASS. Estas pruebas no acreditan entrega real Gmail ni revisión humana.

Deuda: aplicar cambio de correo aún no tiene endpoint; consumidores de bienvenida/cambio de contraseña
y propagación no están implementados. Outbox conserva payloads con códigos; falta depuración segura.
Cancelar baja tras vencer la sesión requiere definir acceso restringido, porque los grants actuales
rechazan pendingDeletion. No empezar el siguiente incremento dando esos flujos por completos.

A evitar: reintroducir SMS, marcar móvil como verificado por confirmar un correo, consumir rutas
históricas `/auth/*`, equiparar existencia de ficha con implementación, o presentar pruebas locales
como Gmail/Azure/Flutter aceptados. No se alteraron secretos ni datos reales.

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

## Versionado, infraestructura efimera y pruebas de seguridad (2026-11-08)

Estado: se cierran tres requisitos de los lineamientos que estaban pedidos con todas las letras y
sin implementar. 53 pruebas en verde con el `.env` apartado (16 unitarias, 7 de arquitectura, 12
de seguridad, 18 de integracion), 58 de gobierno, gitleaks sin hallazgos, `verify-sdd.ps1`
completo, Terraform valido y actionlint limpio.

**Lineamiento 11.** `VERSION` en la raiz es la unica fuente. `.ci/check-version.py` exige SemVer
y, en ramas `release/*`, que el nombre de la rama coincida; con 19 pruebas propias. El despliegue
sella la version en el ensamblado con el SHA corto, crea la etiqueta `api-vX.Y.Z` **solo si el
sitio respondio** y la publica en `info.version` del documento OpenAPI. Se descarto un
`GET /version`: seria una operacion nueva y exigiria su ficha en `specs/endpoints/`.

**Lineamiento 15.** El Terraform no describia nada de lo que estaba corriendo: declaraba otro
grupo, una app por contenedor desde un ACR inexistente y un plan de dos instancias. Pasa a
describir un **entorno efimero** en plan F1, que no consume la cuota de VMs B1 y es gratuito, con
CDN y colas tras banderas apagadas. El workflow `infra.yml` ofrece `plan`, `apply` y `destroy`,
este ultimo exigiendo escribir `destruir` antes incluso de autenticar.

**Lineamiento 7.** `tests/Nexo.Seguridad.Tests` anade doce guardas de regresion sobre protecciones
ya implementadas: enumeracion de cuentas por los tres caminos, bloqueo tras cinco intentos, lista
negra de `jti`, revocacion de la familia al reusar un refresh, y que ni contrasena, ni hash, ni
codigo salgan en ninguna respuesta. Las pruebas crean cuentas reales leyendo el codigo del outbox,
que es donde lo deja el endpoint; que no viaje en la respuesta es justo lo que una comprueba.

Decisiones con su coste:

- **La validacion de `instancias` baja de 2 a 1.** Prohibir bajar de 2 hacia imposible ejecutar
  `apply` con la cuota de estudiante, y entonces el ciclo crear/eliminar no se podia demostrar en
  absoluto. El valor por omision sigue siendo 2 y la salida `instancias_del_plan` expone la
  desviacion en cada ejecucion.
- **El entorno es efimero y gratuito, no un reflejo de produccion.** Reflejar produccion exigiria
  importar recursos creados a mano y un `destroy` borraria el sitio vivo.
- **Terraform despliega por codigo, no por contenedor.** Se eliminan el ACR y su asignacion de rol:
  menos coste, menos configuracion y se parece a como publica el workflow real.

Deuda: dos instancias siguen bloqueadas por la cuota; Redis sigue en Upstash y no en Azure; no hay
cache a nivel Mongo; el CDN existe en codigo pero sin subida de imagenes implementada; `AC-010`
sigue sin verificarse en cada despliegue. El entorno efimero crea el sitio vacio: publicar codigo
en el exigiria su propio perfil.

A evitar: dar por cumplido el requisito de dos instancias porque el codigo lo soporte. Lo soporta
y hay una prueba que lo acredita, pero en Azure corre una sola.

### 2026-11-09 — Ubicacion de busqueda desde la app

PUT /v1/discovery/me/location implementado en las capas existentes de Discovery.
Persiste GeoJSON, radio y fecha por sub autenticado en user_profiles de Discovery.
Validacion Mexico, radio y origen; la autorizacion global conserva baja/revocacion.
Prueba nueva dentro de CuentaTests: HTTP y Mongo/Redis aislados, 401 anonimo,
400 fuera del viewbox y coordenadas/radio persistidos bajo la cuenta correcta.
Regresion backend: 97 pruebas aprobadas (16 Common, 7 Architecture, 18 Integration,
56 Seguridad). No se cuentan proyectos unitarios vacios como cobertura.
Ver specs/endpoints/04-descubrimiento/implementacion-ubicacion.md para limites:
no hay geocodificacion ni conteos ficticios. Fotos continúan pendientes de Blob
y moderacion no configurados; el usuario autorizo mostrar Proximamente.


### 2026-11-09 — Incremento 03-perfil-prestador (nv-06 parcial)

Estado: cinco operaciones de perfil incorporadas en las capas actuales de Discovery: alta, propio, público, edición por siete secciones y comparación. Índice por cuenta, alta idempotente con respuesta original, CAS por revisión, moderación cerrada, subidas propias/aprobadas y GeoJSON con proyección pública sin punto home. Cuenta activa/correo/móvil se consultan por contrato de Identity; solo Identity actualiza hasProvider.

Decisiones: política provider-completeness/1 única para alta y propio; horario real America/Mexico_City; distancias redondeadas; etiquetas de zona genéricas sin geocodificación. Sin caché de perfiles hasta disponer de invalidación real. Visitas agregadas reales; contacto e intereses null cuando no existen. Eventos durables locales con límite, sin despacho cloud ficticio.

Verificación: 127 pruebas aprobadas: 7 arquitectura, 16 Common.Unit, 14 Discovery.Unit, 26 Integration y 64 Seguridad.Tests. Son 30 casos nuevos sobre los 97 anteriores; Identity.Unit y Engagement.Unit siguen sin casos y no se cuentan como cobertura. Ejecución Release con cobertura, TRX en artifacts/profiles-verified. Integridad SDD aprobada; lint de enlaces conserva fallos históricos. Detalle en specs/endpoints/03-perfil-prestador/implementacion.md.

Deuda: Content Safety/Blob/CDN reales, catálogo público y descubrimiento (04), dispatcher/purgas, validator de colección y revisión humana. No es cierre completo de nv-06. No se modificaron secretos ni móvil, ni se publicaron cambios. A evitar: aprobar texto sin moderación, afirmar teléfono verificado o equiparar fotos con trabajos; no confundir prueba aislada con aceptación cloud.


### 2026-11-10 — Correo de marca y ubicación real
Estado: SMTP multipart HTML/texto con identidad, código escapado y fecha en America/Mexico_City; Photon detrás de Discovery con búsqueda/inversa y GET privado de ubicación. Labels genéricos anteriores se resuelven al leer. Sin logs de direcciones ni fallback ficticio. Contrato/evidencia en specs/endpoints/04-descubrimiento/geocodificacion.md.
Verificación: 139 casos únicos por capas (7/16/19/4/27/66), regresiones del contrato null y nombres corregidas; build final Release sin errores/advertencias. Búsqueda e inversa reales de Tula HTTP200 y ready200. Vista previa de correo inspeccionada en Chrome, sin enviar SMTP real. Docker requiere ejecución fuera del sandbox para acceso al daemon.
Decisiones/deuda: lang=es no admitido por instancia pública; usar nombres locales. Photon público sin SLA, solo uso moderado; Geocoding__BaseUrl permite sustitución HTTPS. Sin catálogo/búsqueda de prestadores, despliegue ni aprobación humana total. .env operativo intacto; valores de Mail en .env.example sustituidos por placeholders. Evitar equiparar Testcontainers, preview o APK con aceptación cloud completa.

### 2026-11-10 — Directorio público del grupo 04
Estado: GET categories, search/suggest, search/providers y map/nearby providers implementados en Discovery. Catálogo Mongo versionado con slugs ya aceptados, colores/etiquetas de maqueta; búsqueda AND sin acentos, filtros, estrategias, radio/serves, expansión y cursor compacto por ancla/filtros. Mapa con puntos difuminados persistidos para home; locales publicados exactos. Identity aporta estado mínimo por lote, solo su adaptador lee su base. Cuenta inactiva no aparece, sin caché de resultados obsoletos.
Verificación: 170 casos aprobados con cobertura en las capas actuales (7 arquitectura, 16 comunes, 34 Discovery, 4 Identity, 30 integración y 79 seguridad). 31 nuevos; Engagement sin casos. Integridad SDD local aprobada, enlaces históricos pendientes. Evidencia, decisiones y límites en specs/endpoints/04-descubrimiento/implementacion-directorio.md; aceptación humana/cloud no inferida.
Decisiones: se refinan fichas históricas a nv-06, no se añaden cinco especialidades ficticias a otros ni cambian slugs existentes. Hora real México, distancias aproximadas 0.5 km. Sin Atlas Search/caché Redis de consultas aún: presupuesto 1000 candidatos/5 s Mongo y 503 explícito al superar, nunca total truncado. Catálogo ETag; búsquedas/mapa/sugerencias private no-store.
Deuda: proyección/índice especializado y caché con invalidación antes de escalar; feed y kind=requests esperan grupos 05/06. requests controla 401/403 y devuelve 503 hasta disponer de la fuente. Siguiente grupo independiente: 05-trabajos con moderación y subidas aprobadas. Sin cambios móviles, secretos, commits/push, despliegue o Terraform. Evitar llamar a resultados vacíos error si no hay perfiles reales; no sembrar perfiles demo.

Cierre operativo: build Release sin errores/advertencias y diff-check 0. API local reiniciada en 5188; ready y las cuatro rutas nuevas respondieron 200 con configuración existente. La regresión de seguridad requirió aislar la dirección de cada HostDeSeguridad mediante TestClientAddress ya existente: evita interferencias de la cuota Redis entre pruebas, sin alterar límites del producto. Resultado final 79/79 en seguridad con cobertura; el primer log con 12 fallos de aislamiento se conserva y no se presenta como verde.

### 2026-11-10 — Trabajos del grupo 05
Estado: cinco operaciones en Discovery: publicar, listado por prestador, borrar propio y POST/DELETE recomendación. Categoría contra perfil y cuenta activa por contrato. Moderación de todos los textos previa a persistir, ubicación solo difuminada 500–800 m, cursores compactos ligados al prestador y marcas por usuario sin caché compartida. Reintento de alta conserva el resultado; clave reusada con otro payload 409; no resucita trabajo borrado. Retirado por moderación puede borrarlo su autor.
Decisiones: cuota durable 30 publicaciones por día UTC dentro de la misma transacción Mongo que trabajo, contador jobs, recibo mínimo y evento. Recomendaciones únicas actualizan trabajo/prestador atómicamente, sin autobombo ni autores públicos; borrado elimina relaciones y decrementa contadores sin negativos. Tests existentes usan Mongo replica set real. Fichas vigentes 90/1000/6 prevalecen sobre ejemplos históricos nv-08 60/200/4; sin paquete/proyecto nuevo.
Verificación: 197 casos únicos por capas con cobertura, 27 nuevos (13 unitarios, 6 persistencia y 8 HTTP/seguridad), mediante solución completa 196 y regresión final de borrado. Integridad SDD aprobada; enlaces históricos pendientes. Registro en specs/endpoints/05-trabajos/implementacion.md. No equivale a moderación Azure real ni aprobación humana.
Deuda y a evitar: Blob/CDN/subida/moderación/purga reales ausentes: no publicar imágenes y no simular borrado de un legacy con medios. Content Safety sin configurar devuelve 503 para publicación; doble solo en pruebas. Eventos Discovery durables, sin despacho cloud ni push acreditados. Feed, guardados y solicitudes no implementados por este incremento. Sin móvil, secretos, commits/push, despliegue o Terraform. Siguiente grupo: 06-solicitudes. No confundir logs/TRX con aceptación cloud.

Cierre operativo: build Release sin errores/advertencias, diff-check 0, API local en 5188 lista (ready 200); OpenAPI registra cinco operaciones works. Comprobación anónima: mutaciones 401 y listado de prestador inexistente 404. No se crearon cuentas/trabajos reales para verificar esas rutas.

### 2026-11-10 — Solicitudes del grupo 06
Estado: POST requests, GET requests/mine, GET requests/opportunities y POST requests/{id}/close en Discovery, más fuente real para map/nearby?kind=requests. Pruebas en las capas existentes: 228 aprobadas con cobertura, 31 nuevas (14 unitarias, 8 integración/contratos, 9 HTTP/seguridad). Regresión completa salida 0; compilación sin errores/advertencias. Ver specs/endpoints/06-solicitudes/implementacion.md y docs/estado-desarrollo-endpoints.md.
Decisiones: propietario nv-09; cuatro fichas refinadas sin implementar detalle/PATCH/DELETE ni grupo 07. Punto exacto solo en location privado de Mongo, excluido antes de materializar; público difuminado una vez. Cuota 10/día UTC, solicitud, recibo mínimo y evento en transacción local; reintento estable y rollback real. Filtros de perfil/categoría/radio, cursor compacto y búsqueda normalizada sin Atlas Search. Contratos mínimos Identity (iniciales) y Engagement (resumen real); Host compone, circuito/timeout con disponibilidad explícita. Cierre solo del propietario, fecha estable, declaración de prestador no habilita reseñas.
Deuda: Content Safety real requerido; fotos bloqueadas hasta almacenamiento/moderación/purga. Eventos durables sin despacho/push/pedidos distribuidos acreditados. canReview false hasta flujo autorizado de pedidos. Interés y su mantenimiento de contadores son el siguiente grupo 07. SDD-INTEGRITY-OK, pero check_sdd.ps1 conserva enlaces externos históricos rotos; revisión humana pendiente.
A evitar: confundir dobles de moderación con nube o pruebas con aceptación. Sin cambios móviles, secretos, commits/push, despliegue ni Terraform. API iniciada en 5188; live 200, OpenAPI cuatro rutas y consultas privadas anónimas 401. Se encendieron Redis/colas locales existentes; ready no aprobado porque Mongo no responde con la configuración actual. No se cambió la conexión ni se sembraron datos reales.


### 2026-11-10 ? Medios Azure reales y consumo Flutter
Estado: intenci?n/complete/estado/retirada de uploads, lectura WebP/miniatura, portada personal y v?nculo de avatar/perfil profesional/trabajos/solicitudes implementados en las capas actuales. Regresi?n completa Release 236/236; despu?s, suite MediaPipelineTests 6/6 (incluye una prueba adicional) y MediaEndpointsTests 3/3 ampliada con portada, solicitud y trabajo. Son 237 casos distintos cubiertos entre las ejecuciones. Flutter267/267 con cobertura; analyze0errores/0warnings y13infos previos. APK construido, instalado -r y abierto en tel?fono096fd2cc784f sin borrar datos. API5188 reiniciada, ready200 y ocho operaciones de medios presentes en OpenAPI.
Decisiones: Discovery es due?o de uploads; contrato m?nimo enlaza Identity mediante Host. SAS create-only15min sin Bearer, privados incoming/public, cola image-processing, byte/mime/dimensiones reales, EXIF/orientaci?n corregidos, WebP1600 y miniatura400. Azure Content Safety real antes de aprobar. Fase verificada durable permite recuperaci?n tras borrar fuente; limpieza caducados y auditor?a de referencias respetan borrado reversible. No secretos en Flutter ni SAS en logs.
Evidencia cloud: SAS PUT, cola, Content Safety imagen, codec/miniatura y borrado con imagen sint?tica y DBAtlas aislada; todo limpiado (artifacts/media-cloud-check.txt). No se cre? publicaci?n ni cambi? foto de usuario real para esa prueba. NuGet audit sin vulnerabilidades conocidas en dependencias Discovery.Infrastructure. Evidencia de pruebas en artifacts/media-tests.txt,media-pipeline-final.txt,media-http-final.txt y media-flutter-tests-final.txt de la carpeta de trabajo.
Deuda: recorrido f?sico de galer?a/c?mara y cada pantalla por usuario; CDN/FrontDoor y chat privado fuera. Configurar MediaStorage__PublicBaseUrl HTTPS con URL de API al desplegar; desarrollo actual usa127.0.0.1:5188 con adb reverse. SDD-INTEGRITY-OK26specs; check_sdd.ps1 a?n falla por enlaces hist?ricos externos a antiguos checkouts. Revisi?n humana no firmada.
A evitar: confundir pruebas HTTP con nube, prueba sint?tica cloud con aceptaci?n de todos los recorridos, o instalaci?n local con despliegue. .env no cambiado ni expuesto; sin nuevos recursos, Terraform, commits ni push.

### 2026-11-10 — Edición de publicaciones y galería de solicitudes
Estado: PATCH de solicitud propia abierta y PATCH de trabajo propio, con nueva moderación de texto, comprobación de medios aprobados y actualización parcial de campos editables en Mongo. Solicitudes admiten hasta seis imágenes y conservan compatibilidad con `imageUrl` anterior. Fichas nuevas en los grupos 05 y 06.
Decisiones: actualizar campos individuales evita sobrescribir contadores y estado concurrente. `checkedAt` dentro de `data` se mantiene como cadena ISO, igual que en el documento original; el valor BSON de fecha ocasionaba HTTP 500 al deserializar y se corrigió.
Verificación: regresión previa al último ajuste de fecha, 246/246 pruebas .NET. Después de ese ajuste, las dos pruebas HTTP de edición que habían fallado pasaron 2/2. Integridad documental local 26 specs OK; `check_sdd.ps1` sigue fallando por enlaces externos históricos. Falta repetir la regresión .NET completa después de la corrección de fecha y comprobar la API en ejecución. No equivale a prueba con Atlas ni aceptación humana.
Deuda: recorrido manual de edición con imágenes reales en la cuenta del usuario. Sin secretos, commit ni despliegue. Evitar declarar la regresión completa verde después del último parche basándose solo en las dos pruebas focalizadas.


### 2026-11-10 ? Corrección de fotos y publicaciones extensas
Estado: filtro de imágenes ahora admite nivel bajo 2 y bloquea medio/alto 4/6; respuesta incompleta o fallo Azure no aprueba. Estado de upload rechazado expone el código de formato/dimensiones separado del rechazo de contenido. Listas mine/opportunities devuelven description completa junto al excerpt previo. Flutter usa Ver más/Ver menos en tarjetas de solicitudes y trabajos con medición real del desbordamiento.
Decisiones: el umbral del texto moderado permanece independiente. No se exige detectar rostro ni se comparan identidades. No se guarda la imagen ni las puntuaciones de categoría en logs. El rechazo específico informado por el usuario no se puede atribuir a una categoría sin evidencia de su análisis.
Verificación: .NET Release 242/242, Flutter 269/269 con cobertura, analyze 0 errores/0 advertencias y 13 avisos informativos anteriores. APK debug/live instalado -r conservando datos, app abierta. API local ready 200. Integridad SDD 26 specs OK; check_sdd.ps1 móvil aún falla por problemas documentales históricos fuera de 057.
Deuda: recorrido manual con la foto real del usuario tras reinstalación, aprobación humana y limpieza documental histórica. A evitar: declarar que la foto anterior se aprobó o que la suite prueba visualmente la galería.

### 2026-11-10 — Zona pública del prestador y caché de medios
Estado: Discovery resuelve etiqueta de localidad con Photon para el prestador sin exponer su punto exacto; repara etiquetas genéricas anteriores al consultar el perfil propio. Las imágenes aprobadas usan caché privada de 5 minutos. Prueba de zona y prueba de seguridad de medios pasan; suite .NET: arquitectura 7/7, Common 16/16, Discovery 63/63, Identity 4/4, Integration 54/54; Seguridad 103/103 tras ajustar expectativa de caché. SDD-INTEGRITY-OK 26 specs; API local /health/ready 200. Aceptación física pendiente.

### Incremento 07-interes (2026-11-10)
Estado local: POST/DELETE interest y GET interested conectados a Engagement; el HTTP valida auth, perfil publicado y visibilidad por cuenta/radio desde Discovery compuesto por Host. Persistencia usa `interests` con índice único, soft withdraw y eventos `interest.expressed`/`interest.withdrawn` en colección `outbox` del dominio Engagement. En GET, propietario recibe solamente interesados activos con tarjeta pública, orden/cursor, acciones y `private, no-store`; ID de perfil separado de ID de cuenta.
Prueba de seguridad/HTTP `Interes_se_expresa_lista_retirada_y_deja_eventos_durables` pasó 1/1 con Mongo replica set y Redis en Testcontainers: requiere autorización, idempotencia POST, no distingue recurso ajeno, GET propietario, no expone teléfono/correo/punto exacto, cache privado, retiro lógico y ambos eventos. Fallos revelaron y corrigieron mapping de ID perfil y colección modificada durante enumeración. Suites completas aún no ejecutadas en este incremento.
Límites: el outbox ya persiste, pero no hay consumidores para push/estadísticas/contador en Discovery. Dependencias de pedidos y conversaciones representan valores disponibles en sus colecciones y quedan sujetas a los bloques 08/10. Puente local a Discovery no tiene timeout/circuit breaker Polly del contrato; tratarlo como pendiente antes de declarar el bloque conforme completo. La prueba usa doble controlado para Content Safety y no demuestra Azure real. Aceptación humana pendiente.

### Incremento 07-interes (2026-11-10)
Estado local: POST/DELETE interest y GET interested conectados a Engagement; Host valida perfil, visibilidad/radio, propiedad y proyección pública mediante puertos de Discovery. Los IDs de interés son IDs de perfil prestador; JWT usa ID de cuenta. Mongo persiste `interests` con índice único/retirada lógica y guarda eventos `interest.expressed`/`interest.withdrawn` en el outbox compartido dentro de transacción local.
Evidencia: `RequestEndpointsTests` 12/12 aprobadas con Mongo replica set y Redis Testcontainers. Incluye idempotencia, GET propietario/ajeno, privacidad, cache `private, no-store`, retirada y eventos. Prueba focalizada repetida después del cambio transaccional: 1/1 aprobada. Build Host Release: 0 warnings/errors.
Límites: no hay consumidores outbox para push/estadísticas/contador; integración con pedidos/conversaciones depende de bloques 08/10. Falta Polly timeout/circuit breaker de 2 s para puente Discovery. La moderación se sustituye por fake en la prueba, Azure no validado. Quedan negativos de visibilidad/perfil/nota/orden/cursor/dependencia y aceptación humana; T-992 sigue abierto.

### Feed comunitario (2026-11-11)
Estado local: `GET /v1/discovery/feed` integrado a trabajos y perfiles de Discovery. Combina publicaciones aprobadas a <=3 km o de prestadores activos que atienden el punto; cursor ligado a coordenadas, orden UUID v7, marcas personales solo autenticado y salida pública sin coordenadas exactas ni datos del cliente. La app ya consume esta ruta.
Verificación: Release build de solución 0 errores/0 advertencias; `FeedTests` 4/4. Prueba Mongo/Testcontainers agregada y compiló, pero su ejecución fue bloqueada por Docker no disponible (`npipe://./pipe/docker_engine`, acceso denegado). Sin prueba real de Atlas/Azure ni despliegue.
Deuda: caché Redis/L2; ejecución de prueba Mongo cuando Docker esté disponible. El botón Guardar requiere grupo 11 (`GET saved`, `POST/DELETE saved/{kind}/{id}`); contacto y reporte dependen del grupo 12. Recomendación ya existe en grupo 05. No confundir implementación local con prueba end-to-end productiva.
