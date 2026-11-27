# Edicion y eliminacion de mensajes privados

Incremento 2026-11-19, implementacion local validada. Autoridad: Engagement, conversaciones privadas.

- PATCH /v1/engagement/chat-keys/conversations/{conversationId}/messages/{messageId}: reemplaza el sobre cifrado de un mensaje propio. Requiere Idempotency-Key UUID y sobres para todos los dispositivos actuales. El contenido autenticado incluye editOf igual al mensaje objetivo. Respuesta con messageId y editedAt; el texto nunca sale del cliente sin cifrar.
- DELETE en la misma ruta: solo autor participante, conserva una marca kind=deleted y retira el contenido del registro activo. Un mensaje eliminado no admite edicion. No afecta mensajes de otra conversacion, de otra cuenta ni eventos de sistema.
- El evento messageUpdated refresca ambas partes; no crea otro mensaje ni reinicia sus recibos. editedAt es un campo aditivo en el historial. No se cambia createdAt ni el orden de la conversacion.

La eliminacion es logica para todos los clientes compatibles, no una garantia de borrado de copias, capturas, respaldos o archivos previamente descargados. Los adjuntos cifrados almacenados requieren una politica separada de recoleccion; no se promete purga fisica de almacenamiento. Los clientes antiguos deben actualizarse para mostrar la marca de eliminacion correctamente.

Validacion pendiente: autoria, pertenencia, bloqueo, no resurreccion, texto cifrado y recorrido entre dispositivos. Sin aceptacion productiva ni firma humana.

Evidencia local 2026-11-19: dos pruebas ChatEncryptionPersistenceTests aprobadas en Release con MongoDB real, incluidas autoria, aislamiento, borrado, no resurreccion y reintento antiguo. Flutter 87 pruebas aprobadas y analyze sin incidencias. Recorrido humano tras reiniciar API pendiente.
