# PATCH /v1/discovery/requests/{id}

Edición de una solicitud abierta por su propietario autenticado. `title` (5–90), `description` (10–600), `urgency` (`today`, `week`, `noRush`), `contact` (`chat`, `chatCall`) y `imageUploadIds` (0–6 UUID distintos) son obligatorios salvo que la lista de imágenes puede omitirse para dejarla vacía. La categoría y ubicación exacta no se editan aquí. Para conservar una foto existente, el cliente incluye su UUID aprobado en `imageUploadIds`; al quitarlo desaparece de la publicación.

El servidor vuelve a moderar texto y verifica propiedad, estado aprobado y propósito `request` de cada imagen. El cambio se confirma atómicamente en Mongo con propietario y estado `open`; 404 para solicitud ajena/inexistente y 409 para cerrada. La respuesta 200 contiene la solicitud actualizada sin ubicación exacta. `GET /requests/mine` y `/opportunities` devuelven `imageUrls` en orden y mantienen `imageUrl` como primer elemento para clientes anteriores. Máximo seis imágenes por publicación, igual que trabajos.

Pruebas: `RequestRulesTests` cubre límites, repetidas y cierre; `RequestEndpointsTests` cubre propietario, ajeno, persistencia y lectura posterior. La suite HTTP requiere Docker de pruebas.
