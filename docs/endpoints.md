# Endpoints — estado real y línea base de diseño

## Estado revisado el 2026-11-09

Fuente de implementación: `IdentityEndpointMapping`, `AuthEndpoints`, `AccountEndpoints` y `TokenEndpoint` en
`src/Domains/Identity/Nexo.Identity.Endpoints/`. Las fichas describen contratos; existir en una
carpeta no acredita implementación. La revisión de código no prueba el despliegue remoto.

| Método | Ruta registrada | Ficha vigente |
|---|---|---|
| POST | `/v1/identity/email/verification` | [Envío](../specs/endpoints/01-auth/post-email-verification.md) |
| POST | `/v1/identity/email/verification/confirm` | [Confirmación](../specs/endpoints/01-auth/post-email-verification-confirm.md) |
| POST | `/v1/identity/accounts` | [Registro](../specs/endpoints/01-auth/post-accounts.md) |
| POST | `/connect/token` (`password` y `refresh_token`) | [Login](../specs/endpoints/01-auth/post-connect-token-password.md), [refresh](../specs/endpoints/01-auth/post-connect-token-refresh.md) |
| POST | `/v1/identity/logout` | [Logout](../specs/endpoints/01-auth/post-logout.md) |
| POST | `/v1/identity/password/forgot` | [Recuperación](../specs/endpoints/01-auth/post-password-forgot.md) |
| POST | `/v1/identity/password/reset` | [Restablecimiento](../specs/endpoints/01-auth/post-password-reset.md) |
| GET | `/v1/identity/me` | [Cuenta propia](../specs/endpoints/02-cuenta/get-me.md) |
| PATCH | `/v1/identity/me` | [Edición](../specs/endpoints/02-cuenta/patch-me.md) |
| POST | `/v1/identity/me/mode` | [Modo](../specs/endpoints/02-cuenta/post-me-mode.md) |
| POST | `/v1/identity/me/phone` | [Contacto móvil declarado](../specs/endpoints/02-cuenta/post-me-phone.md) |
| POST | `/v1/identity/devices` | [Registrar dispositivo](../specs/endpoints/02-cuenta/post-devices.md) |
| DELETE | `/v1/identity/devices/{id}` | [Quitar dispositivo](../specs/endpoints/02-cuenta/delete-devices-id.md) |
| POST | `/v1/identity/me/deletion` | [Solicitar baja local](../specs/endpoints/02-cuenta/post-me-deletion.md) |
| DELETE | `/v1/identity/me/deletion` | [Cancelar baja local](../specs/endpoints/02-cuenta/delete-me-deletion.md) |

Son quince combinaciones método/ruta y dieciséis operaciones al contar por separado los grants OAuth.
`GET /health/live` y `GET /health/ready` son sondas técnicas. OpenAPI, Scalar y la redirección `/`
se controlan con `Docs:Enabled` según el `Program.cs` actual, también en Production si está habilitado.

Las ocho operaciones vigentes de `02-cuenta` ya tienen implementación local; ver
[alcance y dependencias](../specs/endpoints/02-cuenta/implementacion.md). Discovery registra perfiles, comparación, ubicación/geocodificación y directorio público; Engagement y Screens conservan sus límites actuales.
La antigua confirmación de teléfono se retiró del diseño: la verificación es por correo SMTP
y el móvil sigue siendo contacto declarado. Ver [decisión vigente](decisions/registro.md).

Límites encontrados: el flujo `changeEmail` emite un ticket, pero no hay operación que aplique
el cambio; `password.changed` no tiene consumidor de aviso de correo/push; los demás eventos
no prueban propagación a los otros dominios. Los avisos de baja/cancelación y auditoría local sí
tienen consumidor. El outbox conserva códigos de correo en su payload,
sin purga automática implementada. La suite local no acredita entrega real por Gmail ni Content Safety.
La edición de nombre devuelve 503 si falta la configuración de Content Safety; las fotos requieren
un registro propio de avatar aprobado por nv-05. La baja global/anónima y sus consumidores son nv-12.

## Incremento Discovery: perfiles (2026-11-09)

Además de `PUT /v1/discovery/me/location`, se registran las siguientes operaciones. Fuente: `ProviderEndpoints`, `ProviderService` y adaptadores de Discovery. [Evidencia y límites](../specs/endpoints/03-perfil-prestador/implementacion.md).

| Método | Ruta | Autorización |
|---|---|---|
| POST | `/v1/discovery/providers` | Cuenta activa y correo confirmado; Idempotency-Key UUID |
| GET | `/v1/discovery/providers/me` | Dueño por sub |
| GET | `/v1/discovery/providers/{id}` | Público; cuenta activa y perfil publicado |
| PATCH | `/v1/discovery/providers/me/{section}` | Dueño; basic/services/zone/hours/prices/terms/portfolio |
| GET | `/v1/discovery/providers/compare` | Autenticado; exactamente dos UUID distintos |

Publicación textual requiere Content Safety configurado; no hay aprobación automática si falta. Subidas: aprobadas, propias y de la finalidad correcta. No se crean Blob, CDN ni cuentas cloud. Las fichas de caché describen objetivos posteriores: este incremento usa no-store y ETag por respuesta pública. [Orden restante](estado-desarrollo-endpoints.md).

## Línea base histórica de 101 EP

Las rutas siguientes pertenecen al diseño anterior y no son una guía de consumo ni aliases
registrados. Las rutas ejecutables se enumeran arriba; los demás contratos requieren conciliación
por incremento antes de implementarlos. No generar clientes con `/auth/*` a partir de esta tabla.

Fuente histórica: [inventario](contracts/inventario.json), revisión documental 0.4.0. [Fichas históricas](contracts/fichas-operaciones.md) y [cobertura bilateral](../../docs_nexo/SDD_COBERTURA_FUNCIONAL.md). Sus rutas y prefijos se conservan para trazabilidad, no como contratos ejecutables; la API actual usa `/v1/<dominio>` y `/connect/token`.

| Método | Ruta | EP | Propietario | Spec primaria |
|---|---|---|---|---|
| POST | `/auth/otp` | EP-001 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| POST | `/auth/otp/verify` | EP-002 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| POST | `/auth/register` | EP-003 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| POST | `/auth/login` | EP-004 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| POST | `/auth/refresh` | EP-005 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| POST | `/auth/password-reset` | EP-006 | S01 | [nv-16](../specs/arquitectura/nv-04/spec.md) |
| POST | `/auth/logout` | EP-007 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| GET | `/me` | EP-008 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| PATCH | `/me` | EP-009 | S01 | [nv-16](../specs/arquitectura/nv-04/spec.md) |
| GET | `/me/sessions` | EP-010 | S01 | [nv-16](../specs/arquitectura/nv-04/spec.md) |
| DELETE | `/me/sessions/{sid}` | EP-011 | S01 | [nv-16](../specs/arquitectura/nv-04/spec.md) |
| POST | `/me/deletion` | EP-012 | S01 | [nv-12](../specs/arquitectura/nv-12/spec.md) |
| GET | `/me/deletion/{id}` | EP-013 | S01 | [nv-12](../specs/arquitectura/nv-12/spec.md) |
| GET | `/catalog/categories` | EP-014 | S02 | [nv-17](../specs/arquitectura/nv-06/spec.md) |
| PUT | `/admin/catalog/categories/{id}` | EP-015 | S02 | [nv-17](../specs/arquitectura/nv-06/spec.md) |
| POST | `/providers` | EP-016 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| GET | `/providers/{id}` | EP-017 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| GET | `/me/provider` | EP-018 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| PATCH | `/me/provider` | EP-019 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| PATCH | `/me/provider/coverage` | EP-020 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| PATCH | `/me/provider/schedule` | EP-021 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| PATCH | `/me/provider/prices` | EP-022 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| POST | `/me/provider/refresh` | EP-023 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| DELETE | `/me/provider` | EP-024 | S02 | [nv-06](../specs/arquitectura/nv-06/spec.md) |
| GET | `/providers` | EP-025 | S02 | [nv-07](../specs/arquitectura/nv-07/spec.md) |
| GET | `/search/suggestions` | EP-026 | S02 | [nv-07](../specs/arquitectura/nv-07/spec.md) |
| GET | `/providers/compare` | EP-027 | S02 | [nv-07](../specs/arquitectura/nv-07/spec.md) |
| GET | `/geo/search` | EP-028 | S02 | [nv-07](../specs/arquitectura/nv-07/spec.md) |
| GET | `/geo/reverse` | EP-029 | S02 | [nv-07](../specs/arquitectura/nv-07/spec.md) |
| GET | `/feed` | EP-030 | S03 | [nv-07](../specs/arquitectura/nv-07/spec.md) |
| POST | `/posts` | EP-031 | S03 | [nv-08](../specs/arquitectura/nv-08/spec.md) |
| GET | `/posts` | EP-032 | S03 | [nv-08](../specs/arquitectura/nv-08/spec.md) |
| GET | `/posts/{id}` | EP-033 | S03 | [nv-08](../specs/arquitectura/nv-08/spec.md) |
| PATCH | `/posts/{id}` | EP-034 | S03 | [nv-08](../specs/arquitectura/nv-08/spec.md) |
| DELETE | `/posts/{id}` | EP-035 | S03 | [nv-08](../specs/arquitectura/nv-08/spec.md) |
| PUT | `/reactions/{type}/{id}` | EP-036 | S03 | [nv-18](../specs/arquitectura/nv-08/spec.md) |
| DELETE | `/reactions/{type}/{id}` | EP-037 | S03 | [nv-18](../specs/arquitectura/nv-08/spec.md) |
| GET | `/me/saved` | EP-038 | S03 | [nv-18](../specs/arquitectura/nv-08/spec.md) |
| PUT | `/me/saved/{type}/{id}` | EP-039 | S03 | [nv-18](../specs/arquitectura/nv-08/spec.md) |
| DELETE | `/me/saved/{type}/{id}` | EP-040 | S03 | [nv-18](../specs/arquitectura/nv-08/spec.md) |
| POST | `/reviews` | EP-041 | S03 | [nv-19](../specs/arquitectura/nv-08/spec.md) |
| GET | `/providers/{id}/reviews` | EP-042 | S03 | [nv-19](../specs/arquitectura/nv-08/spec.md) |
| PATCH | `/reviews/{id}` | EP-043 | S03 | [nv-19](../specs/arquitectura/nv-08/spec.md) |
| DELETE | `/reviews/{id}` | EP-044 | S03 | [nv-19](../specs/arquitectura/nv-08/spec.md) |
| POST | `/interactions` | EP-045 | S03 | [nv-18](../specs/arquitectura/nv-08/spec.md) |
| GET | `/me/provider/metrics` | EP-046 | S03 | [nv-18](../specs/arquitectura/nv-08/spec.md) |
| POST | `/requests` | EP-047 | S04 | [nv-09](../specs/arquitectura/nv-09/spec.md) |
| GET | `/requests` | EP-048 | S04 | [nv-09](../specs/arquitectura/nv-09/spec.md) |
| GET | `/requests/{id}` | EP-049 | S04 | [nv-09](../specs/arquitectura/nv-09/spec.md) |
| PATCH | `/requests/{id}` | EP-050 | S04 | [nv-09](../specs/arquitectura/nv-09/spec.md) |
| POST | `/requests/{id}/close` | EP-051 | S04 | [nv-09](../specs/arquitectura/nv-09/spec.md) |
| DELETE | `/requests/{id}` | EP-052 | S04 | [nv-09](../specs/arquitectura/nv-09/spec.md) |
| PUT | `/requests/{id}/interest` | EP-053 | S04 | [nv-20](../specs/arquitectura/nv-09/spec.md) |
| DELETE | `/requests/{id}/interest` | EP-054 | S04 | [nv-20](../specs/arquitectura/nv-09/spec.md) |
| GET | `/requests/{id}/interests` | EP-055 | S04 | [nv-20](../specs/arquitectura/nv-09/spec.md) |
| PUT | `/requests/{id}/shortlist/{providerId}` | EP-056 | S04 | [nv-20](../specs/arquitectura/nv-09/spec.md) |
| DELETE | `/requests/{id}/shortlist/{providerId}` | EP-057 | S04 | [nv-20](../specs/arquitectura/nv-09/spec.md) |
| POST | `/orders` | EP-058 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| GET | `/me/orders` | EP-059 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| GET | `/orders/{id}` | EP-060 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| POST | `/orders/{id}/accept` | EP-061 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| POST | `/orders/{id}/reject` | EP-062 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| POST | `/orders/{id}/cancel` | EP-063 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| POST | `/orders/{id}/complete` | EP-064 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| PATCH | `/orders/{id}/address` | EP-065 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| POST | `/conversations` | EP-066 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| GET | `/me/conversations` | EP-067 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| GET | `/conversations/{id}/messages` | EP-068 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| POST | `/conversations/{id}/messages` | EP-069 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| PUT | `/conversations/{id}/read` | EP-070 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| PUT | `/conversations/{id}/block` | EP-071 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| DELETE | `/conversations/{id}/block` | EP-072 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| POST | `/realtime/ticket` | EP-073 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |
| WS | `/ws/v1/chat` | EP-074 | S05 | [nv-11](../specs/arquitectura/nv-11/spec.md) |
| POST | `/media` | EP-075 | S06 | [nv-05](../specs/arquitectura/nv-05/spec.md) |
| GET | `/media/{id}/access` | EP-076 | S06 | [nv-05](../specs/arquitectura/nv-05/spec.md) |
| DELETE | `/media/{id}` | EP-077 | S06 | [nv-05](../specs/arquitectura/nv-05/spec.md) |
| POST | `/reports` | EP-078 | S06 | [nv-05](../specs/arquitectura/nv-05/spec.md) |
| GET | `/me/reports` | EP-079 | S06 | [nv-05](../specs/arquitectura/nv-05/spec.md) |
| GET | `/admin/reports` | EP-080 | S06 | [nv-24](../specs/arquitectura/nv-05/spec.md) |
| POST | `/admin/reports/{id}/decision` | EP-081 | S06 | [nv-24](../specs/arquitectura/nv-05/spec.md) |
| POST | `/assist/draft` | EP-082 | S06 | [nv-13](../specs/arquitectura/nv-13/spec.md) |
| PUT | `/me/devices/{id}` | EP-083 | S07 | [nv-21](../specs/arquitectura/nv-12/spec.md) |
| DELETE | `/me/devices/{id}` | EP-084 | S07 | [nv-21](../specs/arquitectura/nv-12/spec.md) |
| GET | `/me/notifications` | EP-085 | S07 | [nv-21](../specs/arquitectura/nv-12/spec.md) |
| PUT | `/me/notifications/{id}/read` | EP-086 | S07 | [nv-21](../specs/arquitectura/nv-12/spec.md) |
| POST | `/internal/moderation/check` | EP-087 | S06 | [nv-05](../specs/arquitectura/nv-05/spec.md) |
| GET | `/internal/orders/{id}/review-eligibility` | EP-088 | S04 | [nv-10](../specs/arquitectura/nv-10/spec.md) |
| GET | `/health/live` | EP-089 | Transversal | [nv-01](../specs/arquitectura/nv-00/spec.md) |
| GET | `/health/ready` | EP-090 | Transversal | [nv-01](../specs/arquitectura/nv-00/spec.md) |
| GET | `/providers/{id}/contact` | EP-091 | S02 | [nv-25](../specs/arquitectura/nv-10/spec.md) |
| GET | `/orders/{id}/contact` | EP-092 | S04 | [nv-25](../specs/arquitectura/nv-10/spec.md) |
| GET | `/mentions/suggestions` | EP-093 | S02 | [nv-19](../specs/arquitectura/nv-08/spec.md) |
| GET | `/me/reviews` | EP-094 | S03 | [nv-19](../specs/arquitectura/nv-08/spec.md) |
| GET | `/reviews/{id}` | EP-095 | S03 | [nv-19](../specs/arquitectura/nv-08/spec.md) |
| PATCH | `/me/provider/contact-preferences` | EP-096 | S02 | [nv-25](../specs/arquitectura/nv-10/spec.md) |
| PATCH | `/orders/{id}/contact-preference` | EP-097 | S04 | [nv-25](../specs/arquitectura/nv-10/spec.md) |
| POST | `/graphql/social/v1` | EP-098 | S08 | [nv-15](../specs/arquitectura/README.md) |
| GET | `/catalog/report-reasons` | EP-099 | S06 | [nv-05](../specs/arquitectura/nv-05/spec.md) |
| GET | `/internal/providers/{id}/contact-policy` | EP-100 | S02 | [nv-25](../specs/arquitectura/nv-10/spec.md) |
| POST | `/internal/accounts/visibility-batch` | EP-101 | S01 | [nv-04](../specs/arquitectura/nv-04/spec.md) |

## Directorio público — incremento 2026-11-10
| Método | Ruta | Ficha |
|---|---|---|
| GET | `/v1/discovery/categories` | [Catálogo](../specs/endpoints/04-descubrimiento/get-categories.md) |
| GET | `/v1/discovery/search/suggest` | [Sugerencias](../specs/endpoints/04-descubrimiento/get-search-suggest.md) |
| GET | `/v1/discovery/search/providers` | [Búsqueda](../specs/endpoints/04-descubrimiento/get-search-providers.md) |
| GET | `/v1/discovery/map/nearby` | [Mapa](../specs/endpoints/04-descubrimiento/get-map-nearby.md) |

[Decisiones/evidencia](../specs/endpoints/04-descubrimiento/implementacion-directorio.md): solo mapa providers; requests devuelve 401/403 según permisos y 503 hasta habilitar solicitudes. Sin Atlas Search ni caché L2 de resultados activa.

## Trabajos — incremento 2026-11-10
| Método | Ruta | Ficha |
|---|---|---|
| POST | `/v1/discovery/works` | [Publicar](../specs/endpoints/05-trabajos/post-works.md) |
| GET | `/v1/discovery/providers/{id}/works` | [Listar](../specs/endpoints/05-trabajos/get-providers-id-works.md) |
| DELETE | `/v1/discovery/works/{id}` | [Borrar propio](../specs/endpoints/05-trabajos/delete-works-id.md) |
| POST | `/v1/discovery/works/{id}/recommend` | [Recomendar](../specs/endpoints/05-trabajos/post-works-id-recommend.md) |
| DELETE | `/v1/discovery/works/{id}/recommend` | [Retirar recomendación](../specs/endpoints/05-trabajos/delete-works-id-recommend.md) |

[Alcance/evidencia](../specs/endpoints/05-trabajos/implementacion.md). Requiere Mongo replica set para escrituras transaccionales. Moderación real obligatoria; imágenes aún bloqueadas. Eventos durables no acreditan despacho/push/feed.

## Incremento local 06-solicitudes
Base `/v1/discovery`, JWT obligatorio:
| Método | Ruta | Función |
|---|---|---|
| POST | /requests | Publicación moderada con Idempotency-Key |
| GET | /requests/mine | Solicitudes propias y resumen real de intereses |
| GET | /requests/opportunities | Oportunidades filtradas por perfil prestador |
| POST | /requests/{id}/close | Cierre exclusivo del propietario |

GET /map/nearby?kind=requests ahora consulta solicitudes reales. [Decisiones y evidencia](../specs/endpoints/06-solicitudes/implementacion.md).


## Incremento 14 ? im?genes reales (2026-11-10)
Reemplaza la limitaci?n hist?rica de im?genes de los incrementos 03/05/06. [Contrato y evidencia](../specs/endpoints/14-imagenes-cdn/implementacion.md).

| M?todo | Ruta bajo /v1/discovery | Acceso |
|---|---|---|
| POST | /uploads | JWT, intenci?n y SAS de creaci?n |
| POST | /uploads/{id}/complete | JWT, propietario |
| GET | /uploads/{id} | JWT, estado propio |
| DELETE | /uploads/{id} | JWT, retirada propia |
| GET | /media/{id} | Aprobado y cuenta activa |
| GET | /media/{id}/thumb | Aprobado y cuenta activa |
| GET | /me/cover | JWT, portada personal |
| PUT | /me/cover | JWT, cover aprobado propio |

Avatar, portada profesional, trabajos y solicitudes se adjuntan usando los PATCH/POST existentes. La cola procesa bytes reales, modera y genera WebP. No se utiliza aprobaci?n simulada ni lectura an?nima de los contenedores.
