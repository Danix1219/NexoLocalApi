# PLAN — ep-05

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Renovar la sesión sin pedir acceso de nuevo y cerrarla de forma idempotente en backend y dispositivos. Capacidad de origen: [nv-04](../../arquitectura/nv-04/spec.md). Dependencias de fundamento: nv-01, nv-02, nv-03 y datos de nv-22.

## 2. Diseño técnico

S01 Identity. Domain para reglas, Application para casos y puertos, Infrastructure para persistencia y adaptadores, Api para HTTP y composición. Sin proyecto nuevo: rutas objetivo `services/identity/src/ y services/identity/tests/`.

## 3. Pipeline / orden de ejecución

T-950 → T-001 → T-002 → T-003 → T-004 y T-005 → T-006.

## 4. Riesgos / Deuda

Sin este par la app no mantiene sesión; la rotación debe ser atómica frente a peticiones móviles en paralelo. El proveedor de base y caché sigue sin ratificar en D01: [registro](../../../docs/decisions/registro.md).

## 5. Verificación

AC de spec y matriz local; lint documental hoy, pruebas reales por caso positivo, negativo y fallo al implementar.

## Cobertura por operación propia

| EP | Método y ruta | Requisito → tarea → criterio | Diseño y comprobación |
|---|---|---|---|
| EP-005 | `POST /auth/refresh` | FR-001/FR-004 → T-002, T-003 → AC-001, AC-004 | Actor Poseedor de refresh; datos `sessions`; Rotación atómica; replay revoca la familia; coordinar peticiones móviles concurrentes. |
| EP-007 | `POST /auth/logout` | FR-001/FR-004 → T-002, T-003 → AC-001, AC-004 | Actor Cuenta autenticada; datos `sessions`; Revocación durable y en caché; cierre de canal en vivo; operación idempotente. |

No copiar payloads en la app: el contrato autoritativo es el [inventario](../../../docs/contracts/inventario.json).

## Perfiles de arquitectura aplicables

[Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) y el perfil del propietario en [docs/arquitectura/servicios](../../../docs/arquitectura/servicios/). Reutilizar el propietario; no crear un servicio por spec.
