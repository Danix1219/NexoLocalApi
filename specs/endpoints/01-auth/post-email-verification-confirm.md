# Confirmar el código del correo · `POST /v1/identity/email/verification/confirm`

| Campo | Valor |
|---|---|
| Módulo | Identity |
| Quién | **P** (público, sin token) |
| Pantalla de la maqueta | 7.3 Registro, paso `code` · `ACT.obnext` |
| Spec SDD | nv-01 |
| Idempotente | No. El código se consume al primer acierto |

## 1. Función

Segundo paso del registro. Valida el código de 6 dígitos y devuelve un **`verificationTicket`**: la
prueba de que ese correo es de quien lo pide. El ticket es lo que luego consume
[`POST /v1/identity/accounts`](post-accounts.md) o el restablecimiento de contraseña.

En la maqueta cualquier código de 6 dígitos servía (`code` → siguiente paso). Aquí la comprobación
es real: es uno de los puntos de la constitución §9 ("se reemplazan los códigos OTP aceptados sin
comprobación").

## 2. Petición

```http
POST /v1/identity/email/verification/confirm
Content-Type: application/json
```

```json
{ "email": "ana.lopez@example.com", "code": "481902" }
```

| Campo | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `email` | string | **254** | sí | Se normaliza igual que al enviar |
| `code` | string | **6** exactos | sí | Solo dígitos `[0-9]{6}`. Se compara con el HMAC guardado, en **tiempo constante** |

## 3. Respuesta

`200 OK`

```json
{
  "verificationTicket": "vt_9f3a1c7e8b24d05f6a1b2c3d4e5f6071",
  "expiresInSeconds": 1800,
  "accountExists": false
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `verificationTicket` | string | **64** | Opaco, de un solo uso, TTL 30 min |
| `expiresInSeconds` | int32 | — | 1800 |
| `accountExists` | bool | — | **Solo se devuelve aquí**, ya probada la posesión del correo. Permite a la app enviar al registro o al login sin filtrar nada a un tercero |

**No se devuelve nunca**: datos de la cuenta, nombre, ni si tiene perfil de prestador.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `email.code_invalid` | Código incorrecto. Incrementa `attempts` |
| 410 | `email.code_expired` | No hay clave en Redis: pasaron los 15 min o ya se consumió |
| 429 | `email.too_many_attempts` | 5 intentos fallidos: la clave se borra y hay que pedir otro código |
| 400 | `validation_failed` | `code` no son 6 dígitos |
| 503 | `server.dependency_unavailable` | Redis no responde |

## 5. Datos

**No escribe en MongoDB.**

| Clave Redis | Operación |
|---|---|
| `nexo:otp:email:{emailHash}` | `HGET codeHash`, compara; al acertar **`DEL`** (consumo) |
| `nexo:otp:email:{emailHash}` | Al fallar: `HINCRBY attempts 1`; si llega a **5** → `DEL` |
| `nexo:ticket:{ticket}` | `SET` con `{ email, purpose }`, TTL **1800 s**. Se leerá con `GETDEL` |

Lectura en Mongo: `accounts.findOne({ email }, { _id: 1 })` solo para resolver `accountExists`, con
el índice único `email`. No se tocan otros campos.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`auth`** (20/min por IP) para el endpoint, más el contador propio de `attempts` que
  limita a 5 por código. No consume cuota `otp`: esa política es del **envío**.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Redis** | Lectura y consumo del código, contador de intentos, alta del ticket |
| **Azure Key Vault** | Llave HMAC con la que se comparó el código |
| `CryptographicOperations.FixedTimeEquals` | Comparación en tiempo constante |
| `RandomNumberGenerator` | Generación del ticket (32 bytes → Base64Url) |

## 8. Seguridad y privacidad

1. Comparación en **tiempo constante**: evita deducir el código por el tiempo de respuesta.
2. **Un solo uso**: el acierto borra la clave, de modo que el mismo código no sirve dos veces.
3. **5 intentos** y el código muere: con 10⁶ combinaciones y 5 intentos por código, más el límite de
   10 códigos al día, la probabilidad de acertar a ciegas en un día es ~5 × 10⁻⁵.
4. `accountExists` se devuelve **solo aquí** porque en este punto quien pregunta ya demostró que
   controla el buzón. En el endpoint de envío delatarlo sería enumeración de cuentas.
5. El ticket no contiene el correo legible: es aleatorio y la asociación vive en Redis.
