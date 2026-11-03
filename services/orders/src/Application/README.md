# NexoLocal.Orders.Application

Arquitectura: ddd-hexagonal. Asignación y pedido requieren transiciones, permisos y concurrencia bajo una frontera atómica local.

Referencias permitidas: Domain. Los adaptadores dependen del núcleo; el transporte compone dependencias sin contener reglas. Sin lógica de negocio implementada.
