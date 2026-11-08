# PLAN — nv-23

[Spec](spec.md), [tareas](task.md). Solo diseño.

## 1. Contexto

Coordinar landing, aviso/eliminación, GA4/QR, entrevistas y paquete académico. Dependencias: Preparación nv-00; backend nv-14 y todas las capacidades obligatorias; S08 no bloquea.

## 2. Diseño técnico

Gestión/landing. Landing artefacto separado con dueño web por asignar; no endpoint inventado para entrega. Sin proyecto nuevo; rutas/versiones se concretan al encargar.

## 3. Pipeline / orden de ejecución

T-006/T-020 → T-001 → T-002 → T-010..012/T-021 y tareas EP → T-003/T-004 → T-007/T-005.

## 4. Riesgos / Deuda

Play se comprueba con contraparte; aviso/eliminación/GA4/QR y .rar/scripts reales tienen evidencia sin PII ni secretos. Contratos y decisiones pendientes antes de consumo.

## 5. Verificación

AC de spec y matriz local; lint hoy, pruebas reales según riesgo tras implementar.

## Desglose de funciones y obligaciones

| Fuente | Historia/tarea/criterio | Diseño y prueba |
|---|---|---|
| W01 landing | FR-528 → T-528 → AC-528 | Artefacto web separado, hosting permitido y dueño por asignar; no convertir landing en pantalla Flutter ni en endpoint del backend. |
| R01 Instalación desde Play | FR-401 → T-401 → AC-401 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R17 Landing/Analytics | FR-417 → T-417 → AC-417 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |
| R19 Paquete y evaluación | FR-419 → T-419 → AC-419 | Planear método de comprobación, entorno/responsable y evidencia; no aprobar mientras falten decisiones/contratos o ejecución real cuando corresponda. |

## Orden y frontera vigentes 0.4.0

Cada operación/función/obligación desglosada tiene requisito, tarea y AC en la spec. T-003 espera también esas tareas, no solo el bloque base. La tarea concreta se refina con contrato/revisión, decisión bloqueante y rutas reales antes de ejecutar; no construir todas las historias de una fase de una vez. [Cobertura bilateral](../../../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md) indica primario, integración, opcional o superficie excluida.

## Rutas, propietario y orden verificable 0.4.1

Dueño: Gestion/landing y evidencia backend. Rutas objetivo: `docs/operations/ y expediente enlazado; landing en su superficie acordada` (no creadas por este plan). Alcance propio: no crear un microservicio de expediente o mover Flutter/landing al host Identity. Leer [mapa de carpetas](../../../docs/estructura-backend.md) y [secuencia SDD](../../../docs/orden-desarrollo-backend.md).

Actual: SDD/validadores en este checkout y plantilla Visual Studio separada, sin consolidacion ejecutada. Antes de Ready, T-950 concreta archivos existentes/objetivo y decisiones de la historia; T-001 depende de esa salida ademas del refinamiento. Preservar dependencias de negocio ya declaradas, contratos formales y autoridad API/app.

Durante implementacion, usar Domain→reglas, Application→casos/puertos, Infrastructure→adaptadores propios y Api→HTTP/composicion solo cuando corresponda a este propietario. No crear un servicio por spec; gobierno, contratos, infraestructura y expediente tienen artefactos propios, no Domain ficticio. La construccion concreta reutiliza fundamentos y verifica rutas/suites segun producto real.

Validacion: AC-950 se revisa con lista de archivos/dueño/entrada/salida/suites, mediante T-950 y gate Ready. Es plan de comprobacion, no evidencia de ejecucion. Las tareas de integracion, seguridad y cierre mantienen evidencia por caso positivo/negativo/fallo.

## Perfiles de arquitectura aplicables

Consultar [índice de propietarios](../../../docs/arquitectura/README.md) y las [políticas comunes](../../../docs/arquitectura/politicas-comunes.md) al concretar la preparación estructural de esta historia. Reutilizar el propietario y su contrato; no crear un servicio por spec. Distinguir diseño propuesto, base física y comprobación de producto.
