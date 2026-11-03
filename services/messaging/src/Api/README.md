# NexoLocal.Messaging.Api

Arquitectura: ports-adapters-realtime. Escritura durable y recuperación de historial se separan del transporte efímero de tiempo real.

Referencias permitidas: Core, Adapters. Los adaptadores dependen del núcleo; el transporte compone dependencias sin contener reglas. Sin lógica de negocio implementada.
