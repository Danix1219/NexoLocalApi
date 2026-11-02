# Primer incremento de identidad

Spec viva: [nv-04](../specs/nv-04/spec.md). Consumidor: [app 004](../../App_NexoLocal/specs/004-identidad-sesion/spec.md). Alcance mínimo: EP-001/002/003/004/005/007/008; no implementar todo S01 a la vez. La [referencia de análisis](../../requerimientos_nexolocal/sdd/specs/001-identidad-sesion/incremento-01.md) es histórica. La spec viva y el [acuerdo común](../../docs_nexo/SDD_ALINEACION.md) rigen el refinamiento.

Antes de Ready: ratificar stack/SDK, SMS, consentimiento, límites/TTL y semántica; identificar revisor. Formalizar contratos en API y consumo en app. Recuperación, sesiones adicionales y saga de borrado son historias siguientes, no entregadas por el subset mínimo.

Escenarios a detallar: alta válida; OTP inválido/expirado/reutilizado; reenvío limitado sin enumeración; login inválido neutral; refresh rotatorio y replay; renovación concurrente móvil; logout A rechazado en B; fallo Redis sin acceso por omisión; logout offline con estado remoto pendiente; Ana→logout→Carlos sin datos residuales. Ninguno fue ejecutado como producto en esta fase.

Asignación vigente 0.4.0: identidad mínima permanece en nv-04/app 004. Recuperación, edición de cuenta y sesiones adicionales pertenecen a [nv-16](../specs/nv-16/spec.md) / [app 017](../../App_NexoLocal/specs/017-cuenta-recuperacion-sesiones/spec.md). Borrado pertenece a nv-12/app 012, con dispositivos de nv-21/app 022. Las referencias históricas no sustituyen estas autoridades.
