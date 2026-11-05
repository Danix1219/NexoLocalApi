# Checklist — ep-07

## DoD
- [ ] Requisitos, tareas, criterios y contratos conciliados con la constitución.
- [ ] La prueba de propósito recovery no sirve para registrar; al cambiar el secreto se revocan todas las sesiones y refresh vigentes.
- [ ] Evidencia distingue documental, doble y real; Memoria y checklist de PR actualizados sin secretos.

## Aprobación
- [ ] Revisor humano por asignar; fecha pendiente.
- [ ] Sin aceptación productiva por lint.

## Criterios por operación

- [ ] EP-006 `POST /auth/password-reset` / AC-004: contrato formal enlazado y revisado; evidencia positiva, negativa y de fallo adjunta. No marcar si solo está el catálogo.

## Integridad de alcance

- [ ] La operación no se implementa de nuevo aquí si otra spec la declara primaria.
- [ ] Secretos de base y proveedor viven en configuración protegida, no en el repositorio.
- [ ] AC-950 / T-950: rutas, dueño, decisiones, contrato y suites definidos en plan antes de Ready.
