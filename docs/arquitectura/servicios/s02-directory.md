# S02 — Directorio y descubrimiento

Diseño de implementacion propuesto, 2026-11-03. Base física revisada: 3 proyectos, sin negocio ni almacenamiento configurado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/directory/`. categories, providers y read_cache. Perfil de prestador por cuenta; distinguir ubicacion publica aproximada, direccion comercial y origen privado. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-06](../../../specs/arquitectura/nv-06/spec.md), [nv-07](../../../specs/arquitectura/nv-07/spec.md), [nv-17](../../../specs/arquitectura/nv-17/spec.md), [nv-19](../../../specs/arquitectura/nv-19/spec.md), [nv-25](../../../specs/arquitectura/nv-25/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **vertical-slices**. Catálogo, edición y búsqueda cambian por funcionalidad; reglas de contacto y visibilidad permanecen en su slice.

```text
src/Features/
src/Adapters/
src/Api/
```

- `Features` → sin dependencias de otros proyectos.
- `Adapters` → `Features`.
- `Api` → `Features`, `Adapters`.

Los proyectos de núcleo contienen reglas/casos por capacidad y puertos, sin SDK de datos o transporte. Los adaptadores implementan esas capacidades; el host compone dependencias. No hay referencias entre servicios. Las rutas activas y la política verificable sustituyen la plantilla de cuatro capas anterior. Patrones pendientes de implementación, sin contratos o proveedores ficticios.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| Modelo de perfil y CRUD acotado | Reglas de completitud/cobertura/modo dentro de su dueño; catalogo simple no exige un modelo DDD complejo. | nv-17 posee catalogo; nv-06 consume sin reimplementarlo. |
| CQS/CQRS logico + modelo de lectura | Separar modificaciones de perfil/categoria de busqueda/filtros/comparacion. | No obliga otra DB, otro proceso, un buscador separado o MediatR. |
| Cache-aside y proyecciones publicas | Invalidar por cambio/version, incluir generacion/expiracion y comprobar caducidad al leer. | No usar TTL como garantia de borrado inmediato ni publicar ubicacion precisa privada. |

## Persistencia y consistencia

Unicidad cuenta/perfil y cambio de version locales. Actualizacion y evento de invalidacion/outbox no pueden divergir silenciosamente; consultas leen solo modelos autorizados. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Repositorio de perfil/categoria por necesidad real, consultas geoespaciales, cache y geocodificacion como adaptadores; no consultas a MongoDB de S03/S04.

Flujo: Cambio de perfil autorizado → validacion/moderacion S06 cuando aplique → commit S02/outbox → invalidar lectura. Datos derivados de otros dueños llegan por contrato/evento, no joins entre bases.

## Operaciones ya asignadas (sin inventar endpoints)

20 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-014 | `GET /catalog/categories` | [nv-17](../../../specs/arquitectura/nv-17/spec.md) |
| EP-015 | `PUT /admin/catalog/categories/{id}` | [nv-17](../../../specs/arquitectura/nv-17/spec.md) |
| EP-016 | `POST /providers` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-017 | `GET /providers/{id}` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-018 | `GET /me/provider` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-019 | `PATCH /me/provider` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-020 | `PATCH /me/provider/coverage` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-021 | `PATCH /me/provider/schedule` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-022 | `PATCH /me/provider/prices` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-023 | `POST /me/provider/refresh` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-024 | `DELETE /me/provider` | [nv-06](../../../specs/arquitectura/nv-06/spec.md) |
| EP-025 | `GET /providers` | [nv-07](../../../specs/arquitectura/nv-07/spec.md) |
| EP-026 | `GET /search/suggestions` | [nv-07](../../../specs/arquitectura/nv-07/spec.md) |
| EP-027 | `GET /providers/compare` | [nv-07](../../../specs/arquitectura/nv-07/spec.md) |
| EP-028 | `GET /geo/search` | [nv-07](../../../specs/arquitectura/nv-07/spec.md) |
| EP-029 | `GET /geo/reverse` | [nv-07](../../../specs/arquitectura/nv-07/spec.md) |
| EP-091 | `GET /providers/{id}/contact` | [nv-25](../../../specs/arquitectura/nv-25/spec.md) |
| EP-093 | `GET /mentions/suggestions` | [nv-19](../../../specs/arquitectura/nv-19/spec.md) |
| EP-096 | `PATCH /me/provider/contact-preferences` | [nv-25](../../../specs/arquitectura/nv-25/spec.md) |
| EP-100 | `GET /internal/providers/{id}/contact-policy` | [nv-25](../../../specs/arquitectura/nv-25/spec.md) |

## Criterios antes de implementar/aceptar

- Perfil unico por cuenta; actualizacion concurrente/version y completitud calculadas de forma unica.
- Filtros/mapa/comparacion usan criterios equivalentes y paginacion definida.
- Cache vieja, dependencia de geocodificacion caida y ocultacion de origen privado tienen salida explicita.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
