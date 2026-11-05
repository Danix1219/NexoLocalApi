# PLAN — ep-06

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Recuperar la cuenta, capacidades y preferencias del propietario autenticado para pintar su perfil y modos. Capacidad de origen: [nv-04](../../arquitectura/nv-04/spec.md). Dependencias de fundamento: nv-01, nv-02, nv-03 y datos de nv-22.

## 2. Diseño técnico

S01 Identity. Domain para reglas, Application para casos y puertos, Infrastructure para persistencia y adaptadores, Api para HTTP y composición. Sin proyecto nuevo: rutas objetivo `services/identity/src/ y services/identity/tests/`.

## 3. Pipeline / orden de ejecución

T-950 → T-001 → T-002 → T-003 → T-004 y T-005 → T-006.

## 4. Riesgos / Deuda

El summary de solicitudes, experiencias y guardados se compone por llamada autorizada; S01 no consulta datos de otro propietario. El proveedor de base y caché sigue sin ratificar en D01: [registro](../../../docs/decisions/registro.md).

## 5. Verificación

AC de spec y matriz local; lint documental hoy, pruebas reales por caso positivo, negativo y fallo al implementar.

## Cobertura por operación propia

| EP | Método y ruta | Requisito → tarea → criterio | Diseño y comprobación |
|---|---|---|---|
| EP-008 | `GET /me` | FR-001/FR-004 → T-002, T-003 → AC-001, AC-004 | Actor Propietario; datos `accounts`; El modo de UI no concede privilegios. |

No copiar payloads en la app: el contrato autoritativo es el [inventario](../../../docs/contracts/inventario.json).

## Perfiles de arquitectura aplicables

[Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) y el perfil del propietario en [docs/arquitectura/servicios](../../../docs/arquitectura/servicios/). Reutilizar el propietario; no crear un servicio por spec.
