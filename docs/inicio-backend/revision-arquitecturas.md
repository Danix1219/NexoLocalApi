# Evidencia de revisión de arquitectura interna

Fecha: 2026-11-03. Encargo del usuario: revisar casos de uso y aplicar arquitectura adecuada por servicio, combinada con los patrones existentes. [Decisión vigente](../arquitectura/seleccion-arquitecturas.md).

## Cambio ejecutado

Solución única `nexolocal.Api.slnx`, G01 original sin sustitución; 28 proyectos. S01/S04 conservan cuatro capas por sus invariantes; S02 Features, S03 Modules, S05 Core, S06 Evaluation, S07 Delivery y S08 Projections, con Adapters y hosts propios. S07 mantiene Worker independiente. La revisión sustituye la uniformidad de los 33 proyectos anteriores; no modifica las 101 operaciones, sus dueños o el carácter condicional de S08.

Se actualizaron referencias, solución, Dockerfiles de cada host, registro scaffold.json, política de arquitectura, catorce pruebas del verificador, perfiles y preparación de specs. Se conserva estructura previa fuera de la solución en `artifacts/architecture-before-61436060a1ae42f8a306ecfd7de42ffb` en la raíz física, ignorada por Git. No contiene un nuevo repositorio. Sin dependencias de proveedores ni paquetes nuevos.

## Resultados ejecutados

Desde `C:/Users/kevin/source/repos/nexolocal.Api`, SDK 10.0.302:

- `dotnet restore nexolocal.Api.slnx --source artifacts/empty-feed --force --verbosity minimal -m:1 -nodeReuse:false`: PASS, 28 proyectos, packs locales sin proveedores nuevos.
- `dotnet build nexolocal.Api.slnx --no-restore -c Release -m:1 -nodeReuse:false --verbosity minimal`: PASS, 0 errores y 0 advertencias.
- `python -B scripts/check_architecture.py`: PASS. Verifica grafo, rutas y proyectos de la solución, no todos los usos de tipos o límites internos entre módulos.
- `python -B -m unittest discover -s tests/Architecture -p 'test_*.py'`: catorce pruebas PASS; incluye rechazo de dependencias de Features a Adapters, Evaluation a proveedor/Adapters y Delivery a Worker.
- `scripts/Test-ScaffoldHosts.ps1`: PASS, nueve hosts HTTP simultáneos devuelven 404 esperado y Worker S07 permanece activo. Se detuvieron solo procesos propios. Logs locales: `artifacts/smoke/d676c6cff47044118c154ce5e9dd2d4c`.
- Revisión estática: 28 proyectos sin PackageReference, diez Dockerfiles con orígenes COPY existentes y perfiles físicos concordantes con SDD; inventario sigue con 101 operaciones.
- Lint SDD: 26 specs PASS. Integridad: 484 tareas/467 requisitos/322 criterios PASS. Ocho pruebas del validador PASS. `git diff --check` sin errores de espacios.

## Pendientes y límites

No hay negocio implementado, pruebas de producto, conexiones a DB/broker, proveedor de moderación/push, rutas proxy ni mejoría de rendimiento medida. Las métricas y criterios de evolución están en la decisión. Imágenes Docker y CI remoto no se ejecutaron; consolidación del código al checkout SDD y revisión humana siguen pendientes. Jira/Confluence pospuestos y sin nueva publicación. No se marca Ready o cierre humano por estas comprobaciones.
