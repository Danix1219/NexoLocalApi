# S03 — Comunidad y reputacion

Diseño de implementacion propuesto, 2026-11-03. Base física revisada: 3 proyectos, sin negocio ni almacenamiento configurado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/community/`. posts, reviews, reactions, saved y metrics. Separar experiencia declarada de opinion vinculada a pedido; nunca inventar historial o estrellas verificadas. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-07](../../../specs/arquitectura/nv-07/spec.md), [nv-08](../../../specs/arquitectura/nv-08/spec.md), [nv-18](../../../specs/arquitectura/nv-18/spec.md), [nv-19](../../../specs/arquitectura/nv-19/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **feature-modules**. Publicaciones, reseñas, reacciones y guardados tienen reglas propias, con lecturas de feed que las combinan.

```text
src/Modules/
src/Adapters/
src/Api/
```

- `Modules` → sin dependencias de otros proyectos.
- `Adapters` → `Modules`.
- `Api` → `Modules`, `Adapters`.

Los proyectos de núcleo contienen reglas/casos por capacidad y puertos, sin SDK de datos o transporte. Los adaptadores implementan esas capacidades; el host compone dependencias. No hay referencias entre servicios. Las rutas activas y la política verificable sustituyen la plantilla de cuatro capas anterior. Patrones pendientes de implementación, sin contratos o proveedores ficticios.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| Agregados y unicidad por interaccion | Autor controla su contenido; guardado/reaccion por actor/objeto son idempotentes. | Opinion ligada a pedido comprueba elegibilidad de S04, no una marca local de UI. |
| CQRS logico + proyecciones de metricas | Lecturas/contadores derivados con version y consistencia declaradas. | Una proyeccion retrasada no se presenta como contador exacto/verificacion inexistente. |
| Moderacion previa + outbox/inbox | Publicar contenido solo con aprobacion S06 vinculada a autor/huella/proposito; emitir eventos del commit propio. | No persistir texto rechazado ni dar por valida una aprobacion para otro contenido. |

## Persistencia y consistencia

Contenido aprobado y su evento se confirman localmente. Restricciones unicas evitan doble opinion/interaccion bajo concurrencia; proyecciones se actualizan idempotentemente. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Puertos de contenido/interacciones, verificacion de elegibilidad mediante contrato S04, moderacion S06 y publicacion de eventos propios; sin referencias a sus proyectos.

Flujo: Solicitud → autorizacion/eligibilidad → moderacion necesaria → commit contenido/outbox → consumidor actualiza metricas. El resultado de otro dueño se obtiene por contrato vigente, no copia de su base.

## Operaciones ya asignadas (sin inventar endpoints)

19 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-030 | `GET /feed` | [nv-07](../../../specs/arquitectura/nv-07/spec.md) |
| EP-031 | `POST /posts` | [nv-08](../../../specs/arquitectura/nv-08/spec.md) |
| EP-032 | `GET /posts` | [nv-08](../../../specs/arquitectura/nv-08/spec.md) |
| EP-033 | `GET /posts/{id}` | [nv-08](../../../specs/arquitectura/nv-08/spec.md) |
| EP-034 | `PATCH /posts/{id}` | [nv-08](../../../specs/arquitectura/nv-08/spec.md) |
| EP-035 | `DELETE /posts/{id}` | [nv-08](../../../specs/arquitectura/nv-08/spec.md) |
| EP-036 | `PUT /reactions/{type}/{id}` | [nv-18](../../../specs/arquitectura/nv-18/spec.md) |
| EP-037 | `DELETE /reactions/{type}/{id}` | [nv-18](../../../specs/arquitectura/nv-18/spec.md) |
| EP-038 | `GET /me/saved` | [nv-18](../../../specs/arquitectura/nv-18/spec.md) |
| EP-039 | `PUT /me/saved/{type}/{id}` | [nv-18](../../../specs/arquitectura/nv-18/spec.md) |
| EP-040 | `DELETE /me/saved/{type}/{id}` | [nv-18](../../../specs/arquitectura/nv-18/spec.md) |
| EP-041 | `POST /reviews` | [nv-19](../../../specs/arquitectura/nv-19/spec.md) |
| EP-042 | `GET /providers/{id}/reviews` | [nv-19](../../../specs/arquitectura/nv-19/spec.md) |
| EP-043 | `PATCH /reviews/{id}` | [nv-19](../../../specs/arquitectura/nv-19/spec.md) |
| EP-044 | `DELETE /reviews/{id}` | [nv-19](../../../specs/arquitectura/nv-19/spec.md) |
| EP-045 | `POST /interactions` | [nv-18](../../../specs/arquitectura/nv-18/spec.md) |
| EP-046 | `GET /me/provider/metrics` | [nv-18](../../../specs/arquitectura/nv-18/spec.md) |
| EP-094 | `GET /me/reviews` | [nv-19](../../../specs/arquitectura/nv-19/spec.md) |
| EP-095 | `GET /reviews/{id}` | [nv-19](../../../specs/arquitectura/nv-19/spec.md) |

## Criterios antes de implementar/aceptar

- Reintento/doble clic no duplica reaccion/guardado/opinion de pedido.
- Cambio de texto tras moderacion invalida aprobacion y no persiste rechazo.
- Contador/evento duplicado, atrasado o fuera de orden no inventa reputacion ni expone contenido borrado.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
