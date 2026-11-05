# PLAN — nv-07

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir búsqueda, mapas, sugerencias, comparación y composición gradual del feed; claves/TTL e invalidación DB/Redis.

Dependencias: nv-06; feed de comunidad espera nv-08, sin bloquear búsqueda de directorio.

## 2. Diseño técnico

S02/G01: filtros normalizados, read_cache con expiresAt comprobado por lector, cursor estable y DTO público. App: recientes y resúmenes públicos; no origen privado ni contactos cacheados.

Autoridad: [contratos](../../../docs/contracts/README.md). [Acuerdo API–app](../../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. H08/H10: comparar solo serviceKey/unidad/moneda/materiales; local con radio cero aparece; denegar GPS conserva entrada manual; edición invalida caché entre réplicas.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Una edición invalida búsqueda/perfil entre réplicas; TTL retrasado no sirve datos caducados; fallback público ante Redis caído respeta límites y nunca entrega PII ni decide autorización con caché.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-025 | FR-225 → T-225 → AC-225 | Actor Público; datos `providers`; Cobertura real por prestador; orden estable; ubicación exacta no sale Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-026 | FR-226 → T-226 → AC-226 | Actor Público; datos `categories`; Tokens/prefijos indexados; debounce móvil; excluir búsqueda privada histórica Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-027 | FR-227 → T-227 → AC-227 | Actor Cuenta autenticada; datos `providers`; Evitar inventar precio u horario faltante; route estática antes de {id}; destacar precio menor solo si servicio, moneda, unidad y materiales/alcance coinciden; de lo contrario no comparable y sin MEJOR. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-028 | FR-228 → T-228 → AC-228 | Actor Público limitado; datos `read_cache`; Adaptador Azure Maps; comprobar licencia/cupos; limitar precisión pública Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-029 | FR-229 → T-229 → AC-229 | Actor Público limitado; datos `read_cache`; No guardar consultas precisas en logs o caché pública sin anonimizar Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-030 | FR-230 → T-230 → AC-230 | Actor Público o cuenta según filtro; datos `posts`; Gateway agrega DTO públicos; para mine exige usuario; fallos parciales explícitos; conservar trabajo cercano por cobertura de prestador o distancia del trabajo <=3 km según postNear, usando ubicación privada solo dentro del servidor. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| R09 Caché en tres niveles | FR-409 → T-409 → AC-409 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

### Regresión H08

FR-708 → T-708 → AC-708: Precio menor solo se destaca con servicio, unidad, moneda, alcance/materiales equivalentes; si falta dato, no comparable con razón. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

## Rutas, propietario y orden verificable 0.4.1

Dueño: S02 Directory y composicion G01. Rutas objetivo: `services/directory/src/, nexolocal.Api/ y pruebas de contrato/recorrido` (no creadas por este plan). Alcance propio: busqueda de directorio primero; feed comunitario integra S03 despues de nv-08. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Perfiles de arquitectura aplicables

Consultar [S02](../../../docs/arquitectura/servicios/s02-directory.md); [S03](../../../docs/arquitectura/servicios/s03-community.md) y las [políticas comunes](../../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
