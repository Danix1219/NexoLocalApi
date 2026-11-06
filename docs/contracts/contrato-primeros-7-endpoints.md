# Contrato de consumo — primeros 7 endpoints

Para el equipo movil. Describe lo que esta implementado y comprobado hoy, no el catalogo completo.

Version: 0.5.0 | Fecha: 2026-11-05 | Especificaciones: [specs/endpoints/](../../specs/endpoints/) | Catalogo autoritativo: [inventario.json](inventario.json)

## 1. Antes de empezar

> **Actualizado el 2026-11-06 (nv-00).** El backend paso de ocho microservicios a **una sola API
> modular**. Cambian **todas las rutas y el host**. La version anterior de este documento, con
> `/api/v1/...` y dos puertos, ya no corresponde a nada que exista.

| Dato | Valor |
|---|---|
| Identificador de cuenta | **correo electronico** (no telefono) |
| Host unico (local) | `http://127.0.0.1:5188` |
| Prefijo por dominio | `/v1/identity/*`, `/v1/discovery/*`, `/v1/engagement/*`, `/v1/screens/*` |
| Fuera del prefijo | `/health/live`, `/health/ready` |
| Documentacion viva | `/scalar/v1` (UI) y `/openapi/v1.json` (contrato), solo fuera de produccion |
| Content-Type de peticion | `application/json` |
| Autenticacion | `Authorization: Bearer <accessToken>` |
| Tipo del token de acceso | **opaco**, no JWT: no intenten decodificarlo ni leer claims |
| Forma de los errores | `application/problem+json` (RFC 9457) |
| Limite por IP | 60 peticiones por minuto sobre `/auth/*` y `/me`; al excederlo, `429` |

Los nombres de campo van en `camelCase` tal cual aparecen abajo. Todas las fechas son ISO-8601 en UTC con `Z`.

## 2. Tiempos de vida

| Elemento | Duracion | Consecuencia para la app |
|---|---|---|
| `accessToken` | 15 minutos | renovar con `/auth/refresh` antes de que venza |
| `refreshToken` | 30 dias | rota en **cada** refresh: guardar siempre el nuevo y descartar el anterior |
| Codigo de verificacion | 10 minutos, 5 intentos | tras el quinto intento el desafio muere; pedir otro |
| `proofToken` | 10 minutos, **un solo uso** | sirve para un unico registro o una unica recuperacion |
| Reenvio de codigo | 60 segundos | antes de ese plazo se devuelve el **mismo** `challengeId` |

## 3. Tabla de endpoints

| # | Spec / EP | Metodo y ruta | Host | Auth | Cuerpo de peticion | Respuesta correcta | Errores |
|---|---|---|---|---|---|---|---|
| 1 | ep-01 / EP-030 | `GET /v1/discovery/feed` | Discovery | no | — (parametros de consulta) | `200` `{ items[], nextCursor, partial }` | `429` |
| 2 | ep-03 / EP-001 | `POST /v1/identity/auth/otp` | Identity | no | `{ email, purpose }` | `202` `{ challengeId, retryAfter }` | `400`, `429` |
| 3 | ep-03 / EP-002 | `POST /v1/identity/auth/otp/verify` | Identity | no | `{ challengeId, code }` | `200` `{ proofToken, expiresAt }` | `400`, `429` |
| 4 | ep-02 / EP-003 | `POST /v1/identity/auth/register` | Identity | no | `{ proofToken, password, displayName, consentVersion, deviceId }` | `201` `{ accountId, session }` + `Location: /v1/identity/me` | `400`, `401`, `409`, `429` |
| 5 | ep-04 / EP-004 | `POST /v1/identity/auth/login` | Identity | no | `{ email, password, deviceId }` | `200` `session` | `401`, `429` |
| 6 | ep-05 / EP-005 | `POST /v1/identity/auth/refresh` | Identity | no | `{ refreshToken, deviceId }` | `200` `session` | `401`, `429` |
| 7 | ep-05 / EP-007 | `POST /v1/identity/auth/logout` | Identity | **si** | sin cuerpo | `204` | `401`, `429` |
| 8 | ep-06 / EP-008 | `GET /v1/identity/me` | Identity | **si** | — | `200` `{ accountId, email, displayName, createdAt, consentVersion }` | `401`, `404`, `429` |
| 9 | ep-07 / EP-006 | `POST /v1/identity/auth/password-reset` | Identity | no | `{ proofToken, newPassword }` | `204` | `400`, `401`, `429` |

Son 7 specs y 9 operaciones: ep-03 y ep-05 agrupan dos cada una.

## 4. Campos, tipo por tipo

### Objeto `session` (lo devuelven login, refresh y, anidado, register)

| Campo | Tipo | Nota |
|---|---|---|
| `sessionId` | string | identificador de la sesion del dispositivo |
| `accessToken` | string | opaco; va en `Authorization: Bearer` |
| `accessExpiresAt` | fecha UTC | vencimiento del token de acceso |
| `refreshToken` | string | opaco; **cambia en cada refresh** |
| `refreshExpiresAt` | fecha UTC | vencimiento del refresh |

### `POST /v1/identity/auth/otp` — pedir codigo

| Campo | Tipo | Obligatorio | Regla |
|---|---|---|---|
| `email` | string | si | con `@` y dominio con punto, maximo 254 caracteres; se normaliza a minusculas |
| `purpose` | string | si | `registration` o `recovery`, exactamente |

Respuesta `202`: `challengeId` (string) y `retryAfter` (entero, segundos). El codigo **nunca** viaja en la respuesta. La respuesta es la misma exista o no la cuenta: no sirve para averiguar si un correo esta registrado.

### `POST /v1/identity/auth/otp/verify` — canjear codigo por prueba

| Campo | Tipo | Obligatorio | Regla |
|---|---|---|---|
| `challengeId` | string | si | el que devolvio `/auth/otp` |
| `code` | string | si | 6 digitos |

Respuesta `200`: `proofToken` (string) y `expiresAt` (fecha UTC). Un `400` cubre codigo incorrecto, vencido, agotado o ya canjeado: el mensaje no distingue cual.

### `POST /v1/identity/auth/register` — crear cuenta

| Campo | Tipo | Obligatorio | Regla |
|---|---|---|---|
| `proofToken` | string | si | de un desafio con `purpose: registration` |
| `password` | string | si | 8 a 128 caracteres |
| `displayName` | string | si | 2 a 60 caracteres tras recortar espacios |
| `consentVersion` | string | si | version de politicas que acepto la persona |
| `deviceId` | string | no | si falta, la sesion queda como `desconocido` |

El correo **no se envia**: sale del `proofToken`. Respuesta `201`: `accountId` (string) y `session`. Un `409` significa que el correo ya tiene cuenta, con mensaje neutral.

### `POST /v1/identity/auth/login`

| Campo | Tipo | Obligatorio |
|---|---|---|
| `email` | string | si |
| `password` | string | si |
| `deviceId` | string | no |

Correo inexistente y contrasena incorrecta devuelven el **mismo** `401` y tardan lo mismo. No usar la diferencia para nada, porque no hay diferencia.

### `POST /v1/identity/auth/refresh`

| Campo | Tipo | Obligatorio | Regla |
|---|---|---|---|
| `refreshToken` | string | si | el ultimo recibido |
| `deviceId` | string | no | si se envia, debe coincidir con el de la sesion |

**Importante para la app:** reusar un `refreshToken` ya rotado revoca **toda la familia** de esa sesion, y tambien el token nuevo. La persona tendra que entrar de nuevo. Consecuencias practicas:
- serializar el refresh: una sola llamada en vuelo, con las demas peticiones esperando su resultado;
- guardar el nuevo par de forma atomica antes de seguir;
- no reintentar un refresh que fallo con `401`: entonces toca `/auth/login`.

### `POST /v1/identity/auth/logout`

Solo cabecera `Authorization`. Sin cuerpo. Devuelve `204`. Despues de cerrar sesion ese `accessToken` queda revocado: llamarlo otra vez con el mismo token da `401`, que es lo esperado.

### `GET /v1/identity/me`

| Campo de respuesta | Tipo |
|---|---|
| `accountId` | string |
| `email` | string |
| `displayName` | string |
| `createdAt` | fecha UTC |
| `consentVersion` | string |

No incluye contrasena, hashes ni sesiones. El resumen de solicitudes, experiencias y guardados aun no esta: llegara por composicion autorizada, no en este contrato.

### `POST /v1/identity/auth/password-reset`

| Campo | Tipo | Obligatorio | Regla |
|---|---|---|---|
| `proofToken` | string | si | de un desafio con `purpose: recovery` |
| `newPassword` | string | si | 8 a 128 caracteres |

Devuelve `204` exista o no la cuenta. Al cambiar la contrasena se revocan **todas** las sesiones y refresh de esa cuenta: la app debe borrar sus tokens y volver a la pantalla de acceso. Una prueba con `purpose: registration` da `401`.

### `GET /v1/discovery/feed`

| Parametro | Tipo | Obligatorio | Regla |
|---|---|---|---|
| `mode` | string | no | aceptado y reservado; hoy no filtra |
| `zone` | string | no | coincidencia exacta |
| `category` | string | no | coincidencia exacta con el id de categoria |
| `tab` | string | no | aceptado y reservado; hoy no filtra |
| `cursor` | string | no | el `nextCursor` de la pagina anterior; uno invalido se ignora y sirve la primera pagina |
| `limit` | entero | no | por omision 20, acotado entre 1 y 50 |

Respuesta `200`:

| Campo | Tipo | Nota |
|---|---|---|
| `items` | arreglo de tarjetas | ver abajo |
| `nextCursor` | string o `null` | `null` significa ultima pagina |
| `partial` | booleano | `true` cuando falto una fuente; hoy siempre `false` |

Tarjeta del feed: `postId`, `title`, `summary`, `categoryId`, `zone`, `providerId`, `providerName`, `coverMediaId` (string o `null`), `createdAt` (fecha UTC). Solo trabajos publicados. **No** trae coordenadas ni datos de contacto: eso vive detras de consentimiento, en otra spec.

El cursor es opaco: tratarlo como texto y no derivar nada de su contenido.

## 5. Forma de los errores

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Prueba de correo invalida o ya usada.",
  "traceId": "00-...-00"
}
```

| Codigo | Significado en este contrato |
|---|---|
| `400` | entrada invalida, o codigo incorrecto, vencido, agotado o ya canjeado |
| `401` | credenciales invalidas, sin token, token vencido o revocado, prueba de correo invalida o ya usada |
| `404` | la cuenta del token ya no existe |
| `409` | el correo ya tiene cuenta (mensaje neutral) |
| `429` | se paso el limite por IP |

Mostrar `detail` al usuario solo cuando el codigo es `400`. En `401` y `409` el texto es deliberadamente neutral para no revelar si un correo esta registrado; no construir mensajes de UI que lo deduzcan. Guardar `traceId` en el registro de la app ayuda a cruzar incidencias con el backend.

## 6. Flujos completos

**Alta de cuenta:** `POST /v1/identity/auth/otp` con `purpose: registration` → la persona teclea el codigo → `POST /v1/identity/auth/otp/verify` → `POST /v1/identity/auth/register` con el `proofToken` → ya hay sesion, no hace falta iniciar sesion aparte.

**Recuperar acceso:** `POST /v1/identity/auth/otp` con `purpose: recovery` → `POST /v1/identity/auth/otp/verify` → `POST /v1/identity/auth/password-reset` → borrar los tokens guardados → `POST /v1/identity/auth/login` con la contrasena nueva.

**Sesion normal:** `POST /v1/identity/auth/login` → guardar el par → usar `accessToken` → cuando venza o llegue un `401`, un unico `POST /v1/identity/auth/refresh` → guardar el par nuevo. `POST /v1/identity/auth/logout` al salir.

## 7. Limites de hoy que afectan al trabajo movil

1. **No hay proveedor de correo** (decision D04 abierta). El codigo no se envia a ningun buzon: en Development queda escrito en el registro del host, y fuera de Development no se entrega nada. Para probar el alta hace falta leer el log del backend o pedir el codigo al equipo de API.
2. **Una sola URL base.** Ya no hay gateway ni un host por dominio: es una API modular y todo cuelga del mismo host, separado por prefijo de dominio. Aun asi, tengan la URL base en configuracion: lo que cambia al desplegar es el host, no las rutas.
3. **El token de acceso es opaco**, no JWT. No hay claims que leer en el cliente: el estado de la sesion se consulta con `GET /v1/identity/me`. Pasar a JWT es una decision de contrato aun no tomada y cambiaria esta parte.
4. **No guardar tokens en almacenamiento sin cifrar.** El refresh vale 30 dias y, si se filtra, su reuso revoca la sesion de la persona.
5. Lo que **no** existe aun: perfiles de prestador, busqueda, solicitudes, pedidos, mensajeria, notificaciones y listado de sesiones. Estan en el [inventario](inventario.json) con su spec, sin implementar.
6. Este contrato describe lo implementado y comprobado contra base real; **no** es una aceptacion formal. El contrato formal por operacion sigue pendiente en las [fichas](fichas-operaciones.md).
