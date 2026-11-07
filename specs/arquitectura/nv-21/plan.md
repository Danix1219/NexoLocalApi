# PLAN — nv-21

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Asociar token por sesión, deduplicar bandeja/entrega y revalidar destino. Dependencias: nv-04 y primer productor disponible; integrar nv-08..11 progresivamente.

## 2. Diseño técnico

S07. Push mínimo sin PII; logout desvincula; sin permiso permanece bandeja. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Reentrega no duplica; token de cuenta anterior no recibe contenido nuevo; destino retirado revalida autorización. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-083 | FR-283 → T-283 → AC-283 | Actor Propietario; datos `devices`; Asociado a sid actual; token único; rotación Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-084 | FR-284 → T-284 → AC-284 | Actor Propietario; datos `devices`; Idempotente; no afectar otro usuario Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-085 | FR-285 → T-285 → AC-285 | Actor Propietario; datos `notifications`; Payload mínimo, acceso al destino se revalida al abrir Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-086 | FR-286 → T-286 → AC-286 | Actor Propietario; datos `notifications`; Solo aviso propio, idempotente Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| S07 Notificaciones | FR-608 → T-608 → AC-608 | Delimitar S07 con REST y consumo de eventos, dependencias FCM, colas y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Rutas, propietario y orden verificable 0.4.1

Dueño: S07 Notifications. Rutas objetivo: `services/notifications/src/ y services/notifications/tests/` (no creadas por este plan). Alcance propio: dispositivos y entrega push con primer productor; desvinculacion por cuenta/sesion. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Refinamiento de patrones del propietario

T-960 atiende FR-960/NFR-960 y AC-960 con el [perfil S07](../../../docs/arquitectura/servicios/s07-notifications.md). Orden: T-950 → T-960 → T-001. Registrar para la historia patrón/motivo, capas/puertos, frontera atómica, contrato, concurrencia y recuperación, suites y evidencia esperada. Las carpetas objetivo del perfil no son proyectos creados. [Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) aplican según la superficie; S08 permanece condicional.

## Base física ampliada por encargo explícito

El usuario autoriza el scaffold de todos los propietarios, además de G01/S01. [Estado y evidencia](../../../docs/inicio-backend/base-todos-microservicios.md). Proyectos/carpetas según perfil y host independiente; no equivale a implementación de patrones de negocio, Ready o aceptación. S08 permanece condicional; sin proveedores nuevos ni publicación. Consolidación Git/CI real y revisión siguen pendientes.

## Arquitectura interna revisada por responsabilidad

Perfil S07: **event-driven-delivery**; proyectos activos: Delivery, Adapters, Api, Worker. El registro/consulta HTTP y la entrega asíncrona tienen tiempos y fallos distintos. Patrones: API y Worker separados, inbox durable, planificación, reintentos acotados y cola de errores. La [selección por caso de uso](../../../docs/arquitectura/seleccion-arquitecturas.md) sustituye la plantilla común de cuatro capas donde no aplica. Refinamiento y comprobación de la tarea de perfil ya existente; no añade operaciones ni declara Ready.
