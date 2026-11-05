# PLAN — ep-07

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Restablecer el acceso con una prueba de recuperación del correo y revocar las sesiones previas. Capacidad de origen: [nv-16](../../arquitectura/nv-16/spec.md). Dependencias de fundamento: nv-01, nv-02, nv-03 y datos de nv-22.

## 2. Diseño técnico

S01 Identity. Domain para reglas, Application para casos y puertos, Infrastructure para persistencia y adaptadores, Api para HTTP y composición. Sin proyecto nuevo: rutas objetivo `services/identity/src/ y services/identity/tests/`.

## 3. Pipeline / orden de ejecución

T-950 → T-001 → T-002 → T-003 → T-004 y T-005 → T-006.

## 4. Riesgos / Deuda

Cambio de canal: la prueba proviene del correo verificado en ep-03. El proveedor de base y caché sigue sin ratificar en D01: [registro](../../../docs/decisions/registro.md).

## 5. Verificación

AC de spec y matriz local; lint documental hoy, pruebas reales por caso positivo, negativo y fallo al implementar.

## Cobertura por operación propia

| EP | Método y ruta | Requisito → tarea → criterio | Diseño y comprobación |
|---|---|---|---|
| EP-006 | `POST /auth/password-reset` | FR-001/FR-004 → T-002, T-003 → AC-001, AC-004 | Actor Prueba de recuperación de correo; datos `accounts`; No aceptar prueba de registro ni código ya consumido. |

No copiar payloads en la app: el contrato autoritativo es el [inventario](../../../docs/contracts/inventario.json).

## Perfiles de arquitectura aplicables

[Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) y el perfil del propietario en [docs/arquitectura/servicios](../../../docs/arquitectura/servicios/). Reutilizar el propietario; no crear un servicio por spec.
