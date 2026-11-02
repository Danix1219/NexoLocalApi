# CI documental inicial

Repositorio objetivo: https://github.com/Danix1219/NexoLocalApi.git. Solo documentación/validadores, sin CI de aplicación ni CD. Workflow con checkout fijado a SHA, permisos contents:read y sin secretos. Dependabot revisa GitHub Actions; no configura dependencias de proyectos aún inexistentes.

El validador existente usa enlaces fuera del repositorio. sdd-context.zip conserva solo archivos documentales externos referenciados por Markdown de este checkout; context-manifest.json fija sus hashes y revisión documental. Son fixtures congeladas para validar enlaces, no otra autoridad de contratos ni una copia de producto. Actualizarlas al cambiar la revisión compartida/contrato consumido; no asumir que representan HEAD de la contraparte. Los hashes detectan deriva, no sustituyen confianza/revisión del cambio.

verify-sdd.ps1 limita rutas del archivo, verifica hashes, ejecuta check_sdd y comprueba referencias/cobertura documental. No certifica semántica ni integración; la comprobación de ciclos de tareas de sesión previa no se declara parte de este wrapper. Si aparece .csproj/pubspec.yaml, falla hasta configurar CI real en ese cambio, evitando aprobar producto con solo lint.

El ruleset exige PR, conversaciones resueltas, check SDD / docs y bloqueo de force-push/borrado. Se activa tras verificar el primer check remoto. Por decision expresa del usuario, Danix1219 trabaja solo: required_approving_review_count=0; no hay revision independiente ni autoaprobacion. Incorporar un segundo revisor permitira exigir una aprobacion. Esta configuracion no sustituye la aceptacion humana de specs ni declara Ready. No hay bypass administrativo implicito.

Backend futuro: restore/build/test/contrato/integración y seguridad según proyectos reales. App futura: SDK/analyze/unit/widget/integration/dispositivo y seguridad. CD requiere cuentas/entornos/proveedores; no hay deploy ficticio.

Plan de preparación técnica y decisiones previas: [inicio y repositorios](../../docs_nexo/INICIO_DESARROLLO_Y_REPOS.md). Esta referencia puede estar congelada en el contexto CI de la revisión correspondiente.
