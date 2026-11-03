# CHECKLIST_PR — API_NexoLocal

- [ ] Spec conciliada con el cambio y constitución; bloque de aprobación con estado honesto.
- [ ] Plan con archivos reales, contratos, riesgos y orden.
- [ ] task.md con cada tarea →FR/NFR, aceptación y dependencias.
- [ ] Docs vivos actualizados; sin esquemas duplicados.
- [ ] Lint SDD documental ejecutado y resultado registrado.
- [ ] Build/analyze y suites pertinentes ejecutadas si existe código; si no, indicar no aplica.
- [ ] Seguridad, cobertura y contratos verificados cuando corresponde; pendientes explícitos.
- [ ] Memoria actualizada sin secretos.
- [ ] Revisor humano identificado; ninguna firma inventada.
- [ ] Evidencia enlazada y versión/commit cuando exista Git y producto.

## Alineación API y app

- [ ] Contexto/spec/T-ID/FR-NFR y AC identificados, incluida AC-090 cuando aplique.
- [ ] Contraparte y contrato/versión enlazados; compatibilidad y permisos revisados.
- [ ] Decisiones abiertas, dependencia y evidencia documental/doble/real diferenciadas.
- [ ] Herramienta IA y traspaso registrados sin secretos; revisión humana pendiente hasta identificar persona y fecha.

- [ ] Tabla de cada spec cubre FR/NFR y AC; todos los T-ID citados existen y sus dependencias no son circulares.
- [ ] Si se añade spec, actualizar índice, contraparte, dueño de capacidad y dependencia de cierre de entrega.

## Estructura y secuencia de la historia

- [ ] Propietario, archivos actuales/objetivo, contrato/decision de entrada y suites definidos antes de Ready.
- [ ] Consolidacion de plantilla, referencias y transicion CI enlazan dueño de fundacion/calidad; no se duplican en la funcionalidad.
- [ ] python -B scripts/check_sdd_integrity.py y lint documental revisados; si hay proyecto nuevo, CI real en el mismo cambio, sin declarar producto por lint.
