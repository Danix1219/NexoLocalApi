# PLAN — nv-10

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir transiciones de pedido, concurrencia/asignación y contacto por consentimiento; aceptación y outbox locales a S04.

Dependencias: nv-09, nv-06; entrada perfil/candidato primero; entrada chat integra con nv-11 sin ciclo de implementación.

## 2. Diseño técnico

S04: orders y referencia de asignación en requests; version, participante, precio informativo. pend→ok/cancel; ok→done/cancel. Rechazo se representa con cancel y razón ratificada; done/cancel terminales.

Autoridad: [contratos](../../../docs/contracts/README.md). [Acuerdo API–app](../../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. H01/H02: pendiente/rechazado no expone teléfono/domicilio; cancelar retira también caché/rutas; dos aceptaciones simultáneas dejan solo una asignación activa.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Cancel/rechazo no devuelven teléfono, dirección precisa o ruta; ok/done exige participante y consentimiento vigente; carrera de aceptación deja una asignación y evento coherente, nunca dos.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-058 | FR-258 → T-258 → AC-258 | Actor Solicitante; datos `orders`; Idempotency-Key; impedir activos duplicados; verificar interés si origin=post; heredar de solicitud; directo chat por defecto y both solo por consentimiento explícito. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-059 | FR-259 → T-259 → AC-259 | Actor Participante; datos `orders`; Filtrar clientId o providerId derivado de sesión Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-060 | FR-260 → T-260 → AC-260 | Actor Participante; datos `orders`; Domicilio solo según estado y actor; no basta ocultarlo en Flutter Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-061 | FR-261 → T-261 → AC-261 | Actor Prestador destinatario; datos `orders`; Transacción pedido+solicitud+outbox; solo pend; dirección después de commit Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-062 | FR-262 → T-262 → AC-262 | Actor Prestador destinatario; datos `orders`; Liberar referencia de solicitud y retirar acceso privado Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-063 | FR-263 → T-263 → AC-263 | Actor Cliente o prestador participante; datos `orders`; Solo pend/ok; invalidar autorización de domicilio y emitir evento Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-064 | FR-264 → T-264 → AC-264 | Actor Prestador destinatario; datos `orders`; Solo ok; cerrar solicitud y habilitar calificación del cliente Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-065 | FR-265 → T-265 → AC-265 | Actor Cliente del pedido; datos `orders`; Solo pend/ok; notificar cambio; no aceptar direcciones ajenas en evento público Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-088 | FR-288 → T-288 → AC-288 | Actor Identidad S03; datos `orders`; Devuelve solo pertenencia/done/providerId, nunca domicilio Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| S04 Solicitudes y pedidos | FR-605 → T-605 → AC-605 | Delimitar S04 con REST y eventos, dependencias S01, S02, S06 y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

### Regresión H02

FR-702 → T-702 → AC-702: Cancel/rechazo retira dirección precisa, caché y ruta; solo zona pública permanece. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

## Rutas, propietario y orden verificable 0.4.1

Dueño: S04 Orders. Rutas objetivo: `src/Domains/orders/ y services/orders/tests/` (no creadas por este plan). Alcance propio: pedido/asignacion/outbox locales y privacidad de contacto; integrar este spec sin trasladar reglas a Gateway. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Refinamiento de patrones del propietario

T-960 atiende FR-960/NFR-960 y AC-960 con el [perfil S04](../../../docs/arquitectura/servicios/s04-orders.md). Orden: T-950 → T-960 → T-001. Registrar para la historia patrón/motivo, capas/puertos, frontera atómica, contrato, concurrencia y recuperación, suites y evidencia esperada. Las carpetas objetivo del perfil no son proyectos creados. [Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) aplican según la superficie; S08 permanece condicional.

## Base física ampliada por encargo explícito

El usuario autoriza el scaffold de todos los propietarios, además de G01/S01. [Estado y evidencia](../../../docs/inicio-backend/base-todos-microservicios.md). Proyectos/carpetas según perfil y host independiente; no equivale a implementación de patrones de negocio, Ready o aceptación. S08 permanece condicional; sin proveedores nuevos ni publicación. Consolidación Git/CI real y revisión siguen pendientes.

## Arquitectura interna revisada por responsabilidad

Perfil S04: **ddd-hexagonal**; proyectos activos: Domain, Application, Infrastructure, Api. Asignación y pedido requieren transiciones, permisos y concurrencia bajo una frontera atómica local. Patrones: Agregados y máquina de estados, versión optimista, idempotencia y transactional outbox. La [selección por caso de uso](../../../docs/arquitectura/seleccion-arquitecturas.md) sustituye la plantilla común de cuatro capas donde no aplica. Refinamiento y comprobación de la tarea de perfil ya existente; no añade operaciones ni declara Ready.
