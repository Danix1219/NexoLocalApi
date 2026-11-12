# Imágenes — incremento ejecutable
Autoridad nv-05 (medios), fichas grupo14. Usuario autoriza infraestructura existente y funcionalidad perfil/portada/publicaciones. No chat privado ni CDN nuevo. Discovery posee uploads. Contrato Discovery a Identity para avatar; nunca consultar DB ajena. Host compone. Desarrollo sin CDN: URL estable del backend, contenedores privados, lectura de aprobado por id, no-store para que retirar imagen sea inmediato; en producción base HTTPS configurada, dev localhost explícito. No SAS de lectura persistida ni claves Flutter. Avatar personal PATCH Identity existente, portada personal nuevo GET/PUT me/cover con dueño; portada profesional PATCH basic existente.

Subida SAS de creación exclusiva por blob (c), no sobrescritura: evita reemplazar original durante moderación. 15min; límite30/día, tamaño5MB/4096px verificado por bytes, normalizar EXIF/orientación a WebP sin metadatos (1600px, miniatura400). Content Safety real sobre JPEG normalizado; falta/timeout/respuesta incompleta no aprueban. Colas reales image-processing; complete idempotente cambia pending->processing y cron reencola processing si falla enqueue. Worker con lease Mongo y reintento, descarga acotada/snapshot ETag, publica solo tras moderación y borrado de original. Cleanup pendientes caducados y blobs/medios retirados; no TTL de registro antes de limpieza.

Plan: Application/Media puertos/reglas; Infrastructure/Media Blob/Queue/Skia/Mongo/worker; Endpoints MediaEndpoints; Contracts IMediaContract; Host bridge Identity. Reusar pruebas Discovery.Unit/Integration/Seguridad y Flutter tres capas. Paquetes Azure ya fijados y SkiaSharp3.119.4 con assets Linux. No recursos nuevos, .env sin exposición, commits/push/despliegue excluidos.

- FR-991: Intención/complete/estado/lectura aprobada y retirada de medios propios.
- NFR-991: Propiedad, expiración, cuota, procesamiento acotado y sin EXIF; fallos cerrados, concurrencia y reintento.
- AC-991: reglas/codificación, contrato HTTP/JWT y aislamiento con pruebas en capas existentes.
- T-991 → FR-991/NFR-991: construir flujo y consumo móvil, comprobar AC-991 y documentar evidencia real. Revisión humana pendiente.

Evidencia pendiente de ejecución. No afirmar aceptación cloud por dobles.


## Corrección solicitada: falsos rechazos y lectura completa
T-992 -> FR-992/NFR-992; AC-992: imágenes aceptan niveles 0/2 y bloquean 4/6 de las cuatro categorías, sin aprobar respuesta incompleta o fallo Azure. No se exige rostro ni se compara identidad. Errores de bytes/formato se distinguen del rechazo de contenido; no almacenar foto ni puntuaciones personales en logs. Pruebas con respuestas HTTP controladas para cada categoría/nivel y formato inválido; el caso personal anterior no es reproducible sin su análisis y no se afirma categoría específica. Texto conserva su política independiente.
T-993 -> FR-993; AC-993: listados mine/opportunities devuelven description completa además de excerpt compatible; Flutter presenta tres líneas, Ver más/Ver menos solo si desborda, con pruebas de expandir/contraer y texto corto. Nunca intentar reconstruir los caracteres truncados.
Fuente de escala: https://learn.microsoft.com/en-us/azure/ai-services/content-safety/quickstart-image (0 seguro, 2 bajo, 4 medio, 6 alto). Revisión humana pendiente.


Evidencia de corrección 2026-11-10: suite .NET completa 242/242 (7 arquitectura, 16 Common, 61 Discovery.Unit, 4 Identity.Unit, 54 Integration, 100 Seguridad), 0 fallos. MediaPipelineTests comprueba 0/2 permitidos, 4/6 bloqueados para cada una de las cuatro categorías y respuesta incompleta cerrada. RequestEndpointsTests comprueba description completa y excerpt en mine/opportunities por HTTP. SDD-INTEGRITY-OK (26 specs). La prueba cloud sintética anterior acredita integración con Azure, pero no determina la categoría de la foto personal que falló. API local reiniciada, ready=200.
