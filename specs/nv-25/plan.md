# PLAN — nv-25

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Coordinar política del prestador, permiso por pedido y consulta vigente antes de divulgar. Dependencias: nv-06/nv-10; nuevas conversaciones integran nv-11.

## 2. Diseño técnico

S02/S04/S05. ShowPhone/chatEnabled y both vigentes; contacto no cacheable; no nuevo microservicio. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Retirar consentimiento bloquea acceso; pendiente/cancelado no revela teléfono cliente; dependencia caída niega divulgación. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-091 | FR-291 → T-291 → AC-291 | Actor Cuenta autenticada; datos `providers`; Respuesta privada sin caché compartida; número validado por identidad; no mezclar en DTO público cacheable; exigir showPhone=true vigente; no confiar solo en canal visible en Flutter. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-092 | FR-292 → T-292 → AC-292 | Actor Participante habilitado; datos `orders`; Cliente: teléfono del prestador solo con showPhone vigente. Prestador: teléfono del cliente solo asignado en ok/done y clientContactPreference=both vigente. Cancelado o revocado: 403/404. No caché de contacto; comprobación autoritativa. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-096 | FR-296 → T-296 → AC-296 | Actor Prestador propietario; datos `providers`; showPhone=false revoca lectura de teléfono inmediatamente. chatEnabled=false impide nuevas conversaciones; propuesta: historial y pedidos activos conservan chat. S05 consulta regla vigente. Alias local wa significa chat interno, no WhatsApp. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-097 | FR-297 → T-297 → AC-297 | Actor Cliente propietario del pedido; datos `orders`; Ampliación de privacidad: en pend/ok/done permite revocar; cancel no admite conceder. Cambiar a both no revela teléfono antes de ok. S04 es autoridad y retira cachés de contacto. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-100 | FR-300 → T-300 → AC-300 | Actor Identidad S04/S05 autorizada; datos `providers`; Solo servicios permitidos; S05 verifica antes de nueva conversación y S04 antes de contacto. No conceder por caché obsoleta; dependencia caída niega divulgación. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

### Regresión H01

FR-701 → T-701 → AC-701: Participante prestador obtiene teléfono cliente solo en ok/done y both vigente; dato no existe en DTO ni copia no autorizados. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.
