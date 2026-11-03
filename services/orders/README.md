# S04 — Orders

Arquitectura: **ddd-hexagonal**. Asignación y pedido requieren transiciones, permisos y concurrencia bajo una frontera atómica local.

Patrones: Agregados y máquina de estados, versión optimista, idempotencia y transactional outbox.

Proyectos: Domain, Application, Infrastructure, Api.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas.

[Guía común y comandos](../../docs/base-microservicios.md).
