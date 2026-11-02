# PLAN — nv-11

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir persistencia de mensajes, bloqueos, ticket temporal y avisos WS; historial durable y contexto por mensaje.

Dependencias: nv-10, nv-05, nv-04.

## 2. Diseño técnico

S05: conversations/messages/blocks; unique conversationId/clientMessageId, sequence durable y contexto requestId/orderId por mensaje; Redis Pub/Sub no historial. WS no lleva JWT en URL persistible.

Autoridad: [contratos](../../docs/contracts/README.md). [Acuerdo API–app](../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. H05/H11: texto rápido/manual utiliza misma intención; reintento mismo ID da una entrega; nuevo pedido cambia cabecera sin reescribir mensajes; logout invalida conexión existente.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Logout/expiración/revocación corta uso de conexión; no token persistible en URL/log; mismo clientMessageId confirma una vez; aviso perdido se recupera de historial autorizado por cursor.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-066 | FR-266 → T-266 → AC-266 | Actor Cliente o prestador habilitado; datos `conversations`; Prestador inicia solo tras asignación; comprobar bloques y membresía; consultar chatEnabled actual para nueva conversación; acceso por rol/contexto autorizado. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-067 | FR-267 → T-267 → AC-267 | Actor Participante; datos `conversations`; No exponer conversaciones de otro modo/miembro Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-068 | FR-268 → T-268 → AC-268 | Actor Miembro; datos `messages`; Autorización en cada página; adjuntos privados bajo control Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-069 | FR-269 → T-269 → AC-269 | Actor Miembro sin bloqueo; datos `messages`; Moderar antes de persistir; retry devuelve mismo mensaje; no respuesta simulada; texto 1..600; respuesta rápida y manual comparten SendMessage sin sobrescribir el texto seleccionado. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-070 | FR-270 → T-270 → AC-270 | Actor Miembro; datos `conversations`; Monótono; no avanzar más allá de mensaje disponible Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-071 | FR-271 → T-271 → AC-271 | Actor Miembro; datos `blocks`; Revalidar bloqueo al enviar; no solo silenciar UI Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-072 | FR-272 → T-272 → AC-272 | Actor Autor del bloqueo; datos `blocks`; No retirar bloqueo impuesto por la otra persona Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-074 | FR-274 → T-274 → AC-274 | Actor Ticket y membresía vigentes; datos `messages`; Validar membresía, sesión y bloqueos durante conexión; reanudar por REST Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| S05 Mensajería | FR-606 → T-606 → AC-606 | Delimitar S05 con REST + WebSocket JSON, dependencias S01, S04, S06 y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

### Regresión H05

FR-705 → T-705 → AC-705: Respuesta rápida y manual usan la misma intención; texto elegido no se sobrescribe por campo vacío y reintento no duplica. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

### Regresión H11

FR-711 → T-711 → AC-711: Nueva orden no reescribe contexto histórico; mensaje/evento tiene orderId/requestId autorizado y cabecera muestra pedido activo. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.
