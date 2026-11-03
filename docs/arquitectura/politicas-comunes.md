# Políticas de arquitectura comunes

Diseño propuesto, 2026-11-03. Se aplica junto con cada perfil y la constitución. Define responsabilidades; no selecciona paquetes, broker, nube o motores por existir un ejemplo bibliográfico.

## Capas y modelos

Usar puertos y adaptadores donde haya una frontera técnica: los casos de uso dependen de capacidades, no de SDK de proveedores. En S01/S04, Domain conserva reglas y Application coordina; en los demás se usan Features, Modules, Core, Evaluation, Delivery o Projections según el perfil. Infrastructure/Adapters adapta; Api y Worker componen y traducen transporte. La autorización por recurso y estado pertenece también al servicio dueño, aunque G01 valide la entrada.

Usar agregados y DDD táctico para invariantes complejas, especialmente pedidos y sesiones. CRUD sencillo y proyecciones pueden usar modelos más simples. Separar comandos y consultas lógicamente no exige bases distintas, MediatR ni event sourcing. No introducir repositorio genérico universal ni abstracciones sin un caso de uso.

## Atomicidad, eventos y recuperación

Cada escritura tiene un único propietario. Cuando deba producir un evento durable, estado y outbox se confirman en la misma frontera atómica local; verificar que el motor y despliegue elegidos lo soporten antes de implementar. Publicar y confirmar después. No usar transacciones distribuidas entre servicios.

Consumidores con inbox/deduplicación durable cuando haya efectos: registrar evento y efecto en una frontera atómica local. Entrega al menos una vez; no prometer exactamente una vez. Especificar eventId, versión del esquema, propietario, correlación mínima y sourceVersion por agregado. No incluir PII innecesaria. Un duplicado no repite el efecto; una versión vieja no sobrescribe; un hueco requiere reconciliación contractual. Retención de claves, recuperación, reconstrucción y cola de errores necesitan responsable y procedimiento.

Los nombres, esquemas y canales de eventos se formalizan con sus consumidores antes de implementarse. Estas políticas no inventan eventos públicos ni automatismos de negocio.

## Llamadas, reintentos y errores

Definir presupuesto de tiempo por dependencia y cancelación de extremo a extremo. Reintentar únicamente fallos transitorios de operaciones seguras o protegidas por idempotencia durable, con límite, espera creciente y jitter. Un timeout de escritura puede representar resultado desconocido: consultar o repetir con la misma clave según contrato; no repetir POST indiscriminadamente.

Circuit breaker y aislamiento por dependencia se añaden cuando el fallo observado lo justifique; definir reapertura y respuesta degradada. No convertir la falta de moderación o autorización en permiso implícito. Errores públicos estables y correlación sin exponer excepciones, secretos o contenido privado. Colas de error tienen atención y reejecución autorizada; no bucles infinitos.

## Seguridad, privacidad y réplicas

Comprobar actor, pertenencia, consentimiento y estado en cada operación sensible. El estado de sesión, revocación, deduplicación y permisos no puede depender solo de memoria de una réplica. Una conexión WebSocket abierta no conserva permisos revocados. Limitar tráfico y validar aislamiento con al menos dos réplicas cuando corresponda a aceptación.

Cachear solo respuestas permitidas por su contrato, con claves, variación, TTL e invalidación definidos. Una caché pública no incorpora ubicación precisa, contacto privado ni respuestas de otro actor. La caché no es autoridad de consentimiento ni almacenamiento único de revocación. Cada dueño define retención y borrado de sus datos, logs y proyecciones.

## Flujos entre propietarios

| Flujo | Orden y frontera de responsabilidad | Fallo que debe comprobarse |
|---|---|---|
| Identidad | G01 recibe; S01 valida, rota o revoca en almacenamiento propio; cada destino autoriza el recurso | Dos refresh concurrentes, replay y revocación entre réplicas |
| Contenido | Dueño solicita decisión S06 vinculada a contenido/autor/finalidad/política; persiste solo el contenido aprobado correspondiente | Decisión antigua, contenido modificado y proveedor no disponible; sin almacenar rechazo como contenido público |
| Pedido | S04 valida actor/estado/versión y confirma cambio, asignación y outbox local; consumidores aplican sus contratos después | Doble asignación, reentrega y caída de push sin deshacer el pedido confirmado |
| Chat y aviso | S05 comprueba membresía/estado, persiste una vez y después distribuye; S07 entrega aviso según su contrato | Reenvío del cliente, reconexión, orden y revocación durante socket abierto |
| Borrado | S01 coordina solicitud y seguimiento; cada dueño borra o conserva solo lo justificado y confirma; reintentos idempotentes | Confirmación perdida y propietario caído; avanzar al borrado sin compensar restaurando PII |

No se crea un chat automáticamente por cualquier evento de pedido: ese disparador depende del contrato y reglas de la historia. S08 consume únicamente proyecciones autorizadas y nunca habilita acceso por sí mismo.

## Comprobación y operación

Unitarias para reglas y casos de uso con dobles explícitos; integración con almacenamiento/proveedor real para atomicidad, índices, concurrencia y entrega; contratos para DTO/esquemas y compatibilidad. Casos obligatorios pertinentes: positivo, permiso denegado, estado incompatible, duplicado, fallo/reintento y aislamiento. Un doble de SMS/moderación no acredita aceptación productiva.

EP-089 liveness no consulta todas las dependencias. EP-090 readiness comprueba las indispensables para el host según su función y sin datos de usuario. Correlación, métricas de errores/latencia, lag de consumidores y outbox/cola de errores con alertas y responsable. Logs sin tokens, OTP, contactos o contenido rechazado. Valores operativos, SLA y proveedores se resuelven en las specs y decisiones, sin inventar umbrales.

## Fundamento consultado

Estas fuentes explican los patrones, no ratifican productos Azure para Nexo Local:

- [DDD en microservicios, Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice): aplicar complejidad según las reglas del propietario.
- [CQRS simplificado, Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns): separación de responsabilidades de lectura y escritura.
- [Coreografía, Microsoft](https://learn.microsoft.com/en-us/azure/architecture/patterns/choreography): coordinación eventual, eventos e idempotencia.
- [Saga, Microsoft](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/saga/saga): coordinación entre fronteras; el borrado irreversible requiere recuperación hacia adelante.
- [Retry, Microsoft](https://learn.microsoft.com/en-us/azure/architecture/patterns/retry): fallos transitorios y reintentos acotados.

## Arquitecturas internas revisadas

La [selección por caso de uso](seleccion-arquitecturas.md) es la autoridad vigente de proyectos y referencias. Estas políticas comparten límites y garantías, sin exigir la misma división interna a todos los dueños.
