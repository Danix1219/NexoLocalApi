# S01 — Identity

Arquitectura: **hexagonal**. Sesiones, OTP y borrado requieren invariantes aisladas y proveedores reemplazables.

Patrones: Rotación atómica, revocación durable, outbox y recuperación hacia adelante.

Proyectos: Domain, Application, Infrastructure, Api.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas.

[Guía común y comandos](../../docs/base-microservicios.md).
