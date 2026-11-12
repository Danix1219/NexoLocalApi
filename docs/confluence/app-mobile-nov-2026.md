# Nexo Local móvil — integración API, perfiles, publicaciones e imágenes

> Borrador listo para publicar como página de Confluence. Completar Jira, enlace real de la PR móvil y permalink de esta página antes de solicitar merge. No inventar claves ni evidencia.

## Trazabilidad

- Jira principal: **pendiente de asignar/confirmar**.
- Repositorio: [NexoLocalApp](https://github.com/Danix1219/NexoLocalApp).
- Rama: `feature/mobile-endpoints-2026-11` → destino `develop`.
- PR móvil: pendiente de confirmar URL.
- PR API contraparte: [#28 — feat (api): implementar cuenta y endpoints de descubrimiento](https://github.com/Danix1219/NexoLocalApi/pull/28).
- Specs principales: `specs/041-cuenta-modo-celular-baja/`, `specs/053-paridad-visual-maqueta/`, `specs/054-integracion-perfiles/`, `specs/055-revision-visual-pantallas/`, `specs/056-correo-ubicacion-registro/`, `specs/057-integracion-descubrimiento-trabajos-solicitudes/`, `specs/058-perfil-publicaciones-galeria/` y `specs/059-fotos-perfil-zona-navegacion/`.
- Documentación de integración: `docs/integracion-backend-01-06.md`, `docs/integracion-identity-cuenta.md`, `docs/integracion-perfiles.md`, `docs/ubicacion-real-y-registro.md` y `Memoria.md` del repositorio móvil.

## Problema y resultado esperado

El cliente tenía inconsistencias al consultar datos reales, mostrar fotografías/perfiles y completar publicaciones. Este trabajo conecta las pantallas y repositorios Flutter con los contratos implementados, mejora estados de carga/error, paginación y navegación a perfiles, y ajusta la presentación de cuenta, proveedor, solicitudes y publicaciones para mantener una experiencia coherente.

## Alcance

- Integración HTTP para cuenta, perfiles, descubrimiento, búsqueda, ubicación, trabajos y solicitudes cubiertos por las fichas vigentes.
- Edición y visualización de perfil, datos del autor, zona aproximada, imágenes de perfil/portada y feedback de carga/errores según permisos y aprobación que expone la API.
- Flujos de publicación y edición con validación y resultados del servidor; resiliencia/offline y pruebas de integración con dobles donde corresponde.
- Ajustes de navegación, tarjetas, galería, formularios y estilo visual, además de pruebas de privacidad/seguridad, CI y documentación SDD.
- No se debe mostrar una acción como exitosa si el endpoint correspondiente aún no está implementado o devuelve error. Guardados/intereses/contacto se habilitan únicamente conforme a contratos disponibles.

## Coordinación API/app

La API conserva la autoridad de contratos y reglas de propiedad, moderación y publicación. Flutter consume esos contratos y no debe inferir permisos ni estados de aprobación. La compatibilidad se debe revisar contra las fichas EP de API y los escenarios de `http_nexo_api.dart`; los fallos de la PR API deben resolverse antes de aceptar como completa la integración de producto.

## Privacidad y seguridad

El cliente evita incluir datos sensibles en registros, almacena sesión mediante el mecanismo seguro del proyecto y respeta los errores de autorización del servidor. Las imágenes deben proceder de URL autorizadas y estados aprobados; el cliente no puede otorgar aprobación ni exponer ubicación domiciliaria exacta. Las claves idempotentes de pruebas son datos sintéticos y no credenciales.

## Validación y límites de evidencia

- Validación Flutter registrada en la documentación del repositorio el 2026-11-10: 267 pruebas aprobadas; `flutter analyze --no-pub` con 0 errores, 0 advertencias y 13 notas informativas. Estos resultados son evidencia registrada, no una nueva ejecución en esta página.
- Validación de workflow local registrada como `WORKFLOWS-OK`.
- Se instaló y abrió un APK debug en Android; se revisó el inicio en logcat sin errores `AndroidRuntime` ni `E:flutter`. Esto no demuestra que todos los flujos funcionen en dispositivo.
- La validación SDD registrada encontró 59 specs y trazabilidad documental correcta. No constituye prueba del producto.
- Pendiente verificar el CI remoto actual de la rama móvil y repetir los recorridos físicos de publicación, fotos de perfil/portada y carga del feed.
- No se afirma validación contra cuentas externas, correo, moderación cloud, Azure ni despliegue productivo.

## Riesgos, mitigación y reversión

- Riesgo: la API aún presenta checks fallidos en la PR #28. Mitigación: coordinar merge y validar contratos antes de habilitar el cliente contra ese backend.
- Riesgo: disponibilidad de URL/media y permisos del entorno. Mitigación: registrar respuesta HTTP concreta y no reemplazar fallos por éxito aparente.
- Reversión: revertir el commit/PR móvil de integración devuelve el cliente al comportamiento anterior; coordinar con API si ya hubiera consumidores desplegados.
- No se documentan migraciones móviles en este cambio.

## Registro de cambios

| Fecha | Referencia | Cambio | Estado |
|---|---|---|---|
| 2026-11-11 | Rama móvil `feature/mobile-endpoints-2026-11` | Integración de pantallas y repositorios con API, ajustes de medios/UX, cobertura automatizada y SDD | Rama existente; PR móvil y CI remoto por confirmar; sin despliegue confirmado |

## Pendientes antes del merge

1. Asignar/confirmar clave Jira real y alinearla con título, rama y Jira principal.
2. Publicar esta página en Confluence y poner su URL permanente real en la PR móvil.
3. Confirmar URL de la PR móvil y actualizar la referencia cruzada en la página de API.
4. Repetir CI remoto y pruebas en dispositivo para los recorridos principales.
5. Identificar revisor humano independiente y completar revisión de privacidad/compatibilidad.

