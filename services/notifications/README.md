# S07 — Notifications

Arquitectura: **event-driven-delivery**. El registro/consulta HTTP y la entrega asíncrona tienen tiempos y fallos distintos.

Patrones: API y Worker separados, inbox durable, planificación, reintentos acotados y cola de errores.

Proyectos: Delivery, Adapters, Api, Worker.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas.

[Guía común y comandos](../../docs/base-microservicios.md).
