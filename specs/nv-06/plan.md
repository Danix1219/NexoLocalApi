# PLAN — nv-06

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Definir persona/negocio, catálogo, cobertura y preferencias; política de completitud y contadores únicos; edición permanente.

Dependencias: nv-04, nv-05.

## 2. Diseño técnico

S02: categories, providers y read_cache; perfil por cuenta único, GeoJSON [longitud,latitud], origen privado y dirección comercial explícita. Límites: tres precios y seis fotos de portafolio.

Autoridad: [contratos](../../docs/contracts/README.md). [Acuerdo API–app](../../../docs_nexo/SDD_ALINEACION.md). Seleccionar solo operaciones de la historia; tipos, errores, permisos y eventos se formalizan antes de consumo. No abrir escrituras cruzadas entre bases.

Archivos existentes a mantener: spec.md, plan.md, task.md, checklists/gates.md y docs vivos vinculados. Rutas de producto, esquemas ejecutables y herramientas por concretar al encargar implementación, sin crear scaffolding en esta fase.

## 3. Pipeline / orden de ejecución

T-006 refinamiento específico precede a T-001 Ready; T-002/T-003 construcción e integración solo con encargo posterior; T-004 seguridad; T-007 comprueba AC-090; T-005 cierra evidencia. [Gates backend](../../docs/roles-y-gates.md) desde la primera vertical, con comandos reales cuando haya proyecto.

## 4. Riesgos / Deuda

Resolver decisiones D01–D08 pertinentes en el acuerdo común; no asumir proveedores, costo, versiones o aprobación de arquitectura. H06/H07/H09/H10: mismo porcentaje/policyVersion en alta y perfil, fotos no cuentan como trabajos, radio cero solo local y edición siempre accesible.

## 5. Verificación

FR-090→T-006/T-007→AC-090. Cada FR/NFR restante conserva tareas del incremento y sus AC; refinar cobertura por historia antes de Ready. Hoy ejecutar scripts/check_sdd.ps1, que solo valida estructura. Pruebas futuras: casos positivos/negativos del propietario, contrato consumidor/provider, dependencia caída y reintento sin duplicación. Evidencia con versión/entorno/resultado y revisor real.

## Integridad constitucional y orden de tareas

T-006/T-020 refinan FR-090/FR-100 y decisiones antes de T-001. T-002 prepara el cambio; T-010 y siguientes atienden cada FR principal; T-021 comprueba AC-100. T-003 integra, T-004 revisa seguridad, T-007 comprueba AC-090 y T-005 cierra. [Tareas](task.md) y tabla de trazabilidad en [spec](spec.md) son autoridad de IDs/dependencias; este orden prevalece sobre resúmenes anteriores.

AC-100: La lectura pública excluye privateOrigin y contacto sin consentimiento; editar horario/precio conserva acceso permanente; seis fotos de portafolio y tres precios no se convierten en trabajos o ventas.

Tipo de evidencia: documental hoy; al implementar, unitario con doble separado de contrato/integración real y, en móvil, dispositivo. Sin comandos de producto válidos mientras no existan proyectos.

## Cobertura por operación propia

| EP | Requisito/tarea/AC | Diseño y comprobación |
|---|---|---|
| EP-016 | FR-216 → T-216 → AC-216 | Actor Cuenta sin perfil; datos `providers`; Un perfil por cuenta; mínimos de registro; texto/medios aprobados; portafolio de registro hasta seis fotos aprobadas; no contar fotos como trabajos ni reutilizar providerId=me entre cuentas. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-017 | FR-217 → T-217 → AC-217 | Actor Público; datos `providers`; Ocultar origen privado y datos de cuenta; teléfono según permisos Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-018 | FR-218 → T-218 → AC-218 | Actor Prestador propietario; datos `providers`; Solo propio; completitud se calcula desde campos persistidos Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-019 | FR-219 → T-219 → AC-219 | Actor Prestador propietario; datos `providers`; Máximo tres precios orientativos en UI inicial; catálogo/categorías aparte; contenido moderado; editor permanente permite corregir campos completos, no solo faltantes. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-020 | FR-220 → T-220 → AC-220 | Actor Prestador propietario; datos `providers`; Validar rango de coordenadas y radio positivo en dom/ambos, cero permitido en local; publicación comercial explícita Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-021 | FR-221 → T-221 → AC-221 | Actor Prestador propietario; datos `providers`; Cierre posterior a apertura para alcance inicial; validar zona IANA Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-022 | FR-222 → T-222 → AC-222 | Actor Prestador propietario; datos `providers`; Cero representa visita gratis; ausencia no equivale a cero; tres filas visibles; no comparar montos de unidades distintas. Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-023 | FR-223 → T-223 → AC-223 | Actor Prestador propietario; datos `providers`; No aumentar reputación por pulsar actualizar Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |
| EP-024 | FR-224 → T-224 → AC-224 | Actor Prestador reautenticado; datos `providers`; Ampliación operativa; definir tratamiento de pedidos abiertos y vínculos Formalizar validación/errores/compatibilidad y realizar prueba positiva, negativa, aislamiento/fallo según superficie. |

No copiar payloads en app: [inventario](../../../API_NexoLocal/docs/contracts/inventario.json) (mismo contexto API). Cada tarea se refina antes de Ready con contrato exacto y rutas reales al encargar código.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| S02 Directorio y descubrimiento | FR-603 → T-603 → AC-603 | Delimitar S02 con REST y eventos, dependencias Mapas, S06, proyecciones de S03/S04 y propietario de datos; probar aislamiento, fallo y observabilidad sin duplicar base. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

### Regresión H06

FR-706 → T-706 → AC-706: Alta/perfil/cuenta reciben la misma política versionada sobre los mismos datos; fórmula por ratificar sin porcentajes inventados. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

### Regresión H07

FR-707 → T-707 → AC-707: Fotos de portafolio, publicaciones, pedidos resueltos y recomendaciones son contadores distintos; seis fotos no equivalen a seis servicios. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

### Regresión H09

FR-709 → T-709 → AC-709: Tras completar horario/precios/condiciones/contacto se conserva acceso a editar y respuesta actualiza vistas/caché. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.

### Regresión H10

FR-710 → T-710 → AC-710: Radio cero en local no vuelve invisible negocio ni muestra menos de 0 km a domicilio; selector y búsqueda respetan modalidad. Fuente: auditoría 1.2.0; prueba nueva de producto pendiente.
