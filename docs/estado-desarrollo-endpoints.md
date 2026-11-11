# Ejecución ordenada de endpoints

Revisión de código local: 2026-11-10. No equivale a despliegue ni aceptación humana. Autoridad contractual: fichas en `specs/endpoints/`. El número de carpeta es un grupo funcional, no el número de una spec de arquitectura.

| Grupo | Estado local | Dependencia / siguiente acción |
|---|---|---|
| 01-auth | Implementado previamente; SMTP por configuración | Conservar regresión y verificación por correo |
| 02-cuenta | Implementado previamente | Baja distribuida sigue dependiendo de consumidores posteriores |
| 03-perfil-prestador | Cinco operaciones incorporadas en este incremento | Moderación externa real y aceptación cloud pendientes; ver registro de implementación |
| 04-descubrimiento | Ubicación/geocodificación y directorio público implementados localmente | Categorías, sugerencias, búsqueda y mapa providers; mapa requests habilitado por grupo 06; feed pendiente. Atlas Search/caché de resultados pendientes |
| 05-trabajos | Publicar, listar, editar, borrar y recomendar; ver incremento | Texto e imágenes usan moderación real; feed/push/despacho cloud pendientes |
| 06-solicitudes | Publicar, listar, editar y cerrar; mapa requests habilitado | Texto e imágenes usan moderación real; pedidos/reseñas pendientes |
| 07-interés | Implementado localmente (T-992); 19 pruebas de endpoints y resiliencia aprobadas | Outbox durable sin consumidores; pedidos/chat dependen de 08/10; Azure real y aceptación humana pendientes. Ver registro de implementación |
| 08-pedidos | Parcial local; no habilitar como bloque terminado | Operaciones base de pedido/estados y cifrado local implementados. Falta asignaciÃ³n/compensaciÃ³n atÃ³mica desde solicitud, consumidores de outbox, integraciÃ³n Key Vault y pruebas de integraciÃ³n; ver `specs/endpoints/08-pedidos/implementacion.md`
| 09-opiniones | Pendiente | Pedido resuelto y participantes |
| 10-mensajes | Pendiente | Elegibilidad de conversación y pedidos/participantes |
| 11-guardados | Pendiente | Recursos existentes de perfiles, trabajos y solicitudes |
| 12-contacto-reportes | Pendiente | Canales consentidos, auditoría y moderación |
| 13-notificaciones | Pendiente | Eventos reales de los grupos anteriores |
| 14-imágenes-cdn | Pendiente transversal | Refinar antes de habilitar subida en 03/05/06; Blob/moderación no configurados. No hace falta simular imágenes para desarrollar texto |
| 15-pantallas | Pendiente | Componer únicamente operaciones ya implementadas |

No implementar un feed con datos inventados para anticiparse a sus fuentes. Antes de cada bloque: revisar fichas → conciliar decisiones → concretar plan/tareas/criterios → desarrollar en las capas existentes → pruebas unitarias/integración/seguridad → registrar evidencia y límites. No tocar móvil, publicar, aplicar Terraform ni modificar secretos por este encargo.


## Actualizaci?n autorizada de medios ? 2026-11-10
El encargo posterior autoriza backend y Flutter para fotos reales. Grupo14: intenci?n, confirmaci?n, consulta, retirada, lectura de WebP/miniatura y portada personal implementados; avatar profesional/personal, portadas y publicaciones 03/05/06 conectados. Blob privado, Queue y Content Safety existentes verificados en prueba sint?tica con limpieza. Sustituye los estados hist?ricos de infraestructura no configurada y fotos bloqueadas. CDN/Front Door y chat privado contin?an pendientes. Ver [detalle y evidencia](../specs/endpoints/14-imagenes-cdn/implementacion.md).
