# Consolidación y publicación en ramas

Encargo explícito del usuario del 2026-11-03: publicar todos los cambios pendientes en sus repositorios y colocar la solución en el backend.

La solución `nexolocal.Api.slnx` y sus 28 proyectos están ahora en la raíz del checkout API_NexoLocal, que conserva origin NexoLocalApi. G01 sigue en nexolocal.Api/; los ocho servicios y Worker siguen en services/. La fuente temporal fuera de Git se conserva como referencia local; la ubicación versionada es este repositorio.

Ramas a publicar: `chore/publicacion-base-backend` en NexoLocalApi y `chore/publicacion-alineacion-app` en NexoLocalApp. Son provisionales porque no se dispone de clave Jira ni página Confluence reales. No se abren/fusionan PR sin esos datos, no se simulan enlaces y no se cambian reglas remotas. La autorización de publicación actual amplía la espera de publicación anterior, no la aprobación de integración.

Backend: workflow sdd ejecuta primero el workflow reutilizable backend-base (restore/build/grafo/pruebas del verificador/smoke), y solo después documentación. Móvil: lint documental, sin producto Flutter; guard de manifiestos permanece. Ambos workflows se ejecutan en ramas de trabajo para verificar antes de PR. Las referencias docs_nexo ya publicadas se refrescan como snapshots documentales con hashes; no se amplía el payload a PDFs/XLSX o árboles completos de la contraparte. docs_nexo no tiene repositorio propio.

Los resultados reales de validación, commits, push y CI se reportan al terminar. Sin CD, nube, pruebas funcionales o revisión humana ficticia.

Verificación local previa: restore/build de la solución consolidada PASS, 0 errores/0 advertencias; arquitectura y catorce pruebas PASS; arranque simultáneo de nueve HTTP/Worker PASS. Pruebas SDD (ocho) y gobierno (39) PASS; wrappers aislados API/app PASS. Referencias documentales refrescadas conservando el alcance previo de 33/38 archivos. CI remoto se evalúa tras publicar.
