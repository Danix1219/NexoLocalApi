# S07 — Notificaciones y dispositivos

> **Mapa de dominios, no unidad desplegable.** Tras la **enmienda E-001**, este servicio se implementa en el modulo **Engagement** de la API modular por dominios, con datos en `nexo_engagement`. Comparte host y se despliega en una app de App Service con **2 instancias**. El contenido funcional de abajo sigue vigente; lo que cambia es donde vive. Ver [estructura-backend.md](../../estructura-backend.md).


Diseño de implementacion propuesto, 2026-11-03. Base física revisada: 4 proyectos, sin negocio ni almacenamiento configurado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/notifications/`. devices, notifications y deliveries. Vinculo dispositivo/cuenta/sesion, identidad de entrega y registro de intentos; tokens fuera de eventos/logs publicos. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-21](../../../specs/arquitectura/nv-12/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **event-driven-delivery**. El registro/consulta HTTP y la entrega asíncrona tienen tiempos y fallos distintos.

```text
src/Delivery/
src/Adapters/
src/Api/
src/Worker/
```

- `Delivery` → sin dependencias de otros proyectos.
- `Adapters` → `Delivery`.
- `Api` → `Delivery`, `Adapters`.
- `Worker` → `Delivery`, `Adapters`.

Los proyectos de núcleo contienen reglas/casos por capacidad y puertos, sin SDK de datos o transporte. Los adaptadores implementan esas capacidades; el host compone dependencias. No hay referencias entre servicios. Las rutas activas y la política verificable sustituyen la plantilla de cuatro capas anterior. Patrones pendientes de implementación, sin contratos o proveedores ficticios.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| Inbox idempotente + entrega asincrona | Consumir evento de dueño, deduplicar y planificar entrega durable antes del acuse. | Origen versionado y cuenta/sesion correctas; no compartir permisos del consumidor con el productor. |
| Worker y reintentos acotados | Estado de entrega/intentos, backoff con jitter, limite y cola de error operable. | Distinguir fallo transitorio/permanente y token invalido; no reintento infinito. |
| Adapter de push y desvinculacion | Proveedor fuera de Application; logout/borrado desvincula dispositivo y tareas pendientes. | Timeout del proveedor puede dejar resultado desconocido; no prometer exactly-once ni lectura por exito de envio. |

## Persistencia y consistencia

Inbox/dedupe + notificacion/plan de entrega en commit local. Llamada al proveedor no es una transaccion de DB; reconciliar resultado/intento sin volver a afectar negocio de S04/S05. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Registro de dispositivo, plan/estado de entrega, proveedor push y reloj del planificador; Api y Worker comparten Delivery/Adapters del MISMO dueño, no proyectos de otros servicios.

Flujo: Evento → inbox/version → crear entrega → worker verifica cuenta/dispositivo vigente → proveedor → resultado/pendiente/reintento/cola de error. El productor no espera push exitoso para confirmar su transaccion.

## Operaciones ya asignadas (sin inventar endpoints)

4 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-083 | `PUT /me/devices/{id}` | [nv-21](../../../specs/arquitectura/nv-12/spec.md) |
| EP-084 | `DELETE /me/devices/{id}` | [nv-21](../../../specs/arquitectura/nv-12/spec.md) |
| EP-085 | `GET /me/notifications` | [nv-21](../../../specs/arquitectura/nv-12/spec.md) |
| EP-086 | `PUT /me/notifications/{id}/read` | [nv-21](../../../specs/arquitectura/nv-12/spec.md) |

## Criterios antes de implementar/aceptar

- Evento duplicado no crea entregas duplicadas locales; resultado desconocido no se registra como leido.
- Logout/cambio de cuenta/borrado evita envio al antiguo usuario con el mismo dispositivo.
- Token invalido se retira; reintentos terminan y la recuperacion manual mantiene dedupe.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
