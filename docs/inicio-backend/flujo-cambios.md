# Flujo de cambios y excepciones de nv-00

Este flujo usa [registro de decisiones](../decisions/registro.md), [orden backend](../orden-desarrollo-backend.md), [Git del equipo](../flujo-git.md) y [politica Jira/Confluence](../trazabilidad-jira-confluence.md). Constitucion y contratos API conservan autoridad.

## Cambio normal

Solicitud/fuente → identificar dueño y spec → analizar impacto API/app/contrato/datos/seguridad/CI → resolver/registrar decision → refinar spec/plan/tareas/AC y rutas → revision humana/Ready → ejecutar SOLO alcance encargado → validacion/evidencia → PR a develop → release a main y reintegracion si corresponde.

Mientras los datos Jira/Confluence esten pospuestos, la solicitud/documentos se mantienen locales y el ticket/enlace quedan pendientes. No convertir el nombre temporal de rama en una excepcion permanente. Cuando existan datos, asignar tarea real y renombrar rama antes de publicar. Una modificacion de contrato solicita coordinacion con el consumidor y version/compatibilidad; no fuerza desplegar app y API simultaneamente.

## Decision rechazada, incierta o cambiada

Si la fuente se contradice, registrar las dos referencias, diferencia y alcance afectado; detener solo el paso dependiente. Si una opcion no esta ratificada, conservar estado propuesto y presentar alternativas/impactos. Si cambia una decision ya ratificada, crear revision/ADR que la sustituya con motivo y plan de compatibilidad; no editar el historial como si la decision anterior nunca hubiera existido.

## Excepcion

Solicitar motivo, riesgo, mitigacion, responsable, vencimiento/condicion de reapertura y evidencia de autorizacion. Sin datos/aprobacion real sigue como propuesta. No hay excepcion implicita por urgencia, resultado de IA o CI verde. No omitir autorizacion por recurso, aislamiento o controles de secretos para cerrar una tarea.

## Fallo de validacion y rollback

Registrar comando, entorno, salida/exit y archivos/criterios afectados; corregir con alcance pequeño y repetir los checks pertinentes. No eliminar el verificador para poner verde un PR. En documentos, revertir selectivamente el cambio y conciliar enlaces/IDs/Memoria; en producto futuro, fijar rollback compatible con datos/contratos antes de liberar. No reescribir ni force-push a ramas compartidas.

## Trabajo con Codex y Claude Code

Una herramienta trabaja un cambio acotado; el traspaso incluye spec/historia, estado real, decisiones, archivos cambiados, validaciones ejecutadas y pendientes. Registrar herramienta que SI intervino, no atribuir ejecucion a Claude o a subagentes sin haberla realizado. El receptor vuelve a leer constitucion/AGENTS/Memoria/spec y verifica estado local. Ninguna herramienta firma aprobaciones o entrevistas.

Registro de esta intervencion: Codex prepara el paquete nv-00 el 2026-11-03; ejecucion secuencial, sin herramientas externas de mensajeria, delegacion, publicacion ni codigo de producto. La revision humana sigue pendiente.

## Revision y entrega

El autor adjunta evidencia accesible, el revisor contrasta fuente y alcance, y el responsable de entrega verifica dependencias/versiones. Confluence documenta ese cambio y Jira su trabajo; el SDD sigue versionado en API. No cerrar todas las tareas de una spec por completar un subset. Todo diferimiento identifica que puerta bloquea y que trabajo independiente puede continuar.
