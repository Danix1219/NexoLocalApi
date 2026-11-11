# API Nexo Local — cuenta, perfiles, descubrimiento y solicitudes

> Borrador listo para publicar como página de Confluence. Completar Jira y pegar el permalink real de esta página en la PR antes de solicitar merge. No inventar claves ni evidencia.

## Trazabilidad

- Jira principal: **pendiente de asignar/confirmar**.
- PR API: [#28 — feat (api): implementar cuenta y endpoints de descubrimiento](https://github.com/Danix1219/NexoLocalApi/pull/28).
- Rama: `feature/api-endpoints-2026-11` → destino `develop`.
- Specs principales: `specs/endpoints/02-cuenta/`, `03-perfil-prestador/`, `04-descubrimiento/`, `05-trabajos/`, `06-solicitudes/`; arquitectura `specs/arquitectura/nv-04`, `nv-05`, `nv-06`, `nv-08`, `nv-09` y tareas relacionadas.
- Implementación/estado: [`docs/endpoints.md`](../endpoints.md), [`docs/estado-desarrollo-endpoints.md`](../estado-desarrollo-endpoints.md), [`docs/decisions/registro.md`](../decisions/registro.md) y [`Memoria.md`](../../Memoria.md).
- PR móvil contraparte: pendiente de confirmar URL.

## Problema y resultado esperado

Los flujos de cuenta, perfiles, descubrimiento, trabajos y solicitudes necesitaban contratos implementados y persistencia real para que el cliente pudiera dejar de depender de respuestas simuladas. El resultado de este incremento es una API modular con operaciones documentadas, reglas de propiedad y estado, consultas paginadas y proyecciones públicas controladas por privacidad. La existencia de una operación en el inventario no significa que todo el catálogo histórico esté implementado; consultar el índice de estado para el alcance exacto.

## Alcance y decisiones

- Cuenta e identidad: operaciones propias de cuenta, modo, dispositivos y solicitud/cancelación de baja según la ficha vigente. Se conserva la sesión restringida durante recuperación; tokens de épocas anteriores se rechazan.
- Perfil de proveedor y medios: consultas y edición protegidas por propiedad y estado de aprobación. Cargas, moderación y almacenamiento externos tienen dependencias explícitas; no se afirma aceptación de Azure/Key Vault ni de servicios externos por pruebas locales.
- Descubrimiento/feed: `GET /v1/discovery/feed` combina trabajos aprobados y perfiles publicados con ubicación aproximada; cursores vinculados a filtros/ámbito, marcas personalizadas por sesión y proyección sin domicilio exacto ni datos privados.
- Trabajos y solicitudes: persistencia, filtros por cobertura/cercanía, moderación, cuotas, propiedad, paginación y cierre conforme a las fichas de grupo 05/06. Las rutas de interés, guardados y contacto se consideran disponibles solo cuando el índice de implementación lo confirme.
- Los contratos y las fronteras entre dominios permanecen en las fichas versionadas del repositorio. Identity y Engagement exponen contratos, no acceso directo de un dominio a las bases internas de otro.

## Seguridad y privacidad

La ubicación exacta de solicitudes se mantiene en el almacenamiento privado necesario para la búsqueda geoespacial; las respuestas públicas usan ubicación difuminada. Las proyecciones excluyen domicilios, credenciales y datos de contacto privados. Las operaciones mutables validan sesión, propietario, estado y límites. Los errores de dependencias externas deben expresarse como fallos/degradación documentados, nunca como éxito fabricado.

## Validación y límites de evidencia

- La PR #28 mostró dos checks fallidos: **Compilación y estilo** y **Puerta de calidad**. El log identifica `WHITESPACE` en `src/Domains/Discovery/Nexo.Discovery.Application/Directory/DirectoryQuery.cs`; la puerta falla como consecuencia de la etapa requerida. Se corrigió el inicializador del hash y se verificó localmente con `dotnet format src/Nexo.sln whitespace --include src/Domains/Discovery/Nexo.Discovery.Application/Directory/DirectoryQuery.cs --no-restore --verify-no-changes` (exit 0) y `dotnet build src/Nexo.sln --no-restore --configuration Release` (0 advertencias, 0 errores). Falta repetir CI remoto para confirmar los checks de la PR.
- La salida compartida previamente reportó un fallo del test `Nexo.Integration.WorkPersistenceTests.Feed_combina_trabajos_por_cobertura_y_cercania_pagina_y_marca_estado_personal` en la línea 146. Tras el arreglo se ejecutó esa prueba individual localmente con acceso a Docker/Testcontainers: 1/1 aprobada. No se ha repetido la suite completa ni confirmado todavía la ejecución remota de integración.
- No se afirma despliegue ni prueba contra Atlas, Azure, Key Vault o servicios de correo/moderación salvo que se añada evidencia específica con entorno y resultado.
- La validación SDD comprueba estructura, trazabilidad y enlaces; no es aceptación funcional.

## Riesgos, mitigación y reversión

- Riesgo: diferencias entre contratos API y cliente Flutter. Mitigación: revisar conjuntamente la PR móvil y las fichas EP. Revertir el commit funcional de API o cerrar la PR restaura el estado previo.
- Riesgo: checks de CI aún no repetidos tras la corrección de formato. Mitigación: exigir nueva ejecución de workflow antes del merge.
- Migraciones: no se documenta una migración de datos en este registro. Verificar los cambios de almacenamiento/índices en el diff y specs antes de aprobación.

## Registro de cambios

| Fecha | Referencia | Cambio | Estado |
|---|---|---|---|
| 2026-11-11 | PR API #28 | Implementación y contratos de cuenta/descubrimiento y áreas relacionadas; corrección del formato reportado por CI y verificación dirigida del test de feed | Formato/build locales aprobados y prueba dirigida 1/1; CI remoto por volver a ejecutar; sin merge ni despliegue confirmado |

## Pendientes antes del merge

1. Asignar/confirmar la clave Jira real y hacer coincidir título, rama y Jira principal según la política.
2. Publicar esta página en Confluence y reemplazar el campo Confluence de la PR por su URL permanente real.
3. Repetir CI; investigar y resolver cualquier fallo funcional de integración que persista.
4. Añadir URL de la PR móvil contraparte y revisión humana independiente.

