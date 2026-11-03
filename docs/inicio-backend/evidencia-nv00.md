# Evidencia y pendientes del arranque nv-00

Fecha: 2026-11-03. Registro de preparacion documental; no firma de Ready, entrevistas, revisiones docentes o producto. [Paquete de trabajo](README.md) y [registro de decisiones](../decisions/registro.md).

## Evidencia por criterio/tarea

| Tarea / criterio | Artefacto disponible | Resultado y limite |
|---|---|---|
| T-006 / AC-090 | README de arranque y fuentes inventariadas | Alcance/fuentes/puertas preparados; revision semantica humana pendiente |
| T-020/T-010/T-021 / AC-100 | Registro D01-D08 y flujo de cambios/excepciones | Distingue hecho, propuesta y diferimiento; no resolucion de decisiones ni excepciones aprobadas |
| T-950 / AC-950 | README y plan con rutas reales de documentos | Entrada/salida/suites concretas para gobierno; no scaffold ni aceptacion |
| T-001 / AC-001 | Spec/plan/tareas/checklist y paquete | Ready pendiente de revision real de Danix1219 (designado provisionalmente, sin independencia) y ratificacion; contratos propios N/A por no tener endpoints |
| T-002/T-003/T-004/T-007/T-005 / AC-001/AC-002/AC-090/AC-100 | Documentos y procedimiento de revision/cierre | Borradores preparados y checks locales; integracion global, seguridad/revision y cierre humano no firmados |
| T-402 / AC-402 | Guion de evaluacion interactiva | Prompt/preguntas preparados; respuestas reales/evaluacion pendientes |
| T-405 / AC-405 | Flujo SDD y scripts existentes | Validacion documental disponible; CI de producto/DevSecOps completo no ejecutados |
| T-406 / AC-406 | Flujo de traspaso IA y esta intervencion Codex | Trabajo secuencial documentado; no Claude/subagentes ni aprobacion humana atribuida |
| T-411 / AC-411 | Matriz compartida SDD_VERSIONES | Se corrigen versiones documentales; versiones exactas de primer scaffold/proveedores aun por ratificar |
| T-412 / AC-412 | Politica MCP del guion | Opcional; no se instala/adopta MCP por redactar la politica |
| T-418 / AC-418 | Informe previo FLUJO_GIT_EQUIPO | URLs/controles previos registrados; accesos de equipo, carpeta compartida y revisor pendientes. Sin refresco remoto en esta fase |
| T-451 / AC-451 | Guion de entrevista y manejo de evidencia | Preparacion, no participantes/consentimiento/respuestas reales |
| T-452 / AC-452 | Documentacion de arquitectura/cobertura separada API/app | Fuentes disponibles; conciliar descripcion con revision humana sin asumir aceptacion movil |
| T-453 / AC-453 | Libro XLSX y mapa previo 13/27 componentes | Archivo inventariado; no nueva comprobacion de cada celda |
| T-454 / AC-454 | Catalogo 101 operaciones y fichas existentes | Entradas/reglas disponibles; todos formalContract pendientes y semantica por revisar |
| T-455 / AC-455 | Guion de referencias docentes | URL/commit exactos y evidencia de lectura pendientes, no inventados |
| T-456 / AC-456 | Constitucion, cuatro piezas SDD y guias | Estructura presente; ratificacion y revision humana pendientes |
| T-951 / AC-951 | Plantilla/politica/workflow Jira-Confluence preparados | Datos reales y activacion pospuestos; no nuevas publicaciones o reglas remotas |

## Registro de fuentes

[inventario-fuentes.json](inventario-fuentes.json) contiene rutas relativas a la raiz API, presencia, SHA-256 y alcance de lectura. El hash prueba la revision del archivo inventariado, no que se haya evaluado todo su contenido o ratificado sus reglas. El JSON es evidencia local de nv-00, no reemplaza .ci/context-manifest.json ni obliga a cargar binarios de fuentes en un nuevo pipeline. Si una fuente cambia, regenerar la revision con motivo; no sobrescribir la evidencia de una validacion anterior como si fuera actual.

## Controles que se ejecutan para esta preparacion

Lint documental, integridad/ciclos locales y las pruebas del verificador. Resultado final se anade tras correrlos; los checks no certifican privacidad real de producto, disponibilidad de servicios, semantica completa o revisiones humanas. Ninguna tarea se marca aceptada por este registro.

## Pendientes con salida concreta

- Danix1219 es el revisor provisional ya designado por el usuario; falta su revision/decision real antes de Ready formal. El ratificador de cada decision tecnica debe confirmarse sin suponer que su rol GitHub lo determina.
- D01 y politicas/contratos necesarios antes del primer scaffold/identidad; D03/D06 esperan la etapa que los usa, no se eligen por inferencia.
- Datos Jira/Confluence cuando el usuario los facilite; entonces completar titulo/cuerpo/rama reales y aplicar el flujo de publicacion/verificacion acordado.
- Evidencias academicas autenticas por tarea para el cierre global. Este paquete no las reemplaza.

Entrega a la siguiente etapa: diseño de limites/rutas/propietarios y lista de decisiones/entradas pendientes para nv-01, contratos nv-02 y gates nv-03. No se entrega un proyecto reorganizado ni un servicio listo para produccion.

## Resultado ejecutado de esta preparacion (2026-11-03)

- Lint: SDD-OK, 26 specs, exit 0.
- Integridad: SDD-INTEGRITY-OK, 474 tareas / 448 requisitos / 312 criterios, sin duplicados/referencias ausentes/ciclos locales detectados; orden Ready/preparacion comprobado.
- Pruebas del verificador: 8 tests, OK.
- Wrapper documental completo en copia aislada: SDD-OK, SDD-TRACE-OK y SDD-INTEGRITY-OK; ocho tests OK. Sin extraccion sobre carpetas vivas.
- Inventario local: 9 fuentes presentes con SHA-256 coincidente; no se infiere lectura integral PDF/XLSX.
- Recorrido documental ejecutado: constitucion → nv-00/FR-100 → T-020 → AC-100 → registro de decisiones → checklist. Se comprobaron referencias y artefacto; no aprobacion semantica ni ejecucion interactiva de producto.
- git diff --check: correcto.

Revisor provisional designado: Danix1219. Revision, fecha de aceptacion y ratificacion tecnica: pendientes. No nueva publicacion/CI remoto, implementacion, entrevistas o despliegue. La salida preparada es el paquete local revisable, no Ready formal ni cierre completo de nv-00.
Actualizacion 2026-11-03 por limpieza IA: inventario conserva previousSha256 y motivo del cambio de constitucion al retirar la referencia operativa descartada. No cambia la ratificacion pendiente ni la evidencia historica de la validacion anterior.
