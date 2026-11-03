# S06 — Trust

Arquitectura: **evaluation-pipeline**. La decisión combina políticas y proveedores externos; un fallo no puede aprobar contenido por omisión.

Patrones: Pipeline de evaluación con Policy/Strategy; decisión vinculada al contenido y adaptadores reemplazables.

Proyectos: Evaluation, Adapters, Api.

La estructura cambia según la responsabilidad. Los proyectos de núcleo no dependen de framework/proveedores; adaptadores y hosts quedan en el borde. No hay referencias a proyectos de otros propietarios.

Base física sin negocio; patrones y contratos pendientes de implementación/pruebas.

[Guía común y comandos](../../docs/base-microservicios.md).
