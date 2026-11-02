# NV-14 — Infraestructura, landing, DevSecOps y publicación

Versión: 0.4.0 | Fecha: 2026-11-02 | Rama prevista: `nv-14`

## 1. Objetivo

Constitución rectora: [fuente única](../../.specify/memory/constitution.md). Aplicación: Constitución §§8/9/10/11; D03/D05/D07/D08.

Infraestructura, landing, DevSecOps y publicación

Infraestructura, landing, DevSecOps y publicación. Servicios involucrados: G01, S01-S07, Flutter, Landing.

No incluye implementación de dominios ajenos ni afirma integraciones ya existentes.

## 2. Alcance / No-alcance

Requisitos Móvil v2: 01, 02, 03, 05, 06, 07, 08, 09, 11, 12, 13, 14, 15, 16, 17, 18, 19. Componentes: W01, M26, M27. Hallazgos: Sin hallazgo específico asociado.

Dependencias: Preparación desde nv-00..04; cierre requiere nv-04..13 y decisiones D03..D08; nv-15 opcional.

- FR-001: Equipo despliega primera vertical y replica servicios con configuración reproducible.
- FR-002: Integrar expediente/landing de nv-23 y operación de datos de nv-22 con despliegue, sin atribuir Play/GA4 al código de la API.

- NFR-001: autorización, aislamiento, privacidad y manejo de errores según constitución.
- NFR-002: trazabilidad y evidencia real; no declarar integraciones reales con dobles.
- NFR-003: calidad y verificación del recorrido según plan/checklist.

Reglas compartidas del recorrido:
- No posponer Play/CI hasta el final; comenzar con primera vertical, cerrar aquí evidencia global.
- Nube y costo cero requieren cuenta/región/cotización; no asumir gratuidad.
- MCP es opcional; entrevistas y referencias docentes requieren evidencia real.

Fuera: funcionalidades ajenas y despliegue no encargado.

- FR-090: Definir CI/CD, dos réplicas, TLS, CDN/observabilidad, ciclo crear/desplegar/eliminar y manifiesto; cerrar paquete académico sin inventar costo cero.
- NFR-090: Frontera, fallos y evidencia según acuerdo común; Reproducir instalación Play y compatibilidad API, logs sin PII, atención por dos réplicas, rollback, .rar limpio y scripts DB; métricas GA4 sin datos personales y QR final decodificado.

- FR-100: Desglosar despliegue, scripts NoSQL/migración/índices/respaldo, recuperación/rollback, eliminación de infraestructura y coordinación landing/paquete académico por artefacto.
- NFR-100: Verificación específica del propietario y límites de la contraparte según Constitución §§8/9/10/11; D03/D05/D07/D08.

### Funcionalidades y obligaciones con dueño explícito

- FR-612: I03 Observabilidad: Logs de eventos, métricas, trazas y alertas. Datos propios: Servicio de observabilidad.
- FR-613: I04 Infraestructura y entrega: Crear, desplegar, verificar y eliminar entornos. Datos propios: Estado remoto protegido.
- FR-408: R08 CI/CD: Atender el requisito académico 8 en su superficie y coordinar la contraparte.
- FR-413: R13 Nube y balanceo: Atender el requisito académico 13 en su superficie y coordinar la contraparte.
- FR-415: R15 Backend operativo: Atender el requisito académico 15 en su superficie y coordinar la contraparte.

## 3. Contratos API

[Endpoints vivos](../../docs/endpoints.md) y [registro de contratos](../../docs/contracts/README.md).

Sin nuevas operaciones asignadas; revisar dependencias/consumo antes de cambiar contrato.

### Frontera y fuentes

[Acuerdo común](../../../docs_nexo/SDD_ALINEACION.md), [fuentes y cobertura](../../../docs_nexo/SDD_COBERTURA_Y_BRECHAS.md) y [flujo Codex/Claude](../../../docs_nexo/SDD_HERRAMIENTAS_IA.md). Contraparte: [spec relacionada](../../../App_NexoLocal/specs/014-entrega/spec.md).

### Asignación primaria vigente 0.4.0

Sin operaciones primarias nuevas: entregable de gobierno/operación, sin endpoint inventado.

## 4. Modelo de datos

Datos propios del servicio, UUID reales y permisos por campo. Diseño vivo: [modelo y datos](../../docs/modelo-datos.md). No copiar tablas/RowVersion SQL del profesor.

### Diseño específico del incremento

Manifiesto por artefacto, contrato, evento y schemaVersion; scripts DB idempotentes, índices/restauración; cuentas, costos, llaves y responsables fuera del documento público. Landing es componente separado.

## 5. Seguridad

- No posponer Play/CI hasta el final; comenzar con primera vertical, cerrar aquí evidencia global.
- Nube y costo cero requieren cuenta/región/cotización; no asumir gratuidad.
- MCP es opcional; entrevistas y referencias docentes requieren evidencia real.

Ver [checklist seguridad](../../docs/asvs-l2-checklist.md).

## 6. Tests / Gates

- [ ] AC-001: Balanceo alcanza dos instancias y logout revoca en ambas.
- [ ] AC-002: Pipeline conserva versiones, pruebas, trazas, rollback y configuración sin secretos.
- [ ] AC-003: Play, privacidad, eliminación, GA4/consentimiento y QR comprobados en destino final.

Suites y gate: [roles y gates](../../docs/roles-y-gates.md). Todo pendiente de implementación y ejecución.

- [ ] AC-090 → FR-090/NFR-090: Reproducir instalación Play y compatibilidad API, logs sin PII, atención por dos réplicas, rollback, .rar limpio y scripts DB; métricas GA4 sin datos personales y QR final decodificado.

- [ ] AC-100 → FR-100/NFR-100: Expediente reúne dos réplicas/TLS/Redis/CDN/observabilidad, crear/desplegar/eliminar con entorno verificado, restauración y .rar sin secretos; Play/GA4/QR son evidencia de contraparte, no tareas de una API.

- [ ] AC-612 → FR-612/NFR-002/NFR-003: No registrar payloads, tokens ni domicilios. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.
- [ ] AC-613 → FR-613/NFR-002/NFR-003: Manifiesto reproducible sin secretos en repositorio. Tecnología/protocolo del libro son propuesta sujeta a constitución, no instalación aprobada.
- [ ] AC-408 → FR-408/NFR-002/NFR-003: Definir pipelines backend y móvil, artefactos inmutables/manifiesto, permisos mínimos, despliegue y rollback con evidencia real. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-413 → FR-413/NFR-002/NFR-003: Acreditar nube elegida, balanceo y TLS; crear/eliminar infraestructura con procedimiento seguro. Terraform es recomendación, no imposición automática. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.
- [ ] AC-415 → FR-415/NFR-002/NFR-003: Verificar AWS/Azure/GCP y costo cero acordado, dos réplicas API, Redis/JWT, CDN, observabilidad, caching/rate-limit/middleware/paginación/índices/serialización/compresión y ciclo de infraestructura. API entrega su parte y enlaza evidencia móvil/web, sin administrar Flutter.

## 7. Plan de entrega

Spec→plan→task→tests/gates→Memoria→checklist→revisión→release. No desplegar por escribir una spec. Ver [runbook](../../docs/runbook-release.md).

## Aprobación

Estado: Borrador documental; sin implementación ni evidencia de producto.
- [ ] Revisor humano identificado — nombre y fecha pendientes.
- [ ] CHECKLIST_PR.md completado con evidencia propia.
- [ ] Sin conflicto con constitución y requisitos del proyecto.

No firmar ni marcar aprobada sin revisión real.

La tabla define cobertura de diseño, no pruebas ejecutadas. El plan concreta método/entorno; el checklist registra evidencia y revisión humana. Los escenarios compartidos no trasladan la propiedad del backend a Flutter.

La traza acredita asignación documental, no pruebas ejecutadas. Las operaciones internas/admin y opcionales mantienen límites expresos.

## Integración y límites del desglose

Las historias de EP tienen un único dueño primario en el inventario y mapa bilateral; los requisitos agregados previos de fases origen verifican integración con sus subcapacidades. No son una segunda implementación. Una dependencia de cierre de experiencia integrada no impide construir el subset anterior que sí tiene contratos/entradas. Las condiciones bloqueantes específicas se resuelven antes de Ready de cada historia.

## Trazabilidad del incremento

| Requisito local | Tareas definidas | Criterios vinculados |
|---|---|---|
| FR-001 | T-001, T-002, T-010, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-002 | T-001, T-002, T-011, T-003 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-001 | T-010, T-011, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| NFR-002 | T-001, T-007, T-005, T-612, T-613, T-408, T-413, T-415 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-408, AC-413, AC-415, AC-612, AC-613 |
| NFR-003 | T-002, T-010, T-011, T-021, T-003, T-005, T-612, T-613, T-408, T-413, T-415 | AC-001, AC-002, AC-003, AC-090, AC-100, AC-408, AC-413, AC-415, AC-612, AC-613 |
| FR-090 | T-006, T-007 | AC-090 |
| NFR-090 | T-006, T-007 | AC-090 |
| FR-100 | T-020, T-021 | AC-100 |
| NFR-100 | T-020, T-021, T-004 | AC-001, AC-002, AC-003, AC-090, AC-100 |
| FR-612 | T-612 | AC-612 |
| FR-613 | T-613 | AC-613 |
| FR-408 | T-408 | AC-408 |
| FR-413 | T-413 | AC-413 |
| FR-415 | T-415 | AC-415 |

Cobertura de diseño, no ejecución aprobada. Contratos formales, decisiones y evidencia real siguen pendientes.
