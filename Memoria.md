# Memoria — API_NexoLocal

## Fase documental — adaptación al profesor (2026-11-02)
Estado: estructura y borradores documentales creados; sin implementación, compilación, suites de pruebas, CI o despliegue. El lint documental se registra por separado después de ejecutarlo.
Decisiones: adoptar el formato de API_SDD: specs/nv-XX/spec.md → plan.md → task.md → checklists/gates.md. Mantener requisitos Nexo, Flutter/microservicios/NoSQL y fuente única de contratos. Revisores por asignar.
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
