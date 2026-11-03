# Guion de evidencias previas y evaluacion de nv-00

Este guion prepara las tareas existentes; no acredita que se hayan realizado. No hay respuestas de entrevistas, participantes, consentimientos o aprobaciones fabricados. Su custodia final debe evitar PII/secretos en repositorios publicos.

## Entrevistas (T-451)

Antes de entrevistar: definir objetivo y perfil, explicar uso de respuestas, pedir consentimiento real y acordar retencion/acceso. Registrar fecha real e identificador seudonimo; mantener datos de contacto fuera del Git publico. Preguntar sobre problema, busqueda/contacto, privacidad de ubicacion, solicitudes/pedidos, confianza y fallos; pedir ejemplos sin inducir respuestas. Sintetizar hallazgos, contraste con requisito/spec y cambio propuesto. Evidencia necesaria: consentimiento apropiado, registro permitido, sintesis revisada y trazabilidad. Preparar este guion no cuenta como entrevista.

## Descripciones y matriz (T-452/T-453/T-454)

Revisar descripcion separada backend/movil y libro de componentes con su version: dueño, responsabilidad, entrada/salida, dato e integracion. Contrastar 13 componentes backend, 27 moviles y W01 contra mapa vigente. Para las 101 operaciones, revisar contrato/actor/estado/datos/indice/consulta; formalContract pendiente no se convierte en esquema ejecutable por existir una ficha. El archivo XLSX presente no acredita revision celda por celda en esta fase.

## Referencias docentes (T-455)

Registrar URL exacta y commit/revision de cada proyecto usado, seccion revisada, patron reutilizable y diferencia justificada para Nexo. Si no se dispone del enlace/commit, marcar pendiente y solicitarlo cuando sea necesario; no atribuir revision a una URL de ejemplo. No importar SQL/EF, TOTP o reglas de Razor como requisitos de Nexo.

## Recorrido interactivo (T-402)

Usar una tarea real del SDD y formular estas preguntas al evaluador/herramienta: cual es la fuente y version; que FR/NFR/AC cubre; quien posee el contrato/dato; que tarea y archivos lo implementaran; que caso negativo evidencia permiso/estado/fallo; que comprobacion se ejecuto y que queda pendiente. Guardar prompt y respuesta reales con fecha/herramienta, contrastarlos contra archivos y registrar correcciones. Este guion no incluye respuestas supuestamente ejecutadas ni una calificacion.

## Versiones y herramientas (T-411/T-412/T-406)

La matriz compartida diferencia version documental, contrato y artefacto de producto. Fijar versiones de herramientas reales al ratificar el primer scaffold; no llamar release al SDD. MCP es opcional: si se adopta, documentar origen/version/finalidad/permisos antes de usarlo; su ausencia no bloquea el producto y no se instala por esta guia. Registrar uso real de IA y revision humana, nunca una lista de roles como prueba de ejecucion.

## Ampliación de la base por encargo posterior

Consultar [base de todos los microservicios y evidencia ejecutada](base-todos-microservicios.md). La solución original contiene ahora 33 proyectos: G01, S01–S08 y Worker S07; sin negocio. Este informe actualiza el alcance previo de G01/S01; S08 sigue condicional.
