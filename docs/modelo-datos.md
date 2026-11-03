# Datos backend por propietario

Diseño propuesto sobre [fuente 1.2.0](../../docs_nexo/Nexo_Local_Requerimientos_y_Arquitectura.md), anexo de entidades y fichas XLSX. No son bases creadas, índices medidos ni decisiones ratificadas.

| Propietario | Datos | Invariantes y consultas |
|---|---|---|
| G01 | Sin base de negocio | Composición autorizada; no persistir domicilio/teléfono en caché pública |
| S01 | accounts, sessions, otp_challenges, deletions | Teléfono normalizado único, refresh hash/familia, OTP de un uso, revocación durable y trámite de borrado |
| S02 | categories, providers, read_cache | Perfil único por cuenta, filtros/modalidad/cobertura, GeoJSON longitud/latitud; origen privado separado de dirección comercial |
| S03 | posts, reviews, reactions, saved, metrics | Autoría por ID, opinión única por pedido si vinculada, guardado/reacción idempotentes, fotos distintas de pedidos |
| S04 | requests, interests, orders | Interés único por solicitud/prestador, versión, una asignación activa; pedido/asignación/outbox atómicos locales |
| S05 | conversations, messages, blocks | Miembros por IDs; unique conversación/clientMessageId; secuencia/historial durables, contexto por mensaje |
| S06 | media, reports, decisions | Aprobación por hash/autor/propósito/política, medios privados autorizados, rechazados no persistidos |
| S07 | devices, notifications, deliveries | Dispositivo por sesión/cuenta, dedupe, desvinculación al logout |
| S08 opcional | Proyección de relaciones públicas | Sin PII ni autoridad de permisos; versiones, tombstones y reconstrucción |

Cada propietario confirma cambio y outbox en su transacción; consumidores deduplican con inbox. Referencias externas no son FK ni acceso a la base ajena. Validar soporte de transacciones MongoDB en el plan real antes de Ready técnico.

## Índices y versiones

Definir por historia: unicidad, consulta estado/fecha/id, geoespacial, TTL y plan de consulta. UUID/version y schemaVersion son diseño; ratificar serialización en contrato. Dinero usa moneda/unidad y precisión explícita. Fechas de intercambio UTC con representación fijada por contrato.

read_cache es derivado público con generatedAt/expiresAt; el lector comprueba caducidad porque TTL no borra inmediatamente. Redis cache-aside se invalida por cambio/version. Revocación usa fuente durable y fallo seguro, separada de caché expulsable. TTL/límites requieren medición.

## Ciclo de vida y privacidad

Teléfono y domicilio exacto dependen de participante/estado/consentimiento, nunca del modo visual. No incluirlos en eventos, telemetría o caché compartida. Cancelación retira acceso también en consumidor. Definir borrado/anonimización por entidad y acuse por servicio habilitado; bloquear cuenta al iniciar, completar solo con todos los acuses.

Retención y backups requieren decisión de producto; restauración aplica supresiones antes de servir. Scripts idempotentes, índices y respaldo/restauración se preparan al implementar, sin generar scripts DB ahora. Flutter conserva su modelo local y nunca accede a estas bases. Ver [acuerdo común](../../docs_nexo/SDD_ALINEACION.md).

## Perfiles por propietario

El [catálogo de arquitectura](arquitectura/README.md) define estructura, patrones, puertos, consistencia y fallos de G01 y S01–S08. Aplicar sus políticas comunes al refinar cada historia; no crea proyectos, ratifica proveedores ni reemplaza contratos.
