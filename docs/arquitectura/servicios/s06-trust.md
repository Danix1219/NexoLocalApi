# S06 — Confianza, contenido y medios

> **Mapa de dominios, no unidad desplegable.** Tras la **enmienda E-001**, este servicio se implementa en el modulo **Engagement** de la API modular por dominios, con datos en `nexo_engagement`. Comparte host y se despliega en una app de App Service con **2 instancias**. El contenido funcional de abajo sigue vigente; lo que cambia es donde vive. Ver [estructura-backend.md](../../estructura-backend.md).


Diseño de implementacion propuesto, 2026-11-03. Base física revisada: 3 proyectos, sin negocio ni almacenamiento configurado. Esta ficha no instala patrones/proveedores ni convierte la base en producto aprobado. [Politicas comunes](../politicas-comunes.md) y [mapa general](../README.md).

## Limite y dueño

Ruta: `services/trust/`. media, reports y decisions. Decision de aprobacion vinculada a huella/autor/proposito/version de politica; permisos de medios y reportes con acceso minimo. [Datos autoritativos](../../modelo-datos.md).

Specs de referencia: [nv-05](../../../specs/arquitectura/nv-05/spec.md), [nv-13](../../../specs/arquitectura/nv-13/spec.md), [nv-24](../../../specs/arquitectura/nv-05/spec.md). La matriz de requisitos/tareas de cada spec conserva el alcance y dependencias.

## Estructura del propietario

Arquitectura elegida para el scaffold: **evaluation-pipeline**. La decisión combina políticas y proveedores externos; un fallo no puede aprobar contenido por omisión.

```text
src/Evaluation/
src/Adapters/
src/Api/
```

- `Evaluation` → sin dependencias de otros proyectos.
- `Adapters` → `Evaluation`.
- `Api` → `Evaluation`, `Adapters`.

Los proyectos de núcleo contienen reglas/casos por capacidad y puertos, sin SDK de datos o transporte. Los adaptadores implementan esas capacidades; el host compone dependencias. No hay referencias entre servicios. Las rutas activas y la política verificable sustituyen la plantilla de cuatro capas anterior. Patrones pendientes de implementación, sin contratos o proveedores ficticios.

## Patrones y condiciones

| Patron | Problema que resuelve | Limite/decision |
|---|---|---|
| Policy/Strategy y adaptadores de proveedor | Separar regla de decision, evaluacion externa y almacenamiento de medios. | No sustituir aceptacion real por heuristica o proveedor simulado. |
| Puerta de aprobacion antes de persistir contenido | El dueño solicitante solo confirma contenido aprobado para esa version/autor/proposito. | Contenido rechazado no se almacena en DB, blob publico o logs; procesamiento temporal no se convierte en cuarentena persistida inventada. |
| Auditoria minima y aislamiento de fallos | Decisiones administrativas trazables y timeout/bulkhead de proveedor; GenAI produce borrador editable. | Fallo/indeterminacion no aprueba por omision; no publicar automaticamente borradores IA. |

## Persistencia y consistencia

Decision/estado de medio y evento propio se confirman localmente, con minimo dato permitido. Otros servicios conservan sus transacciones y verifican validez de la aprobacion; no escritura directa de S06 en sus bases. Algoritmos, TTL, indices y motor exactos se fijan por contrato/decision antes de implementar; no se declaran ratificados aqui.

## Puertos e integracion

Puertos de moderacion, medios, decision/reporte y asistencia generativa opcional; adaptadores concretos segun proveedor ratificado. El texto sensible no se convierte en payload de evento.

Flujo: Solicitud autorizada → evaluar sin persistir contenido rechazado → decision con huella/politica → dueño confirma solo la version aprobada. Cambio/expiracion exige nueva decision; admin emite resultado y cada dueño lo aplica.

## Operaciones ya asignadas (sin inventar endpoints)

10 operaciones del [inventario API](../../contracts/inventario.json), todas con contrato formal pendiente. No se reasignan por esta ficha.

| ID | Metodo/ruta del catalogo | Spec primaria |
|---|---|---|
| EP-075 | `POST /media` | [nv-05](../../../specs/arquitectura/nv-05/spec.md) |
| EP-076 | `GET /media/{id}/access` | [nv-05](../../../specs/arquitectura/nv-05/spec.md) |
| EP-077 | `DELETE /media/{id}` | [nv-05](../../../specs/arquitectura/nv-05/spec.md) |
| EP-078 | `POST /reports` | [nv-05](../../../specs/arquitectura/nv-05/spec.md) |
| EP-079 | `GET /me/reports` | [nv-05](../../../specs/arquitectura/nv-05/spec.md) |
| EP-080 | `GET /admin/reports` | [nv-24](../../../specs/arquitectura/nv-05/spec.md) |
| EP-081 | `POST /admin/reports/{id}/decision` | [nv-24](../../../specs/arquitectura/nv-05/spec.md) |
| EP-082 | `POST /assist/draft` | [nv-13](../../../specs/arquitectura/nv-13/spec.md) |
| EP-087 | `POST /internal/moderation/check` | [nv-05](../../../specs/arquitectura/nv-05/spec.md) |
| EP-099 | `GET /catalog/report-reasons` | [nv-05](../../../specs/arquitectura/nv-05/spec.md) |

## Criterios antes de implementar/aceptar

- Proveedor caido/timeout/resultado incierto bloquea persistencia; no se llama aceptacion a una simulacion.
- Reutilizar aprobacion de otro texto/autor/proposito es rechazado.
- Reporte/medio privado solo visible al actor autorizado; salida IA no se publica ni se acepta sin revision correspondiente.

Separar unitario con doble, contrato proveedor/consumidor, integracion real, ensayo entre replicas y dispositivo. Incluir permisos, aislamiento, concurrencia, fallo/reintento y evidencia/revisor de la historia. Diagramas y carpetas no aprueban esos criterios. Mantener D01-D08 pertinentes, Ready y credenciales reales pendientes donde corresponda.

## Aplicación física autorizada

El encargo posterior del 2026-11-03 crea la base de todos los propietarios. Las rutas del perfil están reservadas mediante proyectos y carpetas; los patrones de negocio siguen pendientes de implementación. [Estado vigente y alcance](../../inicio-backend/base-todos-microservicios.md) prevalece sobre las menciones anteriores a carpetas todavía no creadas. S08 conserva su condición opcional.

## Revisión de adecuación por caso de uso

[Decisión, alternativas, costes y criterios de evolución](../seleccion-arquitecturas.md). La nomenclatura de proyectos describe responsabilidades; no exige tecnologías distintas entre servicios. La eficiencia permanece pendiente de medición con carga real.
