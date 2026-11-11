# Incremento de ubicacion — 2026-11-09

Alcance autorizado: seleccion real durante registro y cambio de zona en Cuenta. Se implementa PUT /v1/discovery/me/location en las capas Endpoints, Application e Infrastructure de Discovery, sin referencias a Identity.

Persistencia: user_profiles en la base keyed Discovery, _id tomado exclusivamente del sub autenticado. GeoJSON almacena [longitud, latitud], radio 2/5/10/20 y updatedAt. PUT hace upsert idempotente. Middleware global conserva revocacion y bloqueo de cuentas pendientes de baja. Coordenadas no finitas, fuera del viewbox mexicano y origen no permitido devuelven validation_failed. Acceso anonimo devuelve 401.

Limites del incremento respecto a la ficha: no hay catalogo geocodificado ni conteo de prestadores implementados. zoneLabel muestra coordenadas aproximadas y summary declara la busqueda pendiente. No se devuelve un conteo ficticio. La restauracion de la seleccion en esta entrega movil usa almacenamiento local seguro por cuenta; no se agrega un GET nuevo al contrato.

Prueba actual: tests/Nexo.Seguridad.Tests/CuentaTests.cs, UbicacionSeGuardaSoloParaLaCuentaAutenticada (HTTP, Mongo y Redis de pruebas aisladas). Verifica 401 anonimo, 400 fuera de Mexico y persistencia GeoJSON bajo el titular. No es evidencia de GPS de hardware ni de Atlas productivo.

Fotos: Blob Storage y moderacion no configurados por confirmacion del usuario. La interfaz muestra Proximamente; no hay aceptacion falsa ni archivos publicados sin moderar. Las operaciones de uploads siguen pendientes en nv-16.


Actualización 2026-11-10: el límite histórico anterior queda ampliado por [geocodificación real](geocodificacion.md). Se incorporan GET de ubicación propia y búsqueda/inversa con Photon, etiquetas reales y restore remoto; no se incorpora búsqueda de prestadores.
