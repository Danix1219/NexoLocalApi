# PLAN — ep-03

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Emitir y comprobar un código de un uso enviado al correo para obtener prueba de control antes de registrar o recuperar. Capacidad de origen: [nv-04](../../arquitectura/nv-04/spec.md). Dependencias de fundamento: nv-01, nv-02, nv-03 y datos de nv-22.

## 2. Diseño técnico

S01 Identity. Domain para reglas, Application para casos y puertos, Infrastructure para persistencia y adaptadores, Api para HTTP y composición. Sin proyecto nuevo: rutas objetivo `services/identity/src/ y services/identity/tests/`.

## 3. Pipeline / orden de ejecución

T-950 → T-001 → T-002 → T-003 → T-004 y T-005 → T-006.

## 4. Riesgos / Deuda

Cambio de canal: el desafío se envía por correo, no por SMS. Las rutas /auth/otp se conservan; renombrarlas exige decisión de contrato. El proveedor de base y caché sigue sin ratificar en D01: [registro](../../../docs/decisions/registro.md).

## 5. Verificación

AC de spec y matriz local; lint documental hoy, pruebas reales por caso positivo, negativo y fallo al implementar.

## Cobertura por operación propia

| EP | Método y ruta | Requisito → tarea → criterio | Diseño y comprobación |
|---|---|---|---|
| EP-001 | `POST /auth/otp` | FR-001/FR-004 → T-002, T-003 → AC-001, AC-004 | Actor Público limitado; datos `otp_challenges`; Respuesta neutral; límites por IP y por correo; no devolver el código. |
| EP-002 | `POST /auth/otp/verify` | FR-001/FR-004 → T-002, T-003 → AC-001, AC-004 | Actor Público con desafío; datos `otp_challenges`; Verificar intentos, propósito, expiración y no reutilización. |

No copiar payloads en la app: el contrato autoritativo es el [inventario](../../../docs/contracts/inventario.json).

## Perfiles de arquitectura aplicables

[Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) y el perfil del propietario en [docs/arquitectura/servicios](../../../docs/arquitectura/servicios/). Reutilizar el propietario; no crear un servicio por spec.
