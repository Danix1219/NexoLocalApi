# Plan — CÓMO

Spec origen y rama previstas.

## 1. Contexto

Pendiente de concretar.

## 2. Diseño técnico

Pendiente de concretar.

## 3. Pipeline / orden de ejecución

Pendiente de concretar.

## 4. Riesgos / Deuda

Pendiente de concretar.

## 5. Verificación

Pendiente de concretar.

## Guía de refinamiento Nexo

Identificar fuente y versión, actor, precondición, historia y exclusiones; FR/NFR locales y AC positivos/negativos. Indicar dependencia concreta, spec contraparte y operaciones consumidas sin duplicar payloads. Definir datos/propietario, autorización por estado/campo, reintentos/idempotencia, privacidad y degradación. En móvil incluir carga/vacío/error/offline, permisos y accesibilidad; en API incluir consistencia, eventos y observabilidad.

Cada tarea referencia FR/NFR, AC, criterio y dependencia. Antes de Ready resolver bloqueantes y registrar revisor real; al encargar implementación fijar rutas/comandos/versiones de producto. El checklist enlaza evidencia con entorno/resultado y distingue doble de integración. Sin código, mantener aceptación de producto pendiente y ejecutar solo validación documental.

## Integridad obligatoria de la plantilla completada

Incluir tabla requisito → tarea definida → criterio AC; cada FR/NFR y AC debe estar cubierto. Resolver todos los T-ID citados en plan/checklist y dependencias sin ciclos. No copiar T-006/T-007/T-020/T-021 solo por convención: definirlos o actualizar referencias. Un requisito de API no se implementa como permiso local Flutter. Asignar dueño único a capacidades transversales y enlazar integración; mantener N/A y pendientes justificados, sin marcas de ejecución automáticas.

## Preparacion de estructura y orden de implementacion

Antes de Ready, asignar requisito/criterio/tarea con IDs locales libres que concreten propietario, rutas existentes/objetivo, contrato/decision de entrada, referencias permitidas, pruebas y evidencia. Ready depende de esa tarea documental; nunca hacerla depender del codigo que intenta habilitar. Separar tareas de consolidacion de plantilla (fundacion), contratos y transicion a CI de producto (calidad), sin duplicarlas en cada funcion. La integracion espera sus tareas ejecutables propias. Actualizar matriz y checklist; rutas futuras no son evidencia. Consultar docs/orden-desarrollo-backend.md y docs/estructura-backend.md del repositorio.
