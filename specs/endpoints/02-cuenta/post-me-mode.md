# Cambiar de modo · `POST /v1/identity/me/mode`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.13 Cuenta → "Modo de uso" · `ACT.mode` |
| Spec SDD | nv-02 |
| Idempotente | Sí (pedir el modo actual devuelve `200` sin cambios) |

## 1. Función

Cambia entre **Modo Cliente** y **Modo Proveedor** y emite tokens nuevos con el `mode` actualizado.
Modelo de cuenta única: las capacidades de cliente están siempre activas y el perfil profesional es
opcional.

Reglas de la maqueta (`ACT.mode`):

- Si va a `provider` y **no** tiene perfil, la app abre el alta de perfil
  (`ACT.startpro`, que empieza en "¿Cómo vas a ofrecer tus servicios?"). Este endpoint responde
  `409` con `nextStep: "createProvider"` para indicarlo.
- Al cambiar se **reinicia la navegación al Inicio**, se limpia la búsqueda y la comparación, y se
  guarda el modo en la cuenta (`acct.mode`) para restaurarlo en el siguiente inicio de sesión.
- El tipo de perfil (**Persona con oficio** o **Negocio**) **no** limita el cambio: ambos alternan.

**Cambiar de modo cambia la experiencia; nunca otorga permisos nuevos** (constitución §3).

## 2. Petición

```http
POST /v1/identity/me/mode
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "mode": "provider" }
```

| Campo | Tipo | Rango | Requerido | Regla |
|---|---|---|---|---|
| `mode` | string | enum | sí | `client` \| `provider` |

## 3. Respuesta

`200 OK`

```json
{
  "currentMode": "provider",
  "hasProvider": true,
  "accessToken": "eyJhbGciOi…",
  "refreshToken": "rt_c71a…",
  "expiresInSeconds": 600,
  "toast": "Modo Proveedor"
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `currentMode` | string | El modo ya aplicado |
| `hasProvider` | bool | |
| `accessToken` | string | **Nuevo**, con el `mode` actualizado |
| `refreshToken` | string | **Nuevo**: se rota la familia al cambiar el contenido del token |
| `toast` | string 40 | Texto que la maqueta muestra: "Modo Cliente" / "Modo Proveedor" |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 409 | `provider.profile_required` | Pide `provider` sin perfil profesional. El cuerpo trae `nextStep: "createProvider"` |
| 400 | `validation_failed` | `mode` fuera del enum |
| 403 | `account.pending_deletion` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `updateOne({ _id: sub }, { $set: { currentMode, updatedAt } })` | `_id` |
| `openiddict.tokens` | Rota la familia del refresh y emite el par nuevo | `authorizationId` |

`hasProvider` se **lee de `accounts`**, no del token: el campo lo actualiza el worker al consumir
`provider.created` de Discovery.

`audit_events`: `account.mode_changed` `{ accountId, from, to }`.

## 6. Caché y rate limit

- Invalida `me:{accountId}` y el tag `account:{id}`.
- Rate limit **`user`** (120/min). Basta: es una acción manual.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **OpenIddict** | Emisión y rotación del par de tokens |
| **MongoDB** | `accounts.currentMode` |
| **Redis** | Invalidación de caché |

## 8. Seguridad y privacidad

1. **El modo no es un permiso.** Cada endpoint de prestador verifica que exista el perfil profesional
   y que el recurso sea suyo, en la misma consulta. Un token con `mode: provider` falsificado no
   abriría nada.
2. Se emiten tokens nuevos porque el `mode` viaja como claim; sin rotar, la app tendría hasta 10
   minutos de desajuste entre lo que cree y lo que el servidor dice.
3. El `409` con `nextStep` evita que la app tenga que adivinar: el servidor le dice qué sigue.
4. El modo persiste en la cuenta para restaurarse al iniciar sesión, igual que en la maqueta.
