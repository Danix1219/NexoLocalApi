# S01 — Identidad y sesion

> **Mapa de dominios, no unidad desplegable.** Tras la **enmienda E-001**, este servicio se implementa en el modulo **Identity** de la API modular por dominios, con datos en `nexo_identity`. Comparte host y se despliega en una app de App Service con **2 instancias**. El contenido funcional de abajo sigue vigente; lo que cambia es donde vive. Ver [estructura-backend.md](../../estructura-backend.md).


Diseño de implementacion propuesto, 2026-11-03. Cuatro proyectos existentes; Api vacia y sin negocio/proveedores. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/identity/`. accounts, sessions, otp_challenges y deletions. Cuenta, familia de sesion y desafio de verificacion son limites a refinar, sin agregado global de usuario/perfil/pedido. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-04](../../../specs/arquitectura/nv-04/spec.md), [nv-12](../../../specs/arquitectura/nv-12/spec.md), [nv-16](../../../specs/arquitectura/nv-04/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **hexagonal**. Sesiones, OTP y borrado requieren invariantes aisladas y proveedores reemplazables.

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
| Puertos/adaptadores y casos de uso | Separar seguridad, persistencia, SMS y reloj de la orquestacion de identidad. | Algoritmos, factores de trabajo, TTL/cuotas y proveedor se ratifican por historia. |
| Estado de sesion + concurrencia atomica | Rotacion de refresh, deteccion de replay e invalidacion coherente en dos replicas. | Digest y revocacion durables; Redis no es fuente unica expulsable de autorizacion. |
| Outbox y coordinacion de borrado | Bloquear cuenta al iniciar y coordinar acuses de dueños para eliminacion distribuida. | La eliminacion usa avance/reintento: no resucitar cuenta o PII como compensacion. |

## Persistencia y consistencia

Verificacion/consumo de prueba de un uso y cambio de estado deben impedir reutilizacion concurrente. Sesion/refresh se actualizan con control de version/atomicidad. Inicio de borrado + evento se confirman localmente. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Puertos para cuenta/sesion/desafio, seguridad de credenciales/tokens, SMS y reloj; adaptadores propios. No acceso al perfil comercial de S02 como documento de Identity.

Flujo: G01 → S01.Api → caso → regla/puerto → commit local → resultado. Borrado: bloqueo/revocacion → outbox → acuses por dueño → cierre cuando todos los habilitados responden.

## Operaciones ya asignadas (sin inventar endpoints)

15 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-001 | `POST /auth/otp` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-002 | `POST /auth/otp/verify` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-003 | `POST /auth/register` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-004 | `POST /auth/login` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-005 | `POST /auth/refresh` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-006 | `POST /auth/password-reset` | [nv-16](../../../specs/arquitectura/nv-04/spec.md) |
| EP-007 | `POST /auth/logout` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-008 | `GET /me` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-009 | `PATCH /me` | [nv-16](../../../specs/arquitectura/nv-04/spec.md) |
| EP-010 | `GET /me/sessions` | [nv-16](../../../specs/arquitectura/nv-04/spec.md) |
| EP-011 | `DELETE /me/sessions/{sid}` | [nv-16](../../../specs/arquitectura/nv-04/spec.md) |
| EP-012 | `POST /me/deletion` | [nv-12](../../../specs/arquitectura/nv-12/spec.md) |
| EP-013 | `GET /me/deletion/{id}` | [nv-12](../../../specs/arquitectura/nv-12/spec.md) |
| EP-073 | `POST /realtime/ticket` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |
| EP-101 | `POST /internal/accounts/visibility-batch` | [nv-04](../../../specs/arquitectura/nv-04/spec.md) |

## Criterios antes de implementar/aceptar

- OTP expirado/reutilizado/reenvio y login invalidos no enumeran cuentas.
- Dos refresh simultaneos y replay tienen resultado definido; logout en A revoca uso en B.
- Fallo de revocacion no permite acceso por omision; borrado incompleto permanece pendiente.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
