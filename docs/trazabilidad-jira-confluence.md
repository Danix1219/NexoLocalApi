# Jira, Confluence y control de cambios

Esta politica aplica por igual a API y app. Complementa el flujo develop → release/* → main y hace obligatorio que cada PR enlace la documentacion del cambio en Confluence. El SDD versionado conserva la autoridad tecnica y trazabilidad de spec/FR/NFR/T/AC; Confluence documenta contexto, decisiones, evidencia y registro de cambios; Jira controla trabajo/responsables/estado. Mantener enlaces en ambos sentidos, sin copiar contratos como otra autoridad incompatible.

## Configuracion real requerida antes de activar

Completar `.github/change-policy.json` con claves Jira admitidas (pueden existir proyectos diferentes API/app), URL base Jira y URL base Confluence. Ejemplo conceptual, NO datos reales: claves ["NEXO"], Jira https://equipo.atlassian.net y Confluence https://equipo.atlassian.net/wiki. Jira instalado bajo /jira y Confluence bajo /confluence tambien se admiten al configurar esas bases. Actualmente los campos vacios fallan de forma explicita; no se ha inventado un proyecto, tarea, dominio ni pagina real.

Faltan la URL del proyecto Jira, el dominio Confluence y una pagina real que documente este cambio, solicitados al usuario. La rama temporal `chore/trazabilidad-jira-confluence` es preparacion local sin publicar; antes de abrir su PR se renombra con la clave de su tarea real. No es una excepcion permanente ni un ticket inventado.

## Nomenclatura de ramas

`<tipo>/<CLAVE-JIRA>-<descripcion-en-minusculas-con-guiones>`

| Tipo | Finalidad | Titulo/commit habitual |
|---|---|---|
| feature | Funcion nueva | feat |
| fix | Correccion | fix |
| docs | Documentacion | docs |
| chore | Mantenimiento | chore |
| refactor | Reorganizacion sin cambiar comportamiento | refactor |
| test | Pruebas | test |
| ci | Automatizacion/pipelines | ci |
| build | Dependencias/empaquetado | build |
| perf | Rendimiento | perf |
| revert | Reversion | revert |

Ejemplos exclusivamente ilustrativos: `feature/NEXO-123-nv-16-recuperacion` (API), `feature/NEXO-124-017-cuenta` (app), `fix/NEXO-125-evitar-duplicados`, `docs/NEXO-126-contrato-identidad`. NEXO-123 y demas numeros NO son tickets creados o confirmados. La clave va en MAYUSCULAS; descripcion en minusculas sin espacios, tildes, nombres personales o carpetas anidadas. Una rama representa una tarea principal; el ID local de spec se conserva en la descripcion cuando corresponda y siempre en el PR.

Ramas estables main/develop no se renombran. Entregas agrupan tareas y usan `release/<version>` (ejemplo futuro release/0.1.0 o release/0.1.0-rc.1); su tarea coordinadora Jira y pagina Confluence son obligatorias en titulo/cuerpo del PR. Reintegracion main→develop usa esa misma tarea coordinadora. No se cambia el flujo para enviar trabajo directo a main.

Dependabot conserva su rama automatica, pero un mantenedor debe asociar una tarea Jira real y documentacion Confluence, y editar titulo/cuerpo. No hay exencion de enlaces para bots, documentacion, entregas o reintegraciones. Una misma pagina puede servir a ambos repositorios si describe explicitamente ambos cambios y enlaza sus PR; no basta con enlazar la portada del espacio.

## Titulo, commits y cuerpo de PR

Titulo: `<tipo>(<ambito>): <CLAVE-JIRA> <resumen>`; ambito opcional. Ejemplo ilustrativo: `feat(cuenta): NEXO-123 recuperar acceso`. La clave del titulo, Jira principal y rama de trabajo debe coincidir. Commits propios usan el mismo formato y clave para facilitar trazabilidad; su formato es convencion de equipo, no una validacion de todo el historial legacy/merge.

Conservar exactamente estas etiquetas de la plantilla y rellenarlas con valores reales:

```text
Jira principal: <CLAVE-JIRA>
Jira: <URL-HTTPS-del-ticket-/browse/CLAVE-JIRA>
Confluence: <URL-HTTPS-de-la-pagina-del-cambio>
```

No colocar los enlaces solo en comentarios del PR, HTML oculto, bloques de codigo o capturas. La pagina debe pertenecer al Confluence configurado y ser una pagina/permalink, no el sitio o espacio. Se admiten paginas spaces/.../pages/id, permalink pages/viewpage.action?pageId=..., display/ESPACIO/pagina y enlaces cortos x/... bajo la base configurada.

La plantilla tambien pide antes/despues, motivo, alcance SDD, contratos/compatibilidad, PR contraparte, pruebas realmente ejecutadas, riesgos/rollback, revisor y control de entrega/reintegracion. Conservar pendientes explicitos; no marcar revision o pruebas por publicar documentos.

## Contenido minimo de la pagina Confluence

- Clave/titulo Jira y enlaces al ticket, PR y spec/plan/tareas/checklist versionados.
- Problema/alcance y comportamiento esperado; decisiones con fecha/responsable y dependencias.
- Cambio API/app, contratos/versiones afectados, permisos/datos y compatibilidad.
- Evidencia de validacion con comando/recorrido, entorno, resultado y limitaciones.
- Riesgos, mitigacion y rollback o motivo de no aplicacion.
- Registro de cambios: fecha, clave Jira, PR/commit, descripcion, responsable y estado real. Actualizar con el commit final tras merge; no anticipar aceptacion.

El autor actualiza su pagina antes de solicitar revision; el revisor comprueba acceso, vigencia y que documente ESE cambio. Al cerrar Jira, enlazar PR/commit y evidencia. Un PR aprobado o fusionado no equivale a producto desplegado ni a cierre automatico de todas las tareas. GitHub for Jira requiere conexion administrativa independiente; este cambio no instala integraciones, transiciones automaticas ni envia datos a Atlassian.

## Validacion y activacion segura

Workflow pr-governance.yml: `PR / traceability` responde a opened/synchronize/reopened/edited/ready_for_review en PR a main/develop/release. Lee el evento como JSON, no inserta titulo/cuerpo en comandos y no realiza peticiones a Jira/Confluence. pull_request_target usa unicamente checkout del SHA de confianza de la rama predeterminada; token contents:read, sin credenciales persistentes, sin ejecutar codigo de la rama del PR. Las pruebas usan fixtures sinteticas .test, no tareas/paginas reales.

El check valida configuracion, campos visibles no duplicados, URLs HTTPS del dominio/base configurados, enlace directo al ticket, pagina Confluence y coincidencia clave/titulo/rama/ruta. Un campo vacio, pendiente, URL de otro dominio, ticket distinto, enlace en comentario/bloque o rama sin clave falla. No comprueba por HTTP que el ticket o pagina existan, permisos de lectura, contenido o calidad de su documentacion; eso corresponde a revision humana. No se presenta el check como prueba de producto.

Activacion pendiente: completar datos reales y la pagina de este cambio; probar local; publicar por PR a develop, promover por release versionada a main y reintegrar. Como el workflow de metadatos se toma de main de confianza, no se anade como requerido antes de que este publicado y exista una ejecucion real correcta. Para el arranque, verificar manualmente esos mismos metadatos sin inventar enlaces. Despues activar PR / traceability en los tres rulesets de AMBOS repositorios (integration_id GitHub Actions 15368), sin eliminar SDD / docs ni Git / flow, y comprobar con PR real que falta de Confluence bloquea y que editar titulo/cuerpo reejecuta. Los JSON propuestos ya incluyen el nuevo check; no son prueba de activacion remota.

No aplicar una regla remota que bloquee toda la colaboracion por configuracion vacia ni introducir excepciones silenciosas para fusionar este cambio. Los PR ya abiertos deben completar Jira/Confluence al activar el control; los historicos fusionados conservan su evidencia original, sin reescritura. Cambios futuros de politica requieren PR, revision humana y evidencia. Revisores obligatorios siguen pendientes de incorporar al primer colaborador real conforme al flujo Git.
