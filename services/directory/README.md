# S02 — Directory

Arquitectura: **vertical-slices**. Catálogo, edición y búsqueda cambian por funcionalidad; reglas de contacto y visibilidad permanecen en su slice.

Patrones: Separación lógica de consulta/comando; cache-aside solo público, índices y proyecciones por contrato.

Proyectos: Features, Adapters, Api.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas.

[Guía común y comandos](../../docs/base-microservicios.md).
