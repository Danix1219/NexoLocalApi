# S04 — Solicitudes y pedidos

Diseño de implementacion propuesto, 2026-11-03. Base física creada por encargo posterior; sin negocio ni almacenamiento configurado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/orders/`. requests, interests y orders; asignacion pertenece a S04. Precio opcional, actor, version y estado se definen por contrato; no se modela pago/cobro no requerido. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-09](../../../specs/nv-09/spec.md), [nv-10](../../../specs/nv-10/spec.md), [nv-20](../../../specs/nv-20/spec.md), [nv-25](../../../specs/nv-25/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **ddd-hexagonal**. Asignación y pedido requieren transiciones, permisos y concurrencia bajo una frontera atómica local.

```text
src/Domain/
src/Application/
src/Infrastructure/
src/Api/
```

- `Domain` → sin dependencias de otros proyectos.
- `Application` → `Domain`.
- `Infrastructure` → `Application`, `Domain`.
- `Api` → `Application`, `Infrastructure`.

Los proyectos de núcleo contienen reglas/casos por capacidad y puertos, sin SDK de datos o transporte. Los adaptadores implementan esas capacidades; el host compone dependencias. No hay referencias entre servicios. Las rutas activas y la política verificable sustituyen la plantilla de cuatro capas anterior. Patrones pendientes de implementación, sin contratos o proveedores ficticios.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| Maquina de estados y agregado transaccional | Transiciones por participante/estado con control de concurrencia y una asignacion activa. | No separar asignacion y pedido en servicios que requieran transaccion distribuida para esta regla. |
| Idempotencia + control optimista | Clave de solicitud/operacion y version permiten reintentos seguros y rechazan transiciones incompatibles. | La clave, alcance y respuesta repetida se formalizan; no sumar claves nuevas al API sin contrato. |
| Transactional outbox + politicas de contacto | Pedido/asignacion y evento en una frontera local atomica; publicar despues. | Revelar datos precisos solo en estado/consentimiento autorizado; cancelacion revoca acceso tambien en consumidores. |

## Persistencia y consistencia

Asignacion activa, cambio de pedido y outbox requieren atomicidad local demostrada con la persistencia elegida. No hay transaccion entre S02/S04/S05/S07. Validar soporte del motor antes de Ready tecnico. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Repositorios por agregado, autorizacion/datos minimos de participantes, moderacion cuando aplique y eventos versionados; interfaz de reloj/version solo donde la regla lo requiera.

Flujo: Comando autenticado → comprobar actor/estado/version → regla de dominio → commit estado/asignacion/outbox → consumidores contractuales. Un fallo de push/chat no revierte un pedido confirmado; su entrega queda pendiente.

## Operaciones ya asignadas (sin inventar endpoints)

22 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-047 | `POST /requests` | [nv-09](../../../specs/nv-09/spec.md) |
| EP-048 | `GET /requests` | [nv-09](../../../specs/nv-09/spec.md) |
| EP-049 | `GET /requests/{id}` | [nv-09](../../../specs/nv-09/spec.md) |
| EP-050 | `PATCH /requests/{id}` | [nv-09](../../../specs/nv-09/spec.md) |
| EP-051 | `POST /requests/{id}/close` | [nv-09](../../../specs/nv-09/spec.md) |
| EP-052 | `DELETE /requests/{id}` | [nv-09](../../../specs/nv-09/spec.md) |
| EP-053 | `PUT /requests/{id}/interest` | [nv-20](../../../specs/nv-20/spec.md) |
| EP-054 | `DELETE /requests/{id}/interest` | [nv-20](../../../specs/nv-20/spec.md) |
| EP-055 | `GET /requests/{id}/interests` | [nv-20](../../../specs/nv-20/spec.md) |
| EP-056 | `PUT /requests/{id}/shortlist/{providerId}` | [nv-20](../../../specs/nv-20/spec.md) |
| EP-057 | `DELETE /requests/{id}/shortlist/{providerId}` | [nv-20](../../../specs/nv-20/spec.md) |
| EP-058 | `POST /orders` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-059 | `GET /me/orders` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-060 | `GET /orders/{id}` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-061 | `POST /orders/{id}/accept` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-062 | `POST /orders/{id}/reject` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-063 | `POST /orders/{id}/cancel` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-064 | `POST /orders/{id}/complete` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-065 | `PATCH /orders/{id}/address` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-088 | `GET /internal/orders/{id}/review-eligibility` | [nv-10](../../../specs/nv-10/spec.md) |
| EP-092 | `GET /orders/{id}/contact` | [nv-25](../../../specs/nv-25/spec.md) |
| EP-097 | `PATCH /orders/{id}/contact-preference` | [nv-25](../../../specs/nv-25/spec.md) |

## Criterios antes de implementar/aceptar

- Dos aceptaciones concurrentes no crean dos asignaciones; cancelar/aceptar/resolver carreras tienen salida definida.
- Reentrega del comando/evento no duplica pedido o efecto.
- Cliente ajeno/estado incorrecto/consentimiento ausente no ve telefono o domicilio exacto; cancelacion revalida acceso.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
