# Índice de incrementos SDD — 0.4.0

Estado de todos: Borrador documental. Las primeras fases conservan gobierno/fundamento y coordinan subcapacidades; no se implementa todo un dominio de una vez. [Cobertura local](../docs/cobertura-funcional.md) y [mapa bilateral](../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md).

| Incremento | Objetivo | Flujo |
|---|---|---|
| [nv-00](nv-00/spec.md) | Constitución y gobierno | [plan](nv-00/plan.md), [tareas](nv-00/task.md), [gates](nv-00/checklists/gates.md) |
| [nv-01](nv-01/spec.md) | Fundación de servicios y entorno | [plan](nv-01/plan.md), [tareas](nv-01/task.md), [gates](nv-01/checklists/gates.md) |
| [nv-02](nv-02/spec.md) | Molde de incremento y contratos | [plan](nv-02/plan.md), [tareas](nv-02/task.md), [gates](nv-02/checklists/gates.md) |
| [nv-03](nv-03/spec.md) | Gates de calidad y DevSecOps | [plan](nv-03/plan.md), [tareas](nv-03/task.md), [gates](nv-03/checklists/gates.md) |
| [nv-04](nv-04/spec.md) | Identidad, sesión y aislamiento de cuenta | [plan](nv-04/plan.md), [tareas](nv-04/task.md), [gates](nv-04/checklists/gates.md) |
| [nv-05](nv-05/spec.md) | Moderación y medios antes de persistir | [plan](nv-05/plan.md), [tareas](nv-05/task.md), [gates](nv-05/checklists/gates.md) |
| [nv-06](nv-06/spec.md) | Perfiles, catálogo, cobertura y edición | [plan](nv-06/plan.md), [tareas](nv-06/task.md), [gates](nv-06/checklists/gates.md) |
| [nv-07](nv-07/spec.md) | Búsqueda, mapa, comparación y feed público | [plan](nv-07/plan.md), [tareas](nv-07/task.md), [gates](nv-07/checklists/gates.md) |
| [nv-08](nv-08/spec.md) | Trabajos, experiencias, menciones y guardados | [plan](nv-08/plan.md), [tareas](nv-08/task.md), [gates](nv-08/checklists/gates.md) |
| [nv-09](nv-09/spec.md) | Solicitudes, intereses y candidatos | [plan](nv-09/plan.md), [tareas](nv-09/task.md), [gates](nv-09/checklists/gates.md) |
| [nv-10](nv-10/spec.md) | Pedidos, privacidad de contacto y resolución | [plan](nv-10/plan.md), [tareas](nv-10/task.md), [gates](nv-10/checklists/gates.md) |
| [nv-11](nv-11/spec.md) | Conversaciones, mensajes y avisos en vivo | [plan](nv-11/plan.md), [tareas](nv-11/task.md), [gates](nv-11/checklists/gates.md) |
| [nv-12](nv-12/spec.md) | Notificaciones y eliminación distribuida completa | [plan](nv-12/plan.md), [tareas](nv-12/task.md), [gates](nv-12/checklists/gates.md) |
| [nv-13](nv-13/spec.md) | ML y asistencia generativa evaluables | [plan](nv-13/plan.md), [tareas](nv-13/task.md), [gates](nv-13/checklists/gates.md) |
| [nv-14](nv-14/spec.md) | Infraestructura, landing, DevSecOps y publicación | [plan](nv-14/plan.md), [tareas](nv-14/task.md), [gates](nv-14/checklists/gates.md) |
| [nv-15](nv-15/spec.md) | Proyección de relaciones y lectura del grafo | [plan](nv-15/plan.md), [tareas](nv-15/task.md), [gates](nv-15/checklists/gates.md) |
| [nv-16](nv-16/spec.md) | Cuenta, recuperación y sesiones | [plan](nv-16/plan.md), [tareas](nv-16/task.md), [gates](nv-16/checklists/gates.md) |
| [nv-17](nv-17/spec.md) | Catálogo y administración de categorías | [plan](nv-17/plan.md), [tareas](nv-17/task.md), [gates](nv-17/checklists/gates.md) |
| [nv-18](nv-18/spec.md) | Reacciones, guardados e indicadores | [plan](nv-18/plan.md), [tareas](nv-18/task.md), [gates](nv-18/checklists/gates.md) |
| [nv-19](nv-19/spec.md) | Experiencias, menciones y reputación | [plan](nv-19/plan.md), [tareas](nv-19/task.md), [gates](nv-19/checklists/gates.md) |
| [nv-20](nv-20/spec.md) | Intereses y candidatos | [plan](nv-20/plan.md), [tareas](nv-20/task.md), [gates](nv-20/checklists/gates.md) |
| [nv-21](nv-21/spec.md) | Dispositivos y notificaciones push | [plan](nv-21/plan.md), [tareas](nv-21/task.md), [gates](nv-21/checklists/gates.md) |
| [nv-22](nv-22/spec.md) | Operación de datos, migraciones y recuperación | [plan](nv-22/plan.md), [tareas](nv-22/task.md), [gates](nv-22/checklists/gates.md) |
| [nv-23](nv-23/spec.md) | Landing y expediente de evaluación | [plan](nv-23/plan.md), [tareas](nv-23/task.md), [gates](nv-23/checklists/gates.md) |
| [nv-24](nv-24/spec.md) | Administración de confianza | [plan](nv-24/plan.md), [tareas](nv-24/task.md), [gates](nv-24/checklists/gates.md) |
| [nv-25](nv-25/spec.md) | Consentimiento y políticas de contacto | [plan](nv-25/plan.md), [tareas](nv-25/task.md), [gates](nv-25/checklists/gates.md) |

## Dependencias por historia y por cierre

Fundamentos preceden identidad mínima. Catálogo público puede prepararse antes del perfil completo; administración requiere identidad. Publicación de trabajo precede integración con guardados/experiencias. Experiencia declarada no espera pedido; la vinculada sí espera done/eligibilidad. Solicitud precede interés/candidato. Pedido directo precede entrada por chat; contacto privado y preferencias se cierran al integrar su capacidad. Eventos/push/borrado crecen con cada productor/propietario habilitado.

Las dependencias de cierre integrado no se usan para impedir un subset anterior ya definido: las specs y planes delimitan precondiciones de la historia. Primer subset identidad mantiene EP-001/002/003/004/005/007/008; otras operaciones tienen historia separada. S08/shortlist son opcionales. La entrega global exige todas las capacidades obligatorias, no solo la primera vertical. No prometer Ready por este índice.
