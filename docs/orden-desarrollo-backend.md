# Orden SDD del backend y tareas de estructura

Revision documental 0.4.1. La arquitectura de carpetas se mantiene en [estructura backend](estructura-backend.md); este documento define como convertirla en trabajo trazable. No crea servicios ni modifica el catalogo de 101 operaciones.

## Cadena obligatoria de una historia

Fuente/decision → spec con dueño y alcance → plan con archivos y pruebas → tarea de preparacion estructural → Ready humano → construccion autorizada → integracion/contrato → seguridad → evidencia/checklist/Memoria → PR develop → candidato release → main → reintegracion develop.

En las 26 specs actuales se agrega FR-950/NFR-950, AC-950 y T-950. Son IDs LOCALES: citar siempre nv-XX/T-950. T-950 depende del refinamiento T-006/T-020 y fija rutas actuales/objetivo, propietario, referencia de contrato, entrada necesaria y suites/evidencia; T-001 espera T-950. Nunca depende de construccion futura ni del cierre de la propia spec. Rutas propuestas no son carpetas existentes.

El alcance de preparacion varia por spec: gobierno/contratos/expediente entregan documentos y controles, no servicios ficticios; las funcionalidades usan su dominio existente al habilitarlo. S08 sigue condicionado. Esta preparacion no duplica las tareas funcionales anteriores ni añade una implementacion por boton o por spec.

## Trabajos con dueño unico

| Dueño | Tarea nueva | Entregable y criterio |
|---|---|---|
| [nv-00](../specs/nv-00/task.md) | T-951 / FR-951 / AC-951 | Activar Jira/Confluence con datos reales y check verificado; preparado no significa activo. Pendiente por decision del usuario. |
| [nv-01](../specs/nv-01/task.md) | T-951 / FR-951 / AC-951 | Consolidar plantilla de Visual Studio en el checkout backend y actualizar solucion/Docker/lanzamiento juntos, sin segundo repo. |
| [nv-01](../specs/nv-01/task.md) | T-952 / FR-952 / AC-952 | Verificar referencias por capas/servicio y rechazo de referencias o datos ajenos sobre proyectos reales. |
| [nv-03](../specs/nv-03/task.md) | T-951 / FR-951 / AC-951 | Incorporar CI de producto en el mismo PR del csproj; sustituir bloqueo documental por gates reales, no retirarlo sin reemplazo. |

Estas tareas ejecutables/de activacion esperan T-002 y su preparacion de rutas. En nv-01, T-952 espera T-951. La integracion T-003 de cada dueño espera esas tareas; no se altera el resto del flujo seguridad/cierre. Todas las casillas siguen pendientes y no se afirma ninguna aceptacion.

nv-02 conserva autoridad de formalizacion de contratos en sus tareas ya existentes; nv-04 implementa la primera historia sobre el scaffold de nv-01. No se vuelve a migrar la plantilla ni crear otra API de cuentas desde nv-16. nv-22/nv-14 operan los servicios habilitados; no recrean la fundacion.

## Evitar ciclos entre fundamentos

La dependencia entre specs significa disponer de sus entradas concretas para la historia, no terminar todas las capacidades futuras de la fase anterior. Dejar en plan un enlace y la entrada exacta obtenida del dueño:

- nv-00 aporta decisiones y responsable/ratificacion pertinentes. La activacion Jira/Confluence pospuesta bloquea esa publicacion, no el analisis documental local. No inventar enlaces para marcar cierre completo de gobierno.
- nv-01 aporta diseño de fronteras/rutas antes de nv-02. No exigir un proyecto compilado para escribir su contrato.
- nv-02 aporta contrato de la historia antes de implementarla; no exige tener todas las APIs del catalogo formalizadas.
- El primer PR ejecutable coordina scaffold de nv-01 y gates nv-03. El scaffold crea rutas necesarias para build; CI las valida en ESE cambio. No pedir CI verde sobre proyectos inexistentes antes de poder crearlos, ni aprobar csproj con solo lint despues.
- nv-04 recibe fundamentos/contrato/gates necesarios; el primer caso es un subset real y verificable, no los siete endpoints de golpe por marcar una fase.

Antes de crear una rama de implementacion, el plan debe identificar la decision resuelta, artefacto/contrato de entrada y version/revision, archivos actuales/objetivo, comandos aplicables y persona revisora. Si una decision sigue como propuesta, se registra pendiente y no se disfraza de regla ratificada.

## Contrato de preparacion de cada plan

| Dato obligatorio | Pregunta que debe resolver |
|---|---|
| Propietario/alcance | Que servicio o superficie es dueño y que queda fuera |
| Ruta actual | Que archivos existen hoy, en que checkout y con que evidencia |
| Ruta objetivo | Que archivos concretos se crearan/moveran/modificaran en la historia |
| Dependencia de entrada | Que contrato/decision/schema/resultado de otro dueño se requiere, sin esperar su fase completa |
| Referencias | Que proyecto puede referenciar a cual y que integracion es por contrato/evento |
| Verificacion | Que suite/caso positivo, negativo y fallo verifica cada criterio; doble o integracion real |
| Integracion Git/CI | Rama y PR correctos; gate de producto en el mismo cambio del manifiesto |
| Cierre | Evidencia, resultado real, limitacion, revisor y rollback; sin firmas automaticas |

Las rutas exactas de clase/csproj y comandos de producto se fijan al ejecutar el scaffold autorizado, no se presentan como creados por este mapa. La primera consolidacion debe usar un solo repositorio backend con SDD; no copiar .git ni excluir constitucion/specs para tener otra solucion desconectada.

## Validacion disponible

`scripts/check_sdd.ps1` conserva lint de estructura/enlaces. `python -B scripts/check_sdd_integrity.py` comprueba IDs, cobertura requisito/tarea/criterio, referencias, ciclos locales de tareas y que Ready espere la preparacion estructural cuando se declara FR-950. La CI documental backend invoca ambas comprobaciones; sigue rechazando manifiestos de producto hasta una transicion real.

No se verifica por estos scripts la semantica completa, los ciclos entre decisiones/contratos externos ni producto, disponibilidad de proveedores o revision humana. Las entradas entre specs se revisan con la secuencia anterior. Los contextos CI congelados siguen en su revision; la version documental 0.4.1 no afirma que el snapshot movil sea HEAD ni cambia su autoridad.

## Perfiles por propietario

El [catálogo de arquitectura](arquitectura/README.md) define estructura, patrones, puertos, consistencia y fallos de G01 y S01–S08. Aplicar sus políticas comunes al refinar cada historia; no crea proyectos, ratifica proveedores ni reemplaza contratos.
