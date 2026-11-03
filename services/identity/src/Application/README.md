# NexoLocal.Identity.Application

Arquitectura: hexagonal. Sesiones, OTP y borrado requieren invariantes aisladas y proveedores reemplazables.

Referencias permitidas: Domain. Los adaptadores dependen del núcleo; el transporte compone dependencias sin contener reglas. Sin lógica de negocio implementada.
