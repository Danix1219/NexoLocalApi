# Mi cuenta · `GET /v1/identity/me`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | 7.13 Cuenta · `V.account` |
| Spec SDD | nv-04 |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve los datos de la cuenta propia: correo, nombre, modo activo, si tiene perfil profesional y el
estado del celular opcional. Es lo que pinta la pestaña Cuenta y lo que la app usa para decidir si
muestra el conmutador de modo.

## 2. Petición

```http
GET /v1/identity/me
Authorization: Bearer <access_token>
```

Sin cuerpo ni parámetros.

## 3. Respuesta

`200 OK`

```json
{
  "accountId": "019400a1-7c3e-7f21-9b84-3d5e6f70a1b2",
  "email": "ana.lopez@example.com",
  "emailVerifiedAt": "2026-11-02T18:40:11Z",
  "name": "Ana López",
  "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
  "currentMode": "client",
  "hasProvider": false,
  "phone": "+527715550100",
  "status": "active",
  "deletionRequestedAt": null,
  "createdAt": "2026-11-02T18:41:02Z"
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `accountId` | uuid v7 | — | |
| `email` | string | 254 | **El propio**: aquí sí se devuelve completo |
| `name` | string | 80 | |
| `photoUrl` | string | 200 | URL de CDN; ausente si no tiene foto |
| `currentMode` | string | enum | `client` \| `provider` |
| `hasProvider` | bool | — | Si `false`, la app abre el alta de perfil al cambiar a Proveedor |
| `phone` | string | 13 | **El propio, completo.** Ausente si no lo registró |
| `emailVerifiedAt` | date | — | Confirma el correo; no acredita control del móvil |
| `status` | string | enum | `active` \| `pendingDeletion` |
| `deletionRequestedAt` | date | — | Presente solo en `pendingDeletion`; la app muestra los días restantes |

**No se devuelve nunca**: `passwordHash`, `termsVersion` interna, ni el teléfono o correo **de otra
persona** (este endpoint es solo de uno mismo).

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 401 | `auth.token_expired` · `auth.token_revoked` | |
| 404 | `resource.not_found` | La cuenta fue eliminada |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `findOne({ _id: sub })` con proyección explícita | `_id` |

Proyección explícita, nunca el documento entero: `passwordHash` no debe salir ni de la base.

## 6. Caché y rate limit

- Sin caché interna: consulta por `_id` para reflejar cambios entre réplicas inmediatamente.
  `Cache-Control: private, no-store`. Decisión de este [incremento](implementacion.md).
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Lectura por `_id` |
| **Front Door CDN** | Sirve `photoUrl` |

## 8. Seguridad y privacidad

1. El `sub` del token determina qué cuenta se lee: **no hay parámetro de id**, así que no existe
   superficie de BOLA.
2. El correo y el celular propios se devuelven completos; la restricción de la constitución §III.6
   aplica a mostrar el número **de otra persona**.
3. Proyección explícita en la consulta: el hash de la contraseña no viaja ni dentro del servidor.
4. En `pendingDeletion` se devuelve `deletionRequestedAt` para que la app pueda ofrecer cancelar la
   baja (derecho ARCO).
