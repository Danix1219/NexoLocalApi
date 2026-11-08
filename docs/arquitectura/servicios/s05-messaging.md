# S05 — Mensajeria

> **Mapa de dominios, no unidad desplegable.** Tras la **enmienda E-001**, este servicio se implementa en el modulo **Engagement** de la API modular por dominios, con datos en `nexo_engagement`. Comparte host y se despliega en una app de App Service con **2 instancias**. El contenido funcional de abajo sigue vigente; lo que cambia es donde vive. Ver [estructura-backend.md](../../estructura-backend.md).


Diseño de implementacion propuesto, 2026-11-03. Base física revisada: 3 proyectos, sin negocio ni almacenamiento configurado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/messaging/`. conversations, messages y blocks. Historial/secuencia durables por conversacion; identificador estable de mensaje del cliente; contexto preservado por mensaje. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-11](../../../specs/arquitectura/nv-11/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **ports-adapters-realtime**. Escritura durable y recuperación de historial se separan del transporte efímero de tiempo real.

```text
src/Core/
src/Adapters/
src/Api/
```

- `Core` → sin dependencias de otros proyectos.
- `Adapters` → `Core`.
- `Api` → `Core`, `Adapters`.

Los proyectos de núcleo contienen reglas/casos por capacidad y puertos, sin SDK de datos o transporte. Los adaptadores implementan esas capacidades; el host compone dependencias. No hay referencias entre servicios. Las rutas activas y la política verificable sustituyen la plantilla de cuatro capas anterior. Patrones pendientes de implementación, sin contratos o proveedores ficticios.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| Historial durable + fan-out en vivo | Persistir mensaje autorizado antes del acuse de aceptacion y entrega en tiempo real. | Redis/pubsub o sockets no son historial ni fuente de permisos. |
| Idempotencia por conversacion/mensaje + cursor | Misma conversacion/clientMessageId no duplica mensaje; orden y recuperacion con cursor/version definidos. | Reconexion no inventa recibidos; acuse de persistencia no equivale a lectura. |
| Autorizacion continua y outbox | Validar miembro/bloqueo/estado por comando y consulta; evento del mensaje con commit local. | Una conexion WS abierta no conserva permisos tras revocacion/cancelacion. |

## Persistencia y consistencia

Mensaje, deduplicacion/secuencia y evento se confirman en frontera local de S05. Entrega a socket/push ocurre despues; fallo de fan-out se recupera con historial/cursor, no reenviando sin id. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Repositorio de conversacion/mensaje, consultas de pertenencia/estado por contrato, moderacion, transporte realtime y eventos. Gateway solo enruta upgrade; no autoriza historial por S05.

Flujo: HTTP/WS → validar actor/miembro/estado y moderacion → commit mensaje → acuse persistido → fan-out/evento → cliente reconcilia cursor. Push offline usa S07 si la historia lo contempla; no crear una operacion por texto rapido.

## Operaciones ya asignadas (sin inventar endpoints)

8 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-066 | `POST /conversations` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |
| EP-067 | `GET /me/conversations` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |
| EP-068 | `GET /conversations/{id}/messages` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |
| EP-069 | `POST /conversations/{id}/messages` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |
| EP-070 | `PUT /conversations/{id}/read` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |
| EP-071 | `PUT /conversations/{id}/block` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |
| EP-072 | `DELETE /conversations/{id}/block` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |
| EP-074 | `WS /ws/v1/chat` | [nv-11](../../../specs/arquitectura/nv-11/spec.md) |

## Criterios antes de implementar/aceptar

- Dos replicas/reconexion/reintento del mismo clientMessageId producen un mensaje durable.
- Bloqueo o perdida de permiso corta acceso a envio/historial aunque el socket siga conectado.
- Fan-out caido o evento fuera de orden no pierde historial ni permite falso acuse de lectura.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
