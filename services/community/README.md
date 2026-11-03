# S03 — Community

Arquitectura: **feature-modules**. Publicaciones, reseñas, reacciones y guardados tienen reglas propias, con lecturas de feed que las combinan.

Patrones: Módulos locales, moderación previa, unicidad idempotente y proyecciones de métricas.

Proyectos: Modules, Adapters, Api.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas.

[Guía común y comandos](../../docs/base-microservicios.md).
