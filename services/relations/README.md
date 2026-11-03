# S08 — Relations

Arquitectura: **read-projections**. Consulta derivada y reconstruible sin autoridad de escritura o permisos de otros servicios.

Patrones: Vista materializada, consumidores idempotentes y control de versiones/tombstones.

Proyectos: Projections, Adapters, Api.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas. S08 continúa condicional.

[Guía común y comandos](../../docs/base-microservicios.md).
