# Codex y Claude Code en este backend

Las herramientas activas son Codex y Claude Code. Se elimina la carpeta de la herramienta descartada; no se reutilizan perfiles o comandos incompatibles.

| Herramienta | Entrada | Reglas a leer |
|---|---|---|
| Codex | [AGENTS.md](../AGENTS.md) | Constitucion, Memoria y las cuatro piezas de la spec objetivo |
| Claude Code | [CLAUDE.md](../CLAUDE.md) | Remite a AGENTS y a las mismas autoridades; sin otra constitucion |

[Procedimiento comun API/app](../../docs_nexo/SDD_HERRAMIENTAS_IA.md). [Arranque nv-00](inicio-backend/README.md).

Antes de editar: identificar spec/tarea, archivos permitidos, estado y pendientes. Trabajar secuencialmente; documentar herramienta que intervino y validacion real. Al traspasar: entregar archivos cambiados, fuente/decision, comprobaciones y deuda; el receptor vuelve a leer las autoridades. No habilitar subagentes, instalaciones, red o permisos por una tabla de roles.

AGENTS y CLAUDE son las entradas de este proyecto. No se crean carpetas .codex/.claude con archivos vacios ni configuraciones supuestas para reemplazar un README antiguo. Configuracion adicional solo cuando exista una necesidad real y se verifique compatibilidad del cliente. La presencia de instrucciones no acredita que se hayan cargado ni que haya skills instaladas.

Mantener una sola version de reglas permanentes y conservar Memoria como registro historico. Las referencias historicas del material docente o del inventario de publicacion no son configuracion activa y no se reescribe su evidencia.
