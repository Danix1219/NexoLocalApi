# AGENTS — API_NexoLocal

Rector: [.specify/memory/constitution.md](.specify/memory/constitution.md). Este archivo dice cómo trabajar; la constitución qué reglas respetar.

## 1. Forma de trabajo
Leer constitución, este archivo, Memoria y la spec objetivo. Seguir `specs/nv-XX/spec.md → plan.md → task.md → checklists/gates.md`. Esta fase solo documentación/validadores; no crear aplicación, dependencias o nube sin encargo posterior.

## 2. Memoria y cierre
Actualizar Memoria al cerrar fase: Estado/Decisiones/Deuda/A evitar. Conciliar spec/plan/tareas/checklist con archivos y resultados reales. No firmar sin revisor humano identificado.

## 3. Límites
No tocar API_SDD ni WebSDD. Sin secretos ni resultados inventados. Sin acceso directo desde Flutter a MongoDB/Neo4j. No copiar SQL/EF, TOTP, cookies Razor, una sola instancia, ni exclusión de Redis/CD como si fueran requisitos Nexo.

## 4. Verificación
Ejecutar `scripts/check_sdd.ps1` desde esta carpeta. Su PASS solo acredita estructura. Comandos de build/test se definen con rutas reales al crear proyectos; no usar comandos del profesor contra carpetas inexistentes.

## 5. Contratos y docs vivos
API posee contratos y rutas; app registra consumo/pantallas y enlaza la autoridad. FR/NFR se identifican localmente por spec; toda tarea incluye referencia y criterio. No añadir operaciones inventadas para cada botón.

## 6. Seguridad y pruebas
Cubrir H01-H12 pertinentes, aislamiento, consentimiento, estados y reintentos. Distinguir dobles unitarios de integración real. La moderación o SMS simulados no aprueban aceptación productiva.

## 7. Roles con IA
Herramientas del equipo: Codex y Claude Code. Ver [procedimiento compartido](../docs_nexo/SDD_HERRAMIENTAS_IA.md) y [acuerdo de alineación](../docs_nexo/SDD_ALINEACION.md). `.opencode/README.md` es solo referencia histórica. Roles documentados no activan herramientas ni subagentes. Ejecutar secuencialmente salvo autorización explícita para delegar. Mínimo privilegio y revisión humana.

## 8. Fuentes y coordinación
Leer [fuentes y brechas](../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y la spec de la contraparte cuando cambie una frontera. Conservar contratos en API y consumo/pantallas en app. Resolver decisiones bloqueantes antes de Ready. Consultar [skills recomendadas](../docs_nexo/SDD_SKILLS.md) sin instalarlas ni ampliar permisos automáticamente.

## 9. Integridad local de la estructura
Leer [estructura SDD](docs/estructura-sdd.md) y [trazabilidad constitucional](docs/trazabilidad-constitucion.md). Verificar que cada FR/NFR tenga tarea y cada AC comprobación, que todos los T-ID citados existan y que las dependencias locales no formen ciclos. El validador actual no garantiza esas propiedades por sí solo; registrar revisión adicional sin presentarla como prueba de producto.
