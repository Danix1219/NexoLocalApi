# Checklist — ep-05

## DoD
- [ ] Requisitos, tareas, criterios y contratos conciliados con la constitución.
- [ ] Un refresh usado dos veces revoca toda la familia; cerrar sesión no falla si ya estaba cerrada.
- [ ] Evidencia distingue documental, doble y real; Memoria y checklist de PR actualizados sin secretos.

## Aprobación
- [ ] Revisor humano por asignar; fecha pendiente.
- [ ] Sin aceptación productiva por lint.

## Criterios por operación

- [ ] EP-005 `POST /auth/refresh` / AC-004: contrato formal enlazado y revisado; evidencia positiva, negativa y de fallo adjunta. No marcar si solo está el catálogo.
- [ ] EP-007 `POST /auth/logout` / AC-004: contrato formal enlazado y revisado; evidencia positiva, negativa y de fallo adjunta. No marcar si solo está el catálogo.

## Integridad de alcance

- [ ] La operación no se implementa de nuevo aquí si otra spec la declara primaria.
- [ ] Secretos de base y proveedor viven en configuración protegida, no en el repositorio.
- [ ] AC-950 / T-950: rutas, dueño, decisiones, contrato y suites definidos en plan antes de Ready.
