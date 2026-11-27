# DELETE /v1/discovery/requests/{id}

Propietario autenticado y activo. 204 idempotente; solicitud ajena o inexistente 404; asignada 409 order.request_already_assigned. El estado se comprueba dentro de transaccion Mongo. Tombstone preserva historial de pedidos y recibos de idempotencia; desaparece de mine, oportunidades, consulta publica y guardados. No equivale a cerrar como resuelta. Evento request.deleted una vez. No acepta id de cuenta desde el cuerpo. Prueba RequestPersistenceTests.Delete_is_owner_only_idempotent_and_preserves_assigned_orders aprobada con Mongo real.
