# Arranque de nv-00: constitucion y gobierno

Preparado el 2026-11-03. Spec objetivo: [nv-00](../../specs/arquitectura/nv-00/spec.md); [plan](../../specs/arquitectura/nv-00/plan.md), [tareas](../../specs/arquitectura/nv-00/task.md), [checklist](../../specs/arquitectura/nv-00/checklists/gates.md). La primera spec backend es de gobierno; la primera funcional de identidad es nv-04, despues de entradas de nv-01..03. Este paquete no implementa un endpoint ni mueve la plantilla.

Estado: **preparacion local disponible; Ready formal y cierre pendientes de revision/ratificacion humana**. No se crean estados alternativos para fingir Ready. Las tareas permanecen sin marcar; artefacto preparado no significa criterio aceptado.

## Incremento seleccionado: gobierno local de la primera vertical

Entrada: constitucion/fuentes/arquitectura y specs vigentes; acuerdos reales de repositorios/herramientas; plantilla Visual Studio inspeccionada previamente. Salida preparada: registro de decisiones/bloqueos, flujo de cambio, rutas y pruebas de nv-00, matriz de evidencia y traspaso a fundacion/contratos/calidad. Solo se usa la informacion disponible.

Fuera de esta seleccion: instalar paquetes, crear Domain/Application/Infrastructure/Api, mover csproj, implementar autenticacion, elegir nube/SMS/IA, publicar archivos o dar entrevistas/firmas por realizadas. La activacion Jira/Confluence y el cierre academico completo siguen en la spec, sin convertirse en prerrequisitos ficticios de la documentacion local.

## Donde trabajar y en que orden

1. Leer constitucion y las cuatro piezas de nv-00; contrastar [decisiones abiertas](../decisions/registro.md) y [flujo de cambios](flujo-cambios.md). Resolver discrepancias con fuente/acuerdo, no inventando reglas.
2. Refinar tareas de preparacion T-006/T-020 y T-950 con los artefactos de este paquete. Registrar el alcance elegido, entradas, limites y salida para la siguiente historia. Los documentos aqui son borradores revisables.
3. Revisar la [matriz de evidencia](evidencia-nv00.md): cada tarea tiene una referencia y un limite. Preparar las evidencias academicas faltantes con su guion; no sustituir entrevista real o revision docente por un archivo vacio.
4. Obtener la revision real de Danix1219 (revisor provisional designado por el usuario, sin independencia respecto a sus propios cambios) y resolver bloqueantes DE ESTA seleccion antes de T-001 Ready. No exigir un contrato HTTP en nv-00: no hay operacion nueva ni DTO propio. El N/A esta limitado a esta spec; nv-02/nv-04 si formalizan sus contratos.
5. Despues de Ready, consolidar las decisiones/documentos de la seleccion y ejecutar sus comprobaciones. T-003/T-004/T-007/T-005 conservan integracion, revision de seguridad, evidencia y cierre; el cierre GLOBAL de nv-00 espera tambien los criterios no cubiertos por la seleccion.
6. Entregar a nv-01 el mapa de dueños/rutas/decisiones y a nv-02/nv-03 las entradas de contrato/CI. No exigir que todo D01-D08 o toda nv-00 esten cerrados para preparar documentos posteriores; la implementacion espera sus bloqueantes concretos.

## Comandos disponibles desde la raiz API_NexoLocal

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check_sdd.ps1
python -B scripts/check_sdd_integrity.py
python -B -m unittest discover -s scripts -p test_sdd_integrity.py
```

Son validaciones documentales y del propio verificador. No ejecutan build, SMS, DB, dispositivos ni deploy. El wrapper CI se ensaya en copia aislada: NO ejecutarlo sobre las carpetas vivas porque extrae contextos congelados. Sus hashes fijan referencias, no las convierten en HEAD de la contraparte.

## Puertas y salida

| Puerta | Puede continuar ahora | Pendiente antes de declarar salida |
|---|---|---|
| Preparacion local nv-00 | Si: lectura/refinamiento/documentacion y validadores | Sin firmas ni tareas aprobadas automaticas |
| Ready formal de la seleccion | Paquete preparado para revision | Revision registrada de Danix1219, designado provisionalmente por el usuario, y decisiones bloqueantes de la seleccion resueltas; sin revision independiente atribuida |
| Cierre completo nv-00 | No se declara completado | Criterios de gobierno, evidencias academicas pertinentes y activacion real de controles pendientes |
| Primer scaffold nv-01/nv-03 | Solo su diseño documental | Ratificacion tecnica, historia Ready, rutas/contrato de entrada y encargo ejecutable; CI real en el mismo PR del csproj |
| Identidad nv-04 | Refinamiento puede prepararse | Contrato formal, politicas/proveedor y datos de privacidad pertinentes, CI/pruebas y revisor |
| Publicacion de cambios actuales | No | Datos Jira/Confluence, documentacion real y flujo de PR acordado; el usuario los pospuso |

El titular GitHub y revisor provisional designado por el usuario es Danix1219. La revision aun no se ha efectuado; no se infiere que sea el arquitecto/ratificador de todas las decisiones o un segundo revisor y no se firma en su nombre. Para trabajo de equipo, incorporar revisor real y elevar aprobaciones conforme al acuerdo de ramas.

## Condiciones de cierre del paquete de preparacion

Documentos enlazados presentes, requisitos/tareas/criterios coherentes, fuentes inventariadas sin atribuir revision no ejecutada, validadores correctos y pendientes con puerta explicita. Esto acredita preparacion, no ratificacion de constitucion ni implementacion. El resultado real de esta fase se registra en evidencia y Memoria.

## Encargo posterior: base fisica creada

El usuario autorizo crear carpetas/proyectos sin logica de negocio. [Estado vigente y evidencia corregida](correccion-base-microservicios.md): solucion original con G01 Gateway y S01 Identity independiente, en la estructura de servicios. Este resultado es posterior al paquete documental de gobierno; no firma Ready, ratifica proveedores o completa consolidacion al checkout Git.

## Ampliación de la base por encargo posterior

Consultar [base de todos los microservicios y evidencia ejecutada](base-todos-microservicios.md). La solución original contiene ahora 33 proyectos: G01, S01–S08 y Worker S07; sin negocio. Este informe actualiza el alcance previo de G01/S01; S08 sigue condicional.

## Arquitectura interna revisada

[Decisión por caso de uso](../arquitectura/seleccion-arquitecturas.md) y [evidencia vigente](revision-arquitecturas.md): 28 proyectos, diez hosts; sustituye el scaffold uniforme previo.
