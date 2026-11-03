# S05 — Messaging

Arquitectura: **ports-adapters-realtime**. Escritura durable y recuperación de historial se separan del transporte efímero de tiempo real.

Patrones: Comandos/consultas, secuencia por conversación, deduplicación y fan-out posterior al commit.

Proyectos: Core, Adapters, Api.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas.

[Guía común y comandos](../../docs/base-microservicios.md).
