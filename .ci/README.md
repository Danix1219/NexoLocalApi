# CI de documentación y base backend

La solución física está consolidada en la raíz junto con el SDD. `sdd.yml` ejecuta primero el workflow reutilizable `backend-base.yml`: restore, Release build, comprobación de referencias, catorce pruebas del verificador y smoke de nueve HTTP/Worker. El job documental espera `product`; no aprueba el scaffold solo con lint. `product-ci.json` delimita los 28 proyectos previstos y exige esa conexión de jobs; nuevos manifiestos requieren revisión de la política.

Estos checks comprueban estructura/compilación/arranque de hosts vacíos. No son pruebas de negocio, integración, seguridad funcional o despliegue. El móvil mantiene su guard documental hasta tener producto Flutter y CI real.

El validador existente usa enlaces fuera del repositorio. sdd-context.zip conserva solo archivos documentales externos referenciados por Markdown de este checkout; context-manifest.json fija sus hashes y revisión documental. Son fixtures congeladas para validar enlaces, no otra autoridad de contratos ni una copia de producto. Actualizarlas al cambiar la revisión compartida/contrato consumido; no asumir que representan HEAD de la contraparte. Los hashes detectan deriva, no sustituyen confianza/revisión del cambio.

verify-sdd.ps1 limita rutas del archivo, verifica hashes, ejecuta check_sdd y comprueba referencias/cobertura documental. No certifica semántica ni integración; desde la revision 0.4.1 este wrapper tambien ejecuta scripts/check_sdd_integrity.py y sus pruebas para ciclos LOCALES de tareas, IDs, cobertura y orden Ready/preparacion; no resuelve ciclos semanticos entre specs. Si aparecen manifiestos, exige la configuración explícita de CI del scaffold y el job previo; un pubspec.yaml sigue rechazándose en este repositorio.

El flujo vigente del equipo es feature/* → develop → release/* → main, con reintegracion main→develop y PR en ramas compartidas. Consultar [flujo Git](../docs/flujo-git.md). Los rulesets importables de .github describen main/develop/release; el resultado remoto se registra en el informe comun. SDD / docs y Git / flow son checks obligatorios. Aprobaciones=0 solo durante el arranque sin segundo colaborador; cambiar a 1 al incorporar al primer revisor. Sin revision independiente inventada ni bypass.

Backend futuro: restore/build/test/contrato/integración y seguridad según proyectos reales. App futura: SDK/analyze/unit/widget/integration/dispositivo y seguridad. CD requiere cuentas/entornos/proveedores; no hay deploy ficticio.

Plan de preparación técnica y decisiones previas: [inicio y repositorios](../../docs_nexo/INICIO_DESARROLLO_Y_REPOS.md). Esta referencia puede estar congelada en el contexto CI de la revisión correspondiente.

Control Jira/Confluence preparado: [guia de trazabilidad](../docs/trazabilidad-jira-confluence.md). El workflow PR / traceability y sus pruebas validan metadatos/URLs, no contenido remoto ni producto. change-policy.json permanece sin sitios/claves reales y falla explicitamente. Aun no se publica ni se activa como requerido; los JSON ruleset son propuestas hasta completar datos y verificar CI real.
