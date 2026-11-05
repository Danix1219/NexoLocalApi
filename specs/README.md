# Índice de specs SDD — 0.5.0

Las specs viven separadas por tipo: [arquitectura/](arquitectura/) contiene los 26 incrementos de capacidad, gobierno y frontera (sin endpoint propio nuevo); [endpoints/](endpoints/) contiene una spec por operación HTTP a construir, derivada de la capacidad ya documentada en arquitectura. Misma estructura en ambas: `spec.md → plan.md → task.md → checklists/gates.md`.

## Arquitectura y capacidad

Estado de todos: Borrador documental. Las primeras fases conservan gobierno/fundamento y coordinan subcapacidades; no se implementa todo un dominio de una vez. [Cobertura local](../docs/cobertura-funcional.md) y [mapa bilateral](../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md).

| Incremento | Objetivo | Flujo |
|---|---|---|
| [nv-00](arquitectura/nv-00/spec.md) | Constitución y gobierno | [plan](arquitectura/nv-00/plan.md), [tareas](arquitectura/nv-00/task.md), [gates](arquitectura/nv-00/checklists/gates.md) |
| [nv-01](arquitectura/nv-01/spec.md) | Fundación de servicios y entorno | [plan](arquitectura/nv-01/plan.md), [tareas](arquitectura/nv-01/task.md), [gates](arquitectura/nv-01/checklists/gates.md) |
| [nv-02](arquitectura/nv-02/spec.md) | Molde de incremento y contratos | [plan](arquitectura/nv-02/plan.md), [tareas](arquitectura/nv-02/task.md), [gates](arquitectura/nv-02/checklists/gates.md) |
| [nv-03](arquitectura/nv-03/spec.md) | Gates de calidad y DevSecOps | [plan](arquitectura/nv-03/plan.md), [tareas](arquitectura/nv-03/task.md), [gates](arquitectura/nv-03/checklists/gates.md) |
| [nv-04](arquitectura/nv-04/spec.md) | Identidad, sesión y aislamiento de cuenta | [plan](arquitectura/nv-04/plan.md), [tareas](arquitectura/nv-04/task.md), [gates](arquitectura/nv-04/checklists/gates.md) |
| [nv-05](arquitectura/nv-05/spec.md) | Moderación y medios antes de persistir | [plan](arquitectura/nv-05/plan.md), [tareas](arquitectura/nv-05/task.md), [gates](arquitectura/nv-05/checklists/gates.md) |
| [nv-06](arquitectura/nv-06/spec.md) | Perfiles, catálogo, cobertura y edición | [plan](arquitectura/nv-06/plan.md), [tareas](arquitectura/nv-06/task.md), [gates](arquitectura/nv-06/checklists/gates.md) |
| [nv-07](arquitectura/nv-07/spec.md) | Búsqueda, mapa, comparación y feed público | [plan](arquitectura/nv-07/plan.md), [tareas](arquitectura/nv-07/task.md), [gates](arquitectura/nv-07/checklists/gates.md) |
| [nv-08](arquitectura/nv-08/spec.md) | Trabajos, experiencias, menciones y guardados | [plan](arquitectura/nv-08/plan.md), [tareas](arquitectura/nv-08/task.md), [gates](arquitectura/nv-08/checklists/gates.md) |
| [nv-09](arquitectura/nv-09/spec.md) | Solicitudes, intereses y candidatos | [plan](arquitectura/nv-09/plan.md), [tareas](arquitectura/nv-09/task.md), [gates](arquitectura/nv-09/checklists/gates.md) |
| [nv-10](arquitectura/nv-10/spec.md) | Pedidos, privacidad de contacto y resolución | [plan](arquitectura/nv-10/plan.md), [tareas](arquitectura/nv-10/task.md), [gates](arquitectura/nv-10/checklists/gates.md) |
| [nv-11](arquitectura/nv-11/spec.md) | Conversaciones, mensajes y avisos en vivo | [plan](arquitectura/nv-11/plan.md), [tareas](arquitectura/nv-11/task.md), [gates](arquitectura/nv-11/checklists/gates.md) |
| [nv-12](arquitectura/nv-12/spec.md) | Notificaciones y eliminación distribuida completa | [plan](arquitectura/nv-12/plan.md), [tareas](arquitectura/nv-12/task.md), [gates](arquitectura/nv-12/checklists/gates.md) |
| [nv-13](arquitectura/nv-13/spec.md) | ML y asistencia generativa evaluables | [plan](arquitectura/nv-13/plan.md), [tareas](arquitectura/nv-13/task.md), [gates](arquitectura/nv-13/checklists/gates.md) |
| [nv-14](arquitectura/nv-14/spec.md) | Infraestructura, landing, DevSecOps y publicación | [plan](arquitectura/nv-14/plan.md), [tareas](arquitectura/nv-14/task.md), [gates](arquitectura/nv-14/checklists/gates.md) |
| [nv-15](arquitectura/nv-15/spec.md) | Proyección de relaciones y lectura del grafo | [plan](arquitectura/nv-15/plan.md), [tareas](arquitectura/nv-15/task.md), [gates](arquitectura/nv-15/checklists/gates.md) |
| [nv-16](arquitectura/nv-16/spec.md) | Cuenta, recuperación y sesiones | [plan](arquitectura/nv-16/plan.md), [tareas](arquitectura/nv-16/task.md), [gates](arquitectura/nv-16/checklists/gates.md) |
| [nv-17](arquitectura/nv-17/spec.md) | Catálogo y administración de categorías | [plan](arquitectura/nv-17/plan.md), [tareas](arquitectura/nv-17/task.md), [gates](arquitectura/nv-17/checklists/gates.md) |
| [nv-18](arquitectura/nv-18/spec.md) | Reacciones, guardados e indicadores | [plan](arquitectura/nv-18/plan.md), [tareas](arquitectura/nv-18/task.md), [gates](arquitectura/nv-18/checklists/gates.md) |
| [nv-19](arquitectura/nv-19/spec.md) | Experiencias, menciones y reputación | [plan](arquitectura/nv-19/plan.md), [tareas](arquitectura/nv-19/task.md), [gates](arquitectura/nv-19/checklists/gates.md) |
| [nv-20](arquitectura/nv-20/spec.md) | Intereses y candidatos | [plan](arquitectura/nv-20/plan.md), [tareas](arquitectura/nv-20/task.md), [gates](arquitectura/nv-20/checklists/gates.md) |
| [nv-21](arquitectura/nv-21/spec.md) | Dispositivos y notificaciones push | [plan](arquitectura/nv-21/plan.md), [tareas](arquitectura/nv-21/task.md), [gates](arquitectura/nv-21/checklists/gates.md) |
| [nv-22](arquitectura/nv-22/spec.md) | Operación de datos, migraciones y recuperación | [plan](arquitectura/nv-22/plan.md), [tareas](arquitectura/nv-22/task.md), [gates](arquitectura/nv-22/checklists/gates.md) |
| [nv-23](arquitectura/nv-23/spec.md) | Landing y expediente de evaluación | [plan](arquitectura/nv-23/plan.md), [tareas](arquitectura/nv-23/task.md), [gates](arquitectura/nv-23/checklists/gates.md) |
| [nv-24](arquitectura/nv-24/spec.md) | Administración de confianza | [plan](arquitectura/nv-24/plan.md), [tareas](arquitectura/nv-24/task.md), [gates](arquitectura/nv-24/checklists/gates.md) |
| [nv-25](arquitectura/nv-25/spec.md) | Consentimiento y políticas de contacto | [plan](arquitectura/nv-25/plan.md), [tareas](arquitectura/nv-25/task.md), [gates](arquitectura/nv-25/checklists/gates.md) |

## Endpoints — primeros 7 del usuario que solicita servicio

Identificador de cuenta: **correo electrónico** (no teléfono). Las rutas `/auth/otp*` se conservan del inventario; el canal cambia a correo. Decisión de contrato en el [registro](../docs/decisions/registro.md).

| Spec | Operaciones | Dueño | Capacidad origen | Flujo |
|---|---|---|---|---|
| [ep-01](endpoints/ep-01/spec.md) Pantalla principal | EP-030 `GET /feed` | S03 | [nv-07](arquitectura/nv-07/spec.md) | [plan](endpoints/ep-01/plan.md), [tareas](endpoints/ep-01/task.md), [gates](endpoints/ep-01/checklists/gates.md) |
| [ep-02](endpoints/ep-02/spec.md) Registro con correo | EP-003 `POST /auth/register` | S01 | [nv-04](arquitectura/nv-04/spec.md) | [plan](endpoints/ep-02/plan.md), [tareas](endpoints/ep-02/task.md), [gates](endpoints/ep-02/checklists/gates.md) |
| [ep-03](endpoints/ep-03/spec.md) Verificación de correo | EP-001 `POST /auth/otp`, EP-002 `POST /auth/otp/verify` | S01 | [nv-04](arquitectura/nv-04/spec.md) | [plan](endpoints/ep-03/plan.md), [tareas](endpoints/ep-03/task.md), [gates](endpoints/ep-03/checklists/gates.md) |
| [ep-04](endpoints/ep-04/spec.md) Inicio de sesión | EP-004 `POST /auth/login` | S01 | [nv-04](arquitectura/nv-04/spec.md) | [plan](endpoints/ep-04/plan.md), [tareas](endpoints/ep-04/task.md), [gates](endpoints/ep-04/checklists/gates.md) |
| [ep-05](endpoints/ep-05/spec.md) Continuidad y cierre de sesión | EP-005 `POST /auth/refresh`, EP-007 `POST /auth/logout` | S01 | [nv-04](arquitectura/nv-04/spec.md) | [plan](endpoints/ep-05/plan.md), [tareas](endpoints/ep-05/task.md), [gates](endpoints/ep-05/checklists/gates.md) |
| [ep-06](endpoints/ep-06/spec.md) Cuenta propia | EP-008 `GET /me` | S01 | [nv-04](arquitectura/nv-04/spec.md) | [plan](endpoints/ep-06/plan.md), [tareas](endpoints/ep-06/task.md), [gates](endpoints/ep-06/checklists/gates.md) |
| [ep-07](endpoints/ep-07/spec.md) Recuperación de contraseña | EP-006 `POST /auth/password-reset` | S01 | [nv-16](arquitectura/nv-16/spec.md) | [plan](endpoints/ep-07/plan.md), [tareas](endpoints/ep-07/task.md), [gates](endpoints/ep-07/checklists/gates.md) |

Orden de construcción: ep-03 → ep-02 → ep-04 → ep-05 → ep-06 → ep-07; ep-01 es independiente y no exige sesión.

## Dependencias por historia y por cierre

Fundamentos preceden identidad mínima. Catálogo público puede prepararse antes del perfil completo; administración requiere identidad. Publicación de trabajo precede integración con guardados/experiencias. Experiencia declarada no espera pedido; la vinculada sí espera done/eligibilidad. Solicitud precede interés/candidato. Pedido directo precede entrada por chat; contacto privado y preferencias se cierran al integrar su capacidad. Eventos/push/borrado crecen con cada productor/propietario habilitado.

Las dependencias de cierre integrado no se usan para impedir un subset anterior ya definido: las specs y planes delimitan precondiciones de la historia. Primer subset identidad mantiene EP-001/002/003/004/005/007/008; otras operaciones tienen historia separada. S08/shortlist son opcionales. La entrega global exige todas las capacidades obligatorias, no solo la primera vertical. No prometer Ready por este índice.
