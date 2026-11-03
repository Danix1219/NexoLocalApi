# NexoLocal.Directory.Api

Arquitectura: vertical-slices. Catálogo, edición y búsqueda cambian por funcionalidad; reglas de contacto y visibilidad permanecen en su slice.

Referencias permitidas: Features, Adapters. Los adaptadores dependen del núcleo; el transporte compone dependencias sin contener reglas. Sin lógica de negocio implementada.
