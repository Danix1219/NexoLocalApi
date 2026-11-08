# PLAN — nv-08

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Distinguir trabajo, experiencia declarada y opinión vinculada; autoría real, menciones y guardados idempotentes.

Dependencias: nv-06, nv-05; elegibilidad de opinión vinculada depende de nv-10, no la comunidad declarada.

## 2. Diseño técnico

S03: posts, reviews, reactions, saved y metrics. authorId del token; providerId estructurado; único por pedido cuando vinculado. Trabajo 10..200/cuatro fotos; experiencia 10..280/estrellas 1..5.

Autoridad: [contratos](../../../docs/contracts/README.md). [Acuerdo API–app](../../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. H03/H04/H07: cambio de cuenta no mezcla autoría; quitar mención actualiza diálogo; duplicar reintento de guardado no aumenta contador; fotos y pedidos se cuentan separados.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: Una opinión vinculada exige pedido done y dueño confirmado por S04; duplicados no alteran contadores; editar/quitar mención invalida proyecciones y autoría siempre usa ID del token.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-031 | FR-231 → T-231 → AC-231 | Actor Prestador; datos `posts`; Categoría ofrecida, 0..4 medios propios aprobados; título automático hasta 60 caracteres. No exigir campos residuales problema/tiempo. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-032 | FR-232 → T-232 → AC-232 | Actor Público; datos `posts`; Solo publicados; orden por createdAt y _id Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-033 | FR-233 → T-233 → AC-233 | Actor Público; datos `posts`; No exponer domicilio donde se realizó Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-034 | FR-234 → T-234 → AC-234 | Actor Autor; datos `posts`; Volver a moderar cambios; conservar versión anterior solo según política Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-035 | FR-235 → T-235 → AC-235 | Actor Autor o moderador; datos `posts`; Ocultar feed e invalidar caché; purgar medios que queden sin referencias Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| S03 Comunidad | FR-604 → T-604 → AC-604 | Delimitar S03 con REST y eventos, dependencias S02, S04, S06 y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Rutas, propietario y orden verificable 0.4.1

Dueño: S03 Community. Rutas objetivo: `src/Domains/community/ y services/community/tests/` (no creadas por este plan). Alcance propio: publicaciones y coordinacion con este spec/este spec; opinion vinculada espera pedidos. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Refinamiento de patrones del propietario

T-960 atiende FR-960/NFR-960 y AC-960 con el [perfil S03](../../../docs/arquitectura/servicios/s03-community.md). Orden: T-950 → T-960 → T-001. Registrar para la historia patrón/motivo, capas/puertos, frontera atómica, contrato, concurrencia y recuperación, suites y evidencia esperada. Las carpetas objetivo del perfil no son proyectos creados. [Políticas comunes](../../../docs/arquitectura/politicas-comunes.md) aplican según la superficie; S08 permanece condicional.

## Base física ampliada por encargo explícito

El usuario autoriza el scaffold de todos los propietarios, además de G01/S01. [Estado y evidencia](../../../docs/inicio-backend/base-todos-microservicios.md). Proyectos/carpetas según perfil y host independiente; no equivale a implementación de patrones de negocio, Ready o aceptación. S08 permanece condicional; sin proveedores nuevos ni publicación. Consolidación Git/CI real y revisión siguen pendientes.

## Arquitectura interna revisada por responsabilidad

Perfil S03: **feature-modules**; proyectos activos: Modules, Adapters, Api. Publicaciones, reseñas, reacciones y guardados tienen reglas propias, con lecturas de feed que las combinan. Patrones: Módulos locales, moderación previa, unicidad idempotente y proyecciones de métricas. La [selección por caso de uso](../../../docs/arquitectura/seleccion-arquitecturas.md) sustituye la plantilla común de cuatro capas donde no aplica. Refinamiento y comprobación de la tarea de perfil ya existente; no añade operaciones ni declara Ready.
