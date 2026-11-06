# Crear la cuenta (registro) · `POST /v1/identity/accounts`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **P** (público, con `verificationTicket`) |
| Pantalla de la maqueta | 7.3 Registro, paso `who` (cliente) o `pass` (prestador) · `ACT.obnext` |
| Spec SDD | nv-01 |
| Idempotente | Sí — `Idempotency-Key` obligatorio |

## 1. Función

Crea la cuenta consumiendo el ticket del correo verificado. Es el tercer y último paso del registro
de cliente (`correo → código → datos`) y el paso `pass` del registro de prestador.

Reglas de la maqueta:

- **Toda cuenta nace en Modo Cliente** (`currentMode = client`), aunque venga del flujo "Ofrecer
  servicios": el perfil profesional se crea después, con
  [`POST /v1/discovery/providers`](../03-perfil-prestador/post-providers.md).
- Los **datos de cuenta se piden una sola vez**: nombre, ubicación de búsqueda, contraseña y
  términos (paso `who`, `passBlock`).
- La contraseña se valida con la regla `pwOK`: **≥ 8 caracteres, al menos una letra y un número**.
- Aceptar **Términos y Aviso de privacidad** es obligatorio y se sella con fecha.
- `name` es el nombre de la **persona** (`d.uname`), distinto del nombre del perfil profesional
  (`d.name`), que se pide en el alta del perfil.

## 2. Petición

```http
POST /v1/identity/accounts
Content-Type: application/json
Idempotency-Key: 7f3c1a9e-2b44-4d05-8e61-0a9b3c7d5e21
```

```json
{
  "verificationTicket": "vt_9f3a1c7e8b24d05f6a1b2c3d4e5f6071",
  "name": "Ana López",
  "password": "Nexo2026ok",
  "acceptedTerms": true,
  "searchLocation": { "lat": 20.0539, "lng": -99.3403 },
  "searchZoneLabel": "Centro",
  "searchRadiusKm": 10,
  "intent": "client"
}
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `verificationTicket` | string | **64** | sí | De un solo uso. Se consume con `GETDEL` |
| `name` | string | **80** | sí | Mínimo 2 caracteres. Se recorta. No se permiten solo espacios ni solo dígitos |
| `password` | string | **8–64** | sí | Al menos una letra y un número (`pwOK`). **No se registra en ningún log** |
| `acceptedTerms` | bool | — | sí | Debe ser `true`; `false` → `400` |
| `searchLocation.lat` | double | **−90 a 90** | sí | Ubicación de búsqueda inicial |
| `searchLocation.lng` | double | **−180 a 180** | sí | |
| `searchZoneLabel` | string | **60** | sí | Etiqueta de zona (`areaLabel`) |
| `searchRadiusKm` | int32 | **{2,5,10,20}** | no | Por defecto **10** |
| `intent` | string | enum | no | `client` \| `provider`. Solo informa a la app qué pantalla abrir después; **no otorga permisos** |

## 3. Respuesta

`201 Created` · `Location: /v1/identity/me`

```json
{
  "accountId": "019400a1-7c3e-7f21-9b84-3d5e6f70a1b2",
  "email": "ana.lopez@example.com",
  "name": "Ana López",
  "currentMode": "client",
  "hasProvider": false,
  "nextStep": "home"
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `accountId` | uuid v7 | |
| `email` | string | El propio, ya verificado |
| `currentMode` | string | Siempre `client` al crear |
| `hasProvider` | bool | Siempre `false` al crear |
| `nextStep` | string | `home` si `intent = client`; `createProvider` si `intent = provider` |

### Este endpoint no abre sesión

**No devuelve `accessToken` ni `refreshToken`.** Tras el `201`, la app inicia sesión con
[`POST /connect/token`](post-connect-token-password.md) usando el correo y la contraseña que el
usuario acaba de elegir, que ya tiene en memoria. Son dos llamadas seguidas, sin pedirle nada más.

Decidido el 2026-11-06 a petición del equipo móvil, que reportó la diferencia contra el contrato.
El motivo es que solo OpenIddict emite tokens, y solo en su endpoint `/connect/token`. Devolverlos
aquí obligaría a un segundo camino de emisión: dos sitios donde acertar con la vida del token, los
claims, la rotación y la revocación, y dos donde equivocarse. Una ida y vuelta de ~200 ms en el
registro, que ocurre una vez por usuario, no justifica ese riesgo permanente.

**No se devuelve nunca**: `passwordHash`, `phone` (no existe aún), ni la ubicación exacta.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `email.ticket_invalid` | Ticket inexistente, caducado o ya usado |
| 409 | `email.already_registered` | Ya hay cuenta con ese correo. **Seguro aquí**: el ticket prueba que el buzón es suyo |
| 400 | `password.too_weak` | Menos de 8, o sin letra, o sin número |
| 400 | `validation_failed` | `acceptedTerms` en `false`, nombre vacío, coordenadas fuera de rango |
| 400 | `request.idempotency_key_missing` | |
| 409 | `request.idempotency_key_reused` | Misma llave con cuerpo distinto |
| 503 | `server.dependency_unavailable` | Redis caído: no se puede consumir el ticket |

## 5. Datos

**`nexo_identity`**

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `insertOne` | `email` **único** (atrapa la carrera de dos registros simultáneos → `409`) |

Documento creado:

```js
{
  _id: UUID_v7, email: "ana.lopez@example.com", passwordHash: "<argon2id>",
  emailVerifiedAt: ISODate, name: "Ana López",
  currentMode: "client", hasProvider: false, status: "active",
  termsAcceptedAt: ISODate, termsVersion: "2026-10-20",
  createdAt: ISODate, updatedAt: ISODate
}
```

**`nexo_discovery`** — por evento, no en la misma transacción:

| Colección | Operación |
|---|---|
| `user_profiles` | `insertOne` con `_id = accountId`, `name`, `searchLocation`, `searchZoneLabel`, `searchRadiusKm` |

Escrituras derivadas:

- `outbox`: evento **`account.created`** `{ accountId, email, name, searchLocation }` → el worker crea
  el `user_profiles` en Discovery. **Los módulos no escriben en la base del otro.**
- `audit_events`: `account.created` con `actorId = accountId`.
- Redis: `GETDEL nexo:ticket:{ticket}`; `SET nexo:idem:{key}` 24 h con el hash del cuerpo y la
  respuesta.

Validator de la colección (`$jsonSchema`): `email` `maxLength 254` y patrón de correo, `name`
`maxLength 80`, `passwordHash` `maxLength 100`, `status` en el enum, `termsAcceptedAt` requerido.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`auth`** (20/min por IP). No se necesita más: el ticket ya exige haber pasado por
  `otp`, que es la política restrictiva.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Argon2id** (`Konscious.Security.Cryptography.Argon2`) | Hash de la contraseña. Parámetros: 4 iteraciones, 64 MB, paralelismo 2; sal de 16 bytes |
| **OpenIddict** | Emisión del par access + refresh |
| **Redis** | Consumo del ticket e idempotencia |
| **MongoDB** | `accounts` con índice único |
| Outbox → Storage Queue | Propagación a Discovery |
| **MailKit** | Correo de bienvenida, encolado al worker (no bloquea la respuesta) |

## 8. Seguridad y privacidad

1. **La contraseña nunca se registra**: el DTO de entrada está marcado para que el logging
   estructurado la omita, y el `detail` de los errores no la repite.
2. El `409 email.already_registered` solo es seguro **porque hay ticket**: quien pregunta ya probó
   que controla ese buzón, así que no es enumeración.
3. La carrera de dos registros simultáneos la resuelve el **índice único**, no una comprobación
   previa de existencia (que tendría ventana de carrera).
4. `intent` es una pista de navegación, **no un permiso**: el Modo Proveedor solo se habilita cuando
   existe el perfil profesional, y lo verifica el servidor en cada petición.
5. `termsVersion` se guarda para poder demostrar **qué** versión aceptó cada quien (LFPDPPP).
6. La ubicación de búsqueda es un dato de preferencia, no de domicilio: no se difunde en ninguna
   respuesta pública.
