# Constitución — API_NexoLocal

Versión: 0.3.0 documental. Estado: Borrador; pendiente de ratificación técnica. Fuente de verdad: `.specify/memory/constitution.md`.

## 1. Propósito y alcance

Backend por dominios G01/S01-S07 y extensión S08, con APIs y datos propios. Una spec es un incremento liberable, no un permiso para construir todos los servicios de golpe.

## 2. Principios

Fuentes vigentes: requisitos académicos, maqueta aprobada y hallazgos H01-H12. Seguridad, aislamiento por usuario, datos mínimos, fallos explícitos y evidencia verificable. Spec antes de cambio observable. Nunca inferir autorización de los alias Tú/me ni del modo de pantalla. No copiar los defectos de la maqueta como reglas permitidas.

## 3. Stack

Propuesta previa: .NET 10/YARP, MongoDB por propietario, Redis distribuido, Blob/CDN, colas con outbox/inbox. Azure y Neo4j siguen condicionados a ratificación/costo/ensayo.

Fijar versiones exactas y evaluar dependencias al preparar la primera implementación; no hay paquetes instalados por estos documentos.

## 4. Arquitectura

Microservicios por dominio; hexagonal es propuesta interna, no exigencia del ejemplo docente. Caso de uso y permisos en aplicación; adaptadores de datos/integraciones. Transacciones locales S04 para pedido/asignación, eventos versionados y outbox/inbox. Cada servicio escribe solo su base.

## 5. Contratos y datos

Autoridad de inventario: `API_NexoLocal/docs/contracts/inventario.json`. OpenAPI/AsyncAPI/SDL formales pendientes por incremento. docs/endpoints indexa, no sustituye los esquemas. IDs y dinero/fechas según contratos de Nexo, sin copiar prefijos SQL ni PascalCase legacy. Regla de teléfono/domicilio por estado y consentimiento.

## 6. Configuración y seguridad

Sin secretos en archivos, errores o logs. Configuración por entorno. Redis no es historial durable ni una caché expulsable para revocación. Moderar antes de persistir contenido; rechazar por omisión cuando no se puede comprobar autorización. CORS/headers web se aplican a superficies HTTP pertinentes; no inventar antiforgery de Razor para Flutter.

## 7. Gobernanza SDD

Flujo `specs/nv-XX/spec.md → plan.md → task.md → checklists/gates.md`. Un cambio pequeño por revisión, tareas con FR/NFR y aceptación verificable. Docs vivos en docs/, especificaciones los enlazan. Rama de trabajo futura según id de spec. Un revisor humano real; no copiar firmas del profesor. Borrador documental → refinada → lista para desarrollar → implementada con evidencia → aprobada; Borrador con evidencia solo tras ejecutar producto y documentar resultados.

Cada FR/NFR debe aparecer en una tarea definida y cada AC tener comprobación asignada. No citar tareas ausentes ni mantener dependencias circulares. La cobertura de diseño queda en tabla por spec; plan y checklist deben usar los mismos IDs. Las capacidades transversales tienen dueño único y los recorridos enlazan su integración.

## 8. Gates de calidad

Hoy solo lint documental. Al implementar: restore→build→unit→integration→security→database→contract→critic→endpoints→sdd→coverage→delta advisory→mutation advisory→SAST; definir herramientas y comandos reales en la fase de calidad. Cobertura 45% y delta 80% advisory se toman como base docente a ratificar; jamás sustituyen casos críticos de permisos. No crear CI ficticio ni indicadores verdes sin ejecución.

## 9. Entrega y operación

Nexo requiere CI/CD, nube, dos instancias HTTP, TLS, observabilidad, CDN y Play/landing, aunque los ejemplos pospongan hosting/CD y Redis. Costo cero se verifica con cuenta/región. Una publicación, pipeline o despliegue requiere el encargo correspondiente; esta entrega es documental.

## 10. Memoria

Memoria.md registra Estado, Decisiones con porqué, Deuda y A evitar por fase. Breve, sin datos sensibles. Una regla permanente se documenta en constitución/AGENTS. No confundir esta bitácora de proyecto con la memoria global del asistente.

## 11. Revisión y excepciones

Registrar diferencia con el profesor y razón de producto, manteniendo formato y secuencia. Ratificar stack/proveedores y responsable antes de Ready del primer incremento. Controles propuestos y escenarios de aceptación no son pruebas ejecutadas.

## 12. Herramientas y coordinación acordadas

El equipo utilizará Codex y Claude Code; OpenCode/WARP pertenece al ejemplo docente. AGENTS es el procedimiento común y CLAUDE es entrada de lectura, sin duplicar reglas. Ejecución secuencial y revisión humana; roles documentados no activan subagentes.

Aplicar el [acuerdo SDD](../../../docs_nexo/SDD_ALINEACION.md), [cobertura de requisitos](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [protocolo IA](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). La matriz de versiones precede al desarrollo; CI/CD y canal móvil se preparan desde primera vertical, mientras entrega cierra evidencia global. Mantener propuestas técnicas como tales hasta ratificar y resolver bloqueantes de la historia antes de Ready.

## Flujo Git del equipo (acuerdo 2026-11-02)

Aplicar [flujo Git](../../docs/flujo-git.md): ramas por tarea desde develop, PR a develop, candidato release/* y PR de release a main; reintegrar main a develop. main permanece principal. CI y PR obligatorios; revision independiente exigible al incorporar revisor real. No declarar Ready o aceptacion por publicar o fusionar documentacion.
