# Me interesa · `POST /v1/engagement/requests/{id}/interest`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **Pr** (autenticado, con perfil) |
| Pantalla de la maqueta | 7.5 Oportunidades → "Me interesa" · `ACT.reqint` |
| Spec SDD | nv-06 |
| Idempotente | Sí — expresarlo dos veces deja el mismo estado |

## 1. Función

El prestador manifiesta interés en una solicitud. Con eso entra a la lista de **Interesados** que el
cliente revisa para elegir a quién pedirle el servicio.

Reglas de la maqueta:

- Se puede **retirar** el interés ([`DELETE`](delete-requests-id-interest.md)).
- Se puede expresar interés **incluso en otra categoría** si la solicitud le es visible.
- El paso intermedio "Me conviene" **se quitó el 2026-10-20** (`ACT.reqshort` y `r.short` quedaron sin
  uso): **no se implementa**.
- Suma a la estadística **"Con tu interés"** del prestador.

## 2. Petición

```http
POST /v1/engagement/requests/01940107-bc23-7d45-8e67-9f01a2b3c4d5/interest
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{ "note": "Puedo pasar hoy por la tarde" }
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | Solicitud `open` y visible para él |
| `note` | string | **200** | no | Mensaje corto al cliente. **Moderado** |

## 3. Respuesta

`201 Created`

```json
{
  "requestId": "01940107-…",
  "interested": true,
  "interestCount": 4,
  "createdAt": "2026-11-05T18:05:00Z",
  "canMessage": true
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `interestCount` | int32 | Total ya actualizado |
| `canMessage` | bool | `false`: **el prestador no inicia el chat.** Solo puede escribir si el cliente abre la conversación o lo elige |

**No se devuelve nunca**: datos del cliente más allá de lo que ya daba la oportunidad (iniciales),
ni la lista de los demás interesados.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `request.not_found` | No existe, está cerrada, o **no le es visible** (fuera de su radio) |
| 403 | `interest.own_request` | Es su propia solicitud |
| 409 | `interest.request_assigned` | La solicitud ya tiene prestador asignado: ya no admite interesados |
| 403 | `provider.profile_required` | Sin perfil de prestador |
| 403 | `content.inappropriate` | Content Safety rechazó la nota |
| 429 | `rate.limit_exceeded` | Política `user` |

**No hay `409` por interés repetido**: el índice único lo absorbe y se responde `200`.

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `interests` | `updateOne({ requestId, providerId }, { $setOnInsert: { clientId, note, createdAt } , $unset: { withdrawnAt: "" } }, { upsert: true })` | **único `{requestId, providerId}`**; `{clientId, requestId}`; `{providerId, createdAt: -1}` |

El `$unset: withdrawnAt` permite **volver a interesarse** tras haberlo retirado, reusando el mismo
documento en vez de acumular filas.

La visibilidad de la solicitud (que esté en su radio y abierta) se comprueba contra Discovery por
**interfaz de contrato** (`IRequestVisibilityContract`), con timeout 2 s y circuit breaker. Si
Discovery no responde → `503`: no se puede permitir un interés sobre una solicitud que quizá no le
corresponde.

Escrituras derivadas:

- `outbox`: **`interest.expressed`** `{ requestId, providerId, clientId }` → el worker:
  - incrementa `service_requests.interestCount` en Discovery;
  - incrementa la estadística `interestsGiven` del prestador;
  - **notifica al cliente**: "N prestadores interesados" con botón "Ver y elegir".

## 6. Caché y rate limit

- Invalida las entradas de oportunidades del prestador y la lista de interesados de la solicitud.
- Rate limit **`user`** (120/min). No es `publish`: no crea contenido público.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `upsert` con índice único |
| **Azure AI Content Safety** | Moderación de la nota antes de guardar |
| **Polly v8** | Timeout + circuit breaker en la comprobación de visibilidad |
| **FirebaseAdmin** (worker) | Push al cliente |
| Outbox → Storage Queue | Contadores en Discovery y aviso al cliente |

## 8. Seguridad y privacidad

1. **La visibilidad se verifica en el servidor**: no basta conocer el id de una solicitud para
   interesarse en ella. Si está fuera del radio, `404`.
2. `canMessage: false` sostiene la regla de la maqueta: **el cliente inicia el chat**. Un prestador
   interesado no puede escribirle sin más; evita que publicar una solicitud se convierta en recibir
   mensajes no pedidos.
3. La nota pasa por moderación: es texto que el cliente leerá.
4. Un prestador **no ve a los otros interesados**: eso es información del cliente para decidir.
5. El índice único evita inflar el contador con llamadas repetidas.
