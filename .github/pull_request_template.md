## Trazabilidad obligatoria

<!-- Conservar estas tres etiquetas exactas y colocar valores reales en texto plano.
No introducir enlaces ficticios, secretos ni texto interno sensible. Los repositorios son publicos.
No basta con adjuntar la documentacion en un comentario: el enlace debe estar en el cuerpo del PR.
Jira principal, clave de rama y titulo deben coincidir. Los campos vacios bloquean merge.
-->
Jira principal:
Jira:
Confluence:

## Cambio y motivo

Describir el problema, comportamiento anterior y resultado esperado. Enlazar en Confluence el alcance, decisiones, evidencia y registro de cambios de esta tarea. Para promociones/reintegraciones, usar la tarea coordinadora de entrega y su pagina de version.

## Alcance SDD y coordinacion API/app

- Spec y tarea local (rutas/IDs):
- Requisitos FR/NFR y criterios AC cubiertos:
- Contratos EP/eventos/version afectados; compatibilidad con la contraparte:
- PR relacionado del otro repositorio (si aplica):
- Fuera de alcance y decisiones pendientes:

## Validacion realizada

| Comando o recorrido | Entorno/version | Resultado real | Evidencia |
|---|---|---|---|
| Completar solo comprobaciones ejecutadas | | | |

Separar lint documental, unitarios con dobles, integracion real y dispositivo. Si no aplica una prueba, indicar por que. No marcar pruebas de producto por tener CI documental correcto.

## Control de cambios y entrega

- Rama origen y destino; tipo (trabajo / estabilizacion / promocion / reintegracion):
- Cambios incompatibles, datos/migraciones y tratamiento de seguridad/privacidad:
- Riesgos, mitigacion y forma de revertir (o motivo por el que no aplica):
- Version/candidato, artefacto y PR de reintegracion (si es entrega):
- Revisor humano y responsable de entrega cuando corresponda:
- Uso de IA y revision humana del resultado (sin secretos):

## Lista de revision

- [ ] Jira principal real; URL Jira y pagina Confluence actualizada incluidas arriba.
- [ ] Titulo y rama siguen la convencion Jira; destino respeta develop → release/* → main.
- [ ] Spec/plan/tareas/checklist, CHECKLIST_PR.md y Memoria conciliados.
- [ ] Permisos, aislamiento, consentimiento y compatibilidad API/app revisados segun alcance.
- [ ] Evidencia real registrada; sin firmas, pruebas ni aceptaciones inventadas.
- [ ] Revisor humano distinto del autor identificado para trabajo de equipo; pendiente explicito durante el arranque sin segundo revisor.
- [ ] SDD / docs, Git / flow y PR / traceability correctos antes de merge.

<!-- Guia versionada: docs/trazabilidad-jira-confluence.md en este repositorio.
Ninguna casilla es por si misma una aprobacion. Dependabot tambien necesita Jira y Confluence;
un mantenedor completa su titulo/cuerpo sin modificar la rama automatica.
-->

- [ ] Preparacion de rutas/propietario/entrada/suites trazada a la spec antes de Ready; traslado de plantilla y CI tienen dueño unico.
