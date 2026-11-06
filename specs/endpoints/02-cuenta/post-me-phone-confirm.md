# Confirmar el celular · `POST /v1/identity/me/phone/confirm`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Insignia *Teléfono confirmado* (`badges`, `p.ok`) |
| Spec SDD | nv-02 |
| Idempotente | No. El código se consume |

> **Endpoint nuevo.** Es el que **enciende la insignia *Teléfono confirmado***. Sin él, esa señal de
> confianza de la maqueta nunca se activaría tras el cambio a identidad por correo.

## 1. Función

Valida el código enviado por SMS, mueve `phonePending` a `phone` y sella `phoneVerifiedAt`. A partir
de ahí el prestador puede activar el canal de llamada y aparece con la insignia.

En la maqueta la insignia es `p.ok` y afecta el **orden por relevancia** de la búsqueda
(los de teléfono confirmado primero), así que confirmar el celular cambia la posición en resultados.

## 2. Petición

```http
POST /v1/identity/me/phone/confirm
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "code": "740316" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `code` | string | **6** exactos | sí | Solo dígitos. Comparación en **tiempo constante** contra el HMAC guardado |

## 3. Respuesta

`200 OK`

```json
{
  "phone": "+527715550100",
  "phoneVerifiedAt": "2026-11-05T16:22:40Z",
  "badgePhoneVerified": true,
  "callChannelAvailable": true
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `phone` | string 13 | **El propio**, completo |
| `phoneVerifiedAt` | date | |
| `badgePhoneVerified` | bool | Ya puede mostrarse la insignia |
| `callChannelAvailable` | bool | Ya se puede activar `channels.call` en el perfil |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `phone.code_invalid` | Código incorrecto. Incrementa `attempts` |
| 410 | `phone.code_expired` | Pasaron los 15 min o ya se consumió |
| 429 | `phone.too_many_attempts` | 5 intentos: se borra el código |
| 409 | `phone.already_in_use` | Otra cuenta confirmó ese número en el intervalo (lo atrapa el índice único) |
| 400 | `phone.no_pending` | No hay número pendiente: hay que llamar antes a `POST /me/phone` |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `accounts` | `updateOne({ _id: sub }, { $set: { phone, phoneVerifiedAt, updatedAt }, $unset: { phonePending: "" } })` | `phone` único **disperso** (resuelve la carrera → `409`) |

| Clave Redis | Operación |
|---|---|
| `nexo:otp:phone:{accountId}` | `HGET codeHash`, comparar; al acertar `DEL` |
| idem | Al fallar `HINCRBY attempts 1`; a los 5 → `DEL` |

Escrituras derivadas:

- `outbox`: **`account.phone_verified`** `{ accountId, phoneVerifiedAt }` → el worker pone
  `phoneVerified: true` en `provider_profiles` (Discovery) e invalida el tag `provider:{id}`, con lo
  que la búsqueda recalcula el orden por relevancia.
- `audit_events`: `phone.verified` `{ accountId, phoneHash }`.

## 6. Caché y rate limit

- Invalida `me:{accountId}`, el tag `account:{id}` y, si tiene perfil, `provider:{providerId}` y las
  búsquedas de su zona.
- Rate limit **`user`**, más el contador propio de 5 intentos por código.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Redis** | Lectura y consumo del código, contador de intentos |
| **MongoDB** | `accounts` con índice único disperso |
| `CryptographicOperations.FixedTimeEquals` | Comparación en tiempo constante |
| Outbox → Storage Queue | Propagación a Discovery para la insignia y el orden de búsqueda |

## 8. Seguridad y privacidad

1. **Un solo uso** y 5 intentos por código, igual que en el correo.
2. El **índice único disperso** es lo que garantiza que un celular confirmado no quede en dos
   cuentas; no se confía en una comprobación previa.
3. La insignia la escribe el worker en Discovery **por evento**: Identity no toca la base de otro
   módulo.
4. La insignia significa exactamente **control del número**, no identidad legal ni competencia
   profesional (constitución §2), y la UI debe decirlo así.
5. Confirmar el celular **no** lo hace público: sigue oculto en toda respuesta ajena.
