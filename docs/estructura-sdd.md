# Estructura documental y autoridad local

Revisión 0.3.0, 2026-11-02. Estado: diseño pendiente de revisión humana. La constitución es fuente rectora; este mapa asigna responsabilidades y no introduce otra jerarquía.

## Carpetas presentes

| Superficie | Qué posee | Cómo se mantiene |
|---|---|---|
| Constitución | Reglas permanentes y límites | Cambio motivado, impacto y ratificación humana; no copiar reglas divergentes |
| AGENTS.md / CLAUDE.md | Procedimiento común / entrada Claude | Codex y Claude leen la misma autoridad y Memoria |
| Memoria.md | Estado, decisiones, deuda y a evitar | Actualizar cierre con resultados reales; historia no es estado vigente |
| specs/ | Qué, cómo, tareas y aceptación por incremento | IDs FR/NFR/AC/T locales, dependencias concretas y matriz de cobertura |
| docs/ | Diseño vivo del propietario e índices | Enlazar contrato/consumo, datos, gates y runbook; no duplicar payloads |
| scripts/check_sdd.ps1 | Comprobación documental existente | Ejecutar desde la raíz; PASS no acredita seguridad ni producto |
| .github/ | Plantilla PR documental | Su presencia no prueba Git remoto/CI/PR publicado |
| .opencode/README.md | Aviso histórico | No herramienta activa ni instrucciones de agentes ejecutables |

Constitución: [fuente única](../.specify/memory/constitution.md). Plantillas: `.specify/templates/`; alias raíz constitution.md y specs/_template.md enlazan autoridad.

## Cadena de cada incremento

Spec: actor/precondición, alcance y exclusiones, FR/NFR, frontera, datos/seguridad, AC y deuda. Plan: decisiones, propietario, cambios futuros, prueba por requisito y orden. Tareas: ID, FR/NFR, aceptación y dependencia; indicar si es preparación documental o ejecución futura. Checklist: evidencia, limitaciones, revisor y estado real.

Las tareas T-006/T-020 refinan AC-090/AC-100 antes de Ready T-001. T-002 prepara cambio, T-010 y siguientes atienden requisitos principales, T-021 comprueba AC-100, T-003 integra, T-004 verifica seguridad, T-007 comprueba AC-090 y T-005 concilia evidencia. Un documento preparado no cumple un criterio de producto. Las tareas se refinan con rutas/versiones exactas al encargar implementación.

## Propiedad y exclusiones

API posee contratos/esquemas, semántica de errores, autorización, transiciones y datos por servicio. App posee pantallas, estado local, permisos y consumo. EP internos/admin no son endpoints de Flutter. No se crea otra API para cada control local.

No existen proyectos compilables, scaffolding, dependencias, perfiles de agentes o infraestructura creados por este mapa. Son objetivos posteriores sujetos a encargo; no añadir carpetas vacías para aparentar avance. Skills recomendadas no se instalan por tener catálogo.

## Fuentes compartidas y comprobación

[Acuerdo](../../docs_nexo/SDD_ALINEACION.md), [cobertura académica](../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [protocolo Codex/Claude](../../docs_nexo/SDD_HERRAMIENTAS_IA.md). [Trazabilidad local](trazabilidad-constitucion.md) enlaza cada sección constitucional con specs/tareas/AC. Revisar además referencias a tareas, cobertura de todos los FR/NFR/AC y ciclos de dependencias: el validador existente no los acredita por sí solo.

## Cobertura vigente 0.4.0

El desglose vigente contiene 26 specs en este contexto. Consultar [cobertura local](cobertura-funcional.md) y [operaciones/funciones bilaterales](../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md). Cada EP propio y cada función/obligación de fuente tiene FR/tarea/AC, no solo mención en índice. Las asignaciones de fases de versiones anteriores quedan como historia; el inventario API y el índice actual son autoridad vigente.
