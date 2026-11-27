# Perfil compartido de solicitante

Fecha: 2026-11-21. Implementacion local; revision humana pendiente.

- POST /v1/identity/me/profile-share: Bearer de cuenta activa. Sin cuerpo. 200 {shareId: UUID aleatorio}; estable hasta revocacion. Nunca devolver accountId.
- GET /v1/identity/profiles/{shareId}: lectura limitada por tasa; 200 {name, photoUrl}; 404 si no existe, revocado o cuenta no activa. Cache-Control: no-store. Solo datos visibles del perfil, sin historial/contacto/ubicacion.
- DELETE /v1/identity/me/profile-share: Bearer del propietario; 204 idempotente. Volver a activar crea otro enlace; el anterior deja de resolver.

Activacion explicita por accion de compartir/copiar/QR. Abrir la foto no activa el perfil. Enlace app: nexolocal://profile/{shareId}. No es un enlace HTTPS universal y requiere app instalada. Identity posee perfil y opt-in; Discovery no lee su base directamente. Guardados de solicitudes usa IRequestClients y elimina contenido de autores inactivos.

Pruebas: ProfileSharePersistenceTests (opt-in, datos minimos, actualizacion de estado y revocacion), WorkPersistenceTests (autor publico de trabajo y solicitud, sin contacto privado), http_guardados_contacto_test.dart y diseno_maqueta_test.dart. Pruebas HTTP de autenticacion de estas nuevas rutas y aceptacion final en dispositivo pendientes.
