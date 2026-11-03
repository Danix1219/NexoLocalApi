# Roles, pruebas y gates

El profesor separa planificación, construcción, seguridad, QA y documentación. Usamos esos roles sin obligar paralelismo. Cada incremento define matriz requisito→tarea→prueba→evidencia; no solo cobertura numérica.

Hoy: scripts/check_sdd.ps1 verifica documentos. Futuro backend: UnitTest/IntegrationTest/SecurityTest/DatabaseTest/ContractTest, adaptados a datos NoSQL reales y contratos. Futuro Flutter: test/unit, test/widget, integration_test y controles de seguridad con herramientas fijadas. No son carpetas ni suites creadas por esta entrega.

El 45% global y 80% delta advisory proceden de los ejemplos docentes y son propuesta de umbrales iniciales, por ratificar. La revocación, aislamiento, privacidad, concurrencia, moderación y borrado requieren escenarios explícitos aunque se alcance cobertura. Mantener evidencia de fuente real y mocks separados. Mutation/Stryker de C# no se traslada automáticamente a Dart.

Orden conceptual: dependencias fijadas→compilación/análisis→pruebas→contratos/seguridad→critic/endpoints/sdd→cobertura→análisis estático→artefacto→verificación de entorno. Pipeline ejecutable se crea al encargar implementación; no copiar workflows del profesor que apunten a sus proyectos.

## Aplicación por contexto

Herramientas: Codex y Claude Code; [roles secuenciales](../../docs_nexo/SDD_HERRAMIENTAS_IA.md). La revisión IA produce hallazgos; una persona real aprueba. Ready verifica fuentes, contrato de historia, decisiones bloqueantes, plan/tareas y criterios; DoD exige ejecución pertinente con evidencia.

Un gate documental no ejecuta producto. Unitario con doble, prueba de contrato, integración real con NoSQL/Redis/proveedor y recorrido Android se reportan por separado. Si faltan credenciales, una prueba simulada puede verificar lógica y deja la aceptación real pendiente. No aprobar SMS/moderación/ML/GenAI con heurísticas. Los umbrales docentes no se ratifican por instalar una skill.

La API verifica transiciones/aislamiento/concurrencia/outbox/revocación entre réplicas; Flutter verifica intención/estados, limpieza local, permisos, accesibilidad, reconexión y consumo del contrato. El recorrido conjunto enlaza resultados de ambos. Las carpetas de suites son objetivo futuro, no evidencia de creación.

## Preparacion estructural 0.4.1

Las 26 specs integran FR-950/NFR-950 → T-950 → AC-950; Ready espera esa preparacion. Consolidacion de plantilla y referencias tienen dueño nv-01; cambio a CI de producto, nv-03; activacion Jira/Confluence, nv-00 (datos pospuestos). [Orden verificable](orden-desarrollo-backend.md) y [mapa backend](estructura-backend.md) describen entradas/salidas sin exigir el cierre completo de cada fase. No hay proyectos nuevos, tareas ejecutadas o Ready por esta actualizacion documental.
