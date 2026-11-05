# PLAN — ep-01

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Entregar el inicio por zona y modo al usuario que solicita servicio, con tarjetas y cursor estable. Capacidad de origen: [nv-07](../../arquitectura/nv-07/spec.md). Dependencias de fundamento: nv-01, nv-02, nv-03 y datos de nv-22.

## 2. Diseño técnico

S03 Community. Domain para reglas, Application para casos y puertos, Infrastructure para persistencia y adaptadores, Api para HTTP y composición. Sin proyecto nuevo: rutas objetivo `services/community/src/ y services/community/tests/`.

## 3. Pipeline / orden de ejecución

T-950 → T-001 → T-002 → T-003 → T-004 y T-005 → T-006.

## 4. Riesgos / Deuda

Primer endpoint que la app consume sin sesión; el gateway compone y degrada parcialmente sin fingir éxito. El proveedor de base y caché sigue sin ratificar en D01: [registro](../../../docs/decisions/registro.md).

## 5. Verificación

AC de spec y matriz local; lint documental hoy, pruebas reales por caso positivo, negativo y fallo al implementar.

## Cobertura por operación propia

| EP | Método y ruta | Requisito → tarea → criterio | Diseño y comprobación |
|---|---|---|---|
| EP-030 | `GET /feed` | FR-001/FR-004 → T-002, T-003 → AC-001, AC-004 | Actor Público o cuenta según filtro; datos `posts`; Gateway agrega DTO públicos; para mine exige usuario; fallos parciales explícitos; ubicación privada no sale del servidor. |

No copiar payloads en la app: el contrato autoritativo es el [inventario](../../../docs/contracts/inventario.json).

## Perfiles de arquitectura aplicables

[Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) y el perfil del propietario en [docs/arquitectura/servicios](../../../docs/arquitectura/servicios/). Reutilizar el propietario; no crear un servicio por spec.
