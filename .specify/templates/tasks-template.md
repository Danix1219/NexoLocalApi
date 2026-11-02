# Tareas — T-ID → FR/NFR

Spec y plan de origen: pendientes.

## Fase A — Preparación
- [ ] T-001 →FR-001. Descripción. Acepta: resultado verificable. Depende de: ninguna.

## Fase B — Implementación futura
- [ ] T-002 →FR-001,NFR-001. Descripción. Acepta: resultado verificable. Depende de: T-001.

## Verificación
No marcar hasta ejecutar criterio y registrar evidencia en checklist/Memoria.

## Guía de refinamiento Nexo

Identificar fuente y versión, actor, precondición, historia y exclusiones; FR/NFR locales y AC positivos/negativos. Indicar dependencia concreta, spec contraparte y operaciones consumidas sin duplicar payloads. Definir datos/propietario, autorización por estado/campo, reintentos/idempotencia, privacidad y degradación. En móvil incluir carga/vacío/error/offline, permisos y accesibilidad; en API incluir consistencia, eventos y observabilidad.

Cada tarea referencia FR/NFR, AC, criterio y dependencia. Antes de Ready resolver bloqueantes y registrar revisor real; al encargar implementación fijar rutas/comandos/versiones de producto. El checklist enlaza evidencia con entorno/resultado y distingue doble de integración. Sin código, mantener aceptación de producto pendiente y ejecutar solo validación documental.

## Integridad obligatoria de la plantilla completada

Incluir tabla requisito → tarea definida → criterio AC; cada FR/NFR y AC debe estar cubierto. Resolver todos los T-ID citados en plan/checklist y dependencias sin ciclos. No copiar T-006/T-007/T-020/T-021 solo por convención: definirlos o actualizar referencias. Un requisito de API no se implementa como permiso local Flutter. Asignar dueño único a capacidades transversales y enlazar integración; mantener N/A y pendientes justificados, sin marcas de ejecución automáticas.
