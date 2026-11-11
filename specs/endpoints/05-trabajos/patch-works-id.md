# PATCH /v1/discovery/works/{id}

Edición autenticada de un trabajo del prestador propietario. Cuerpo: `title` (5–90), `text` (20–1000), `tag` opcional (hasta 40) e `imageUploadIds` (0–6 UUID distintos). La categoría, zona pública, punto difuminado, recomendaciones y fecha original permanecen. Los UUID anteriores que se desean conservar se vuelven a enviar; cada imagen debe estar aprobada y pertenecer al dueño con propósito `work`.

El texto se modera antes de guardar. Mongo condiciona la escritura al dueño, perfil y estado de moderación aprobado; una publicación ajena devuelve 404. La respuesta 200 devuelve el trabajo actualizado. El listado `GET /providers/{id}/works` incluye `text` completo y conserva `textExcerpt` para compatibilidad.

Pruebas: `WorkRulesTests` cubre edición y límite de imágenes; `WorkEndpointsTests` cubre autenticación, dueño y lectura actualizada. La suite HTTP requiere Docker de pruebas.
