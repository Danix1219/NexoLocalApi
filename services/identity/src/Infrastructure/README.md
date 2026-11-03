# NexoLocal.Identity.Infrastructure

Arquitectura: hexagonal. Sesiones, OTP y borrado requieren invariantes aisladas y proveedores reemplazables.

Referencias permitidas: Application, Domain. Los adaptadores dependen del núcleo; el transporte compone dependencias sin contener reglas. Sin lógica de negocio implementada.
