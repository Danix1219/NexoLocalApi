# S08 — Proyeccion de relaciones

> **Mapa de dominios, no unidad desplegable.** Tras la **enmienda E-001**, este servicio se implementa en el modulo **Discovery** de la API modular por dominios, con datos en `nexo_discovery`. Comparte host y se despliega en una app de App Service con **2 instancias**. El contenido funcional de abajo sigue vigente; lo que cambia es donde vive. Ver [estructura-backend.md](../../estructura-backend.md).


Diseño de implementacion propuesto, 2026-11-03. Opcional/condicionado; ningun proyecto, grafo o motor creado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/relations/`. Proyeccion derivada de relaciones publicas, versiones/tombstones y estado de reconstruccion. No posee la cuenta, el pedido, permisos ni direccion/telefono. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-15](../../../specs/arquitectura/README.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **read-projections**. Consulta derivada y reconstruible sin autoridad de escritura o permisos de otros servicios.

```text
src/Projections/
src/Adapters/
src/Api/
```

- `Projections` → sin dependencias de otros proyectos.
- `Adapters` → `Projections`.
- `Api` → `Projections`, `Adapters`.

Los proyectos de núcleo contienen reglas/casos por capacidad y puertos, sin SDK de datos o transporte. Los adaptadores implementan esas capacidades; el host compone dependencias. No hay referencias entre servicios. Las rutas activas y la política verificable sustituyen la plantilla de cuatro capas anterior. Patrones pendientes de implementación, sin contratos o proveedores ficticios.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| CQRS de proyeccion + inbox | Consumir hechos versionados de dueños y responder consultas publicas del catalogo. | No obliga Neo4j; motor/costo/consulta deben justificar habilitacion. |
| Versiones/tombstones y reconstruccion | Deduplicar, descartar versiones viejas, detectar huecos y reconstruir desde fuentes autorizadas. | Borrado no se revierte por evento atrasado; coordinar acuse solo si servicio habilitado. |
| Lectura prescindible | Respuesta/degradacion explicita si proyeccion incompleta o fuera de servicio. | No usar el grafo como autoridad de permisos o bloquear identidad/pedidos por su ausencia. |

## Persistencia y consistencia

Cambio de proyeccion y version/inbox locales; no transaccion con los dueños. La fuente autoritativa permanece fuera y la reconstruccion no puede recrear PII eliminada. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Almacenamiento de proyeccion, inbox y alimentacion/reconstruccion por contrato publico; sin repositorio a la DB de S01/S02/S04.

Flujo: Hecho publico compatible → dedupe/version → proyeccion → consulta con estado de frescura. Tombstone domina replay anterior; reconstruccion mantiene revision y permite reconciliacion.

## Operaciones ya asignadas (sin inventar endpoints)

1 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-098 | `POST /graphql/social/v1` | [nv-15](../../../specs/arquitectura/README.md) |

## Criterios antes de implementar/aceptar

- Duplicados/versiones viejas no cambian resultado o resucitan relaciones borradas.
- Reconstruccion reproduce proyeccion autorizada sin leer bases ajenas ni recuperar PII.
- Servicio no habilitado o caido no bloquea capacidades obligatorias ni provoca falsas respuestas de permisos.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
