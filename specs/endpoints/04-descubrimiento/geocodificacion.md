# Geocodificación real y ubicación de cuenta

Incremento autorizado: búsqueda explícita de direcciones y geocodificación inversa con Photon/OpenStreetMap detrás de Discovery. No agrega búsqueda de prestadores ni datos simulados.

FR-001: GET /v1/discovery/places/search?q=...&lat=...&lng=... devuelve hasta cinco lugares reales de México, con lat/lng, label, zoneLabel y atribución. Consulta de 3–160 caracteres. Uso anónimo permitido durante registro, con rate limit público.

FR-002: GET /v1/discovery/places/reverse?lat=...&lng=... devuelve el lugar aproximado real o null. Fuera de México o números no finitos: 400. Proveedor no disponible: 503, sin resultados ficticios. No demuestra propiedad ni exactitud del domicilio.

FR-003: GET /v1/discovery/me/location restaura punto/radio y etiqueta privados del usuario autenticado. PUT existente guarda punto y etiqueta de zona resuelta por servidor; si geocodificación no está disponible se conserva el punto con coordenadas como etiqueta, sin inventar colonia.

NFR-001: HTTPS del proveedor configurable por operador, consultas sin logs de direcciones, timeout, serialización y separación mínima de solicitudes al proveedor. Sin claves en Flutter; atribución OpenStreetMap. Instancia pública Photon para uso moderado local; producción de volumen requiere instancia/proveedor con disponibilidad contratada.

## Plan y tareas
- T-001 FR-001/002 NFR-001: puerto Application, adaptador HTTP Infrastructure, rutas Endpoints. Acepta AC-001: parsing, límites, fallos y filtrado probados con dobles HTTP y consulta real no personal.
- T-002 FR-003: persistencia y lectura por accountId, no-store. Acepta AC-002: aislamiento y restore probados en capas actuales.
- T-003: email multipart HTML/texto con identidad visual y OTP seguro. Acepta AC-003: MIME, escape y propósito probados sin SMTP real.

Referencia: https://github.com/komoot/photon/blob/master/docs/api-v1.md y condiciones de instancia pública en README. La UI informa que la consulta se procesa con OpenStreetMap/Photon y permite seleccionar mapa sin publicar punto exacto.

Estado: implementado con evidencia local; revisión humana sin firma.


## Evidencia y resultado — 2026-11-10

Implementado en Application/Infrastructure/Endpoints con Photon: consultas explícitas, límite público, serialización de upstream con separación de un segundo, caché acotada a 128 resultados/15 minutos y timeout de seis segundos. Sin claves en Flutter ni fallback demo. GET de ubicación propia puede resolver etiquetas genéricas antiguas; PUT ignora nombres enviados por cliente y resuelve la zona en servidor. Guardar coordenadas reales sigue siendo posible si falla obtener un nombre. Una dirección geocodificada no acredita residencia ni precisión del GPS.

Configurable por operador mediante Geocoding__BaseUrl (HTTPS); valor predeterminado https://photon.komoot.io/. El proveedor público admite uso moderado y no garantiza disponibilidad. Para volumen/productivo configurar una instancia o proveedor apropiado; no se desplegó infraestructura. La instancia real rechaza lang=es: se usan nombres locales de OSM, comprobados con Tula de Allende, Hidalgo. Se realizaron búsqueda e inversa reales con una ubicación pública, ambas HTTP 200. Ready local 200.

Privacidad: no se registran URLs completas en Hosting.Diagnostics ni consultas del HttpClient de geocodificación. La app omite dirección, coordenadas y respuesta en logs de estas rutas y las excluye de la caché SQLite genérica; solo conserva su selección real en almacenamiento seguro por cuenta. La UI muestra atribución y explica el procesamiento de direcciones.

Correo: CodeEmailTemplate produce HTML inline compatible con clientes de correo y alternativa plain-text MIME. SMTP usa multipart/alternative. Colores #17695D, #FFC043, #FFFEFB; sin imágenes remotas ni tracking. Código escapado, propósito diferenciado y caducidad calculada desde el evento original en hora America/Mexico_City. Vista previa local revisada en Chrome: ../artifacts/email-preview.html (código de prueba no utilizable). No se envió un correo real ni se acreditó render en Gmail mediante estas pruebas.

Validación backend: 139 casos únicos verificados en las capas actuales: Architecture 7, Common.Unit 16, Discovery.Unit 19, Identity.Unit 4, Integration 27 y Seguridad.Tests 66. La suite completa inicial encontró un caso nuevo con null omitido; se corrigió el contrato de reverse y se repitieron los dos casos GeocodingTests correctamente. Los ajustes posteriores de nombres/fecha se verificaron con las 19/4 pruebas correspondientes. Build Release final cero errores y advertencias. Mongo/Redis de pruebas usan Testcontainers; sin modificaciones a datos reales para probar alta o persistencia. Integridad SDD OK; validador de enlaces conserva fallos históricos.

No es cierre del bloque 04: catálogo y búsqueda de prestadores continúan pendientes. Sin publicación, commit/push ni aceptación humana global. La instalación móvil no acredita GPS físico concedido, registro real completo ni entrega SMTP actual.
