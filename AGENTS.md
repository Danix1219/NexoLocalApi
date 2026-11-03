# AGENTS — API_NexoLocal

Rector: [.specify/memory/constitution.md](.specify/memory/constitution.md). Este archivo dice cómo trabajar; la constitución qué reglas respetar.

## 1. Forma de trabajo
Leer constitución, este archivo, Memoria y la spec objetivo. Seguir `specs/nv-XX/spec.md → plan.md → task.md → checklists/gates.md`. El encargo posterior del 2026-11-03 permite carpetas y proyectos base .NET en nexolocal.Api, sin logica de negocio. Conservar limites de cada fase; no crear funcionalidad, dependencias de proveedores o nube fuera de encargo.

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
Herramientas del equipo: Codex y Claude Code. Ver [procedimiento compartido](../docs_nexo/SDD_HERRAMIENTAS_IA.md) y [acuerdo de alineación](../docs_nexo/SDD_ALINEACION.md). Roles documentados no activan herramientas ni subagentes. Ejecutar secuencialmente salvo autorización explícita para delegar. Mínimo privilegio y revisión humana.

## 8. Fuentes y coordinación
Leer [fuentes y brechas](../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y la spec de la contraparte cuando cambie una frontera. Conservar contratos en API y consumo/pantallas en app. Resolver decisiones bloqueantes antes de Ready. Consultar [skills recomendadas](../docs_nexo/SDD_SKILLS.md) sin instalarlas ni ampliar permisos automáticamente.

## 9. Integridad local de la estructura
Leer [estructura SDD](docs/estructura-sdd.md) y [trazabilidad constitucional](docs/trazabilidad-constitucion.md). Verificar que cada FR/NFR tenga tarea y cada AC comprobación, que todos los T-ID citados existan y que las dependencias locales no formen ciclos. El validador actual no garantiza esas propiedades por sí solo; registrar revisión adicional sin presentarla como prueba de producto.

## Flujo Git del equipo (acuerdo 2026-11-02)

Aplicar [flujo Git](docs/flujo-git.md): ramas por tarea desde develop, PR a develop, candidato release/* y PR de release a main; reintegrar main a develop. main permanece principal. CI y PR obligatorios; revision independiente exigible al incorporar revisor real. No declarar Ready o aceptacion por publicar o fusionar documentacion.

## Trazabilidad de cambios Jira/Confluence (2026-11-02)

Aplicar [convencion de cambios](docs/trazabilidad-jira-confluence.md): rama de trabajo con clave Jira y PR con titulo/clave coincidentes, enlace al ticket y enlace obligatorio a la pagina Confluence del cambio. API y app conservan su autoridad SDD; Confluence no reemplaza contratos versionados. No inventar tickets, enlaces ni firmas; activar el check requerido solo con configuracion real y ejecucion verificada.

## Preparacion estructural en el SDD

Seguir [orden backend](docs/orden-desarrollo-backend.md). Antes de Ready cada historia concreta dueño, rutas actuales/objetivo, entrada contractual y suites/evidencia. Ejecutar tambien python -B scripts/check_sdd_integrity.py para IDs/cobertura/ciclos locales y orden de preparacion. Su PASS es documental; consolidar plantilla y crear CI de producto tienen dueño en fundacion/calidad, no en cada funcionalidad.

Para arrancar nv-00, usar [paquete de inicio](docs/inicio-backend/README.md) y [registro operativo de decisiones](docs/decisions/registro.md). Diferenciar entrada disponible, Ready humano y cierre global; los datos Jira/Confluence pospuestos no se inventan ni bloquean la preparacion documental independiente.
