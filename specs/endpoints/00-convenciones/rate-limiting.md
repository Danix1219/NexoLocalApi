# Rate limiting

> **Cambio respecto a la guía original**: los contadores exactos ya **no** viven en la colección
> `rate_counters` de MongoDB. Ahora son claves de **Redis** (`INCR` + `EXPIRE`).
> La política `otp` cambia su partición de *celular + IP* a **correo + IP**.

---

## 1. Políticas

| Política | Partición | Límite | Almacén | Se aplica en |
|---|---|---|---|---|
| `public` | IP | 60 / min | memoria por réplica | GET públicos: categorías, búsqueda, sugerencias, feed, mapa, perfil público |
| `user` | `sub` del JWT | 120 / min (token bucket) | memoria por réplica | Resto de los endpoints autenticados |
| `auth` | IP | 20 / min | memoria por réplica | `/connect/token`, logout |
| `otp` | **correo + IP** | **3 cada 15 min**, **10 al día** | **Redis** (exacto) | Envío de código de verificación y de recuperación |
| `publish` | `sub` | **10** solicitudes · **30** trabajos · **20** opiniones al día | **Redis** | Publicar contenido |
| `contact` | `sub` | **40 al día** | **Redis** | Copiar número |
| `chat` | `sub` | 60 mensajes / min | memoria por réplica | Enviar mensaje |
| `upload` | `sub` | **30 al día** | **Redis** | Pedir URL SAS de subida |
| `ai` | `sub` | **20 al día** | **Redis** | Asistencia generativa, si se proxy-ea por el backend |

**Por qué dos almacenes**: los límites de *negocio* (cuántas solicitudes puede publicar alguien al
día) tienen que ser exactos entre las dos réplicas, así que van a Redis. Los límites de *abuso*
(peticiones por minuto) toleran ser aproximados por réplica y se resuelven en memoria, que es más
rápido y no depende de Redis.

---

## 2. Claves de Redis

```
nexo:rl:{politica}:{particion}:{ventana}
```

- `particion`: `sub` (UUID de la cuenta), o `sha256(email)[0..16] + ":" + ip` en `otp`.
- `ventana`: marca de tiempo truncada al tamaño de la ventana — `20261105` para los límites diarios,
  `202611051415` para los de 15 min.
- Operación: `INCR` y, si el resultado es 1, `EXPIRE` con la vida de la ventana. Una sola ida y
  vuelta con un script Lua para que sea atómico.

Las ventanas diarias se cuentan en **UTC**, no en hora local, para que coincidan entre réplicas.
Documentado aquí porque para el usuario el día "se reinicia" a las 18:00 del día anterior en
horario del centro de México.

---

## 3. Respuesta al exceder el límite

`429` con Problem Details y `code: rate.limit_exceeded`:

```
Retry-After: 847
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1762387200
```

| Cabecera | Tipo | Regla |
|---|---|---|
| `Retry-After` | int32 (segundos) | Siempre presente |
| `X-RateLimit-Limit` | int32 | Límite de la ventana |
| `X-RateLimit-Remaining` | int32 | 0 al rechazar |
| `X-RateLimit-Reset` | int64 | Epoch en segundos |

El `detail` dice cuándo se puede volver a intentar, en palabras ("Puedes publicar otra solicitud
mañana"), sin número de intentos restantes en las políticas de abuso — eso solo ayuda a quien abusa.

---

## 4. Casos especiales

| Caso | Regla |
|---|---|
| Visitante sin cuenta | Solo políticas por IP (`public`, `auth`, `otp`). No puede alcanzar las de `sub` porque esos endpoints exigen JWT |
| Varias cuentas tras una NAT | `otp` cuenta por **correo + IP**, de modo que una escuela o cibercafé no bloquea a todos: cada correo tiene su propio contador y la IP solo suma en combinación |
| Reintento automático de la app | Los `429` **no** se reintentan desde el cliente; Dio respeta `Retry-After` |
| Bloqueo por intentos de login | Es aparte del rate limit: 5 fallos consecutivos → `423 auth.account_locked` 15 min, contado en `nexo:lock:{emailHash}` |
| Redis caído | Se degrada a los límites en memoria por réplica, que son **más estrictos**. Nunca se desactiva el límite |

---

## 5. Implementación

- `Microsoft.AspNetCore.RateLimiting` para las políticas en memoria, declaradas en el `MapGroup` de
  cada módulo.
- Un `IRateLimiterPolicy` propio, respaldado por Redis, para las políticas exactas; se resuelve
  antes del handler, en el pipeline del endpoint.
- Cada `.md` de endpoint nombra su política en la sección *Caché y rate limit*. Un endpoint sin
  política declarada **no pasa el gate** del checklist.
