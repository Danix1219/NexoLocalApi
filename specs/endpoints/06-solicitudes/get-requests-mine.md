# Mis solicitudes · `GET /v1/discovery/requests/mine`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C** (autenticado) |
| Pantalla de la maqueta | 7.4 Inicio → pestaña "Mis solicitudes" · `myReqFoot` |
| Spec SDD | nv-06 |
| Idempotente | Sí (lectura) |

## 1. Función

Lista las solicitudes propias con el **resumen de interesados**, que es lo que la maqueta muestra en
la tarjeta: *"N interesados · Compara y elige a quién pedirle · Ver"*, con hasta 4 fotos y "+N".

Es la pantalla desde la que el cliente entra a
[Interesados](../07-interes/get-requests-id-interested.md) y de ahí a pedir el servicio.

## 2. Petición

```http
GET /v1/discovery/requests/mine?status=open&cursor=&limit=20
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `status` | string | enum | no | `open` \| `assigned` \| `closed` \| `all`. Por defecto `all` |
| `cursor` | string | **64** | no | |
| `limit` | int32 | **1–50** | no | Por defecto 20 |

## 3. Respuesta

`200 OK`

```json
{
  "items": [
    {
      "requestId": "01940107-…",
      "category": "plomeria",
      "title": "Fuga en el baño",
      "descriptionExcerpt": "Hay una fuga debajo del lavabo…",
      "urgency": "today",
      "urgencyText": "Hoy",
      "zoneLabel": "Centro",
      "imageUrl": "https://cdn…",
      "status": "open",
      "interestCount": 3,
      "interestedPreview": [
        { "providerId": "019400d4-…", "name": "Plomería del Centro", "photoUrl": "…" },
        { "providerId": "019400e5-…", "name": "Hugo Torres", "photoUrl": null }
      ],
      "interestedMoreCount": 1,
      "summaryText": "3 interesados · Compara y elige a quién pedirle",
      "pickedProvider": null,
      "ago": "hace 2 horas",
      "createdAt": "2026-11-05T17:30:00Z"
    }
  ],
  "nextCursor": null,
  "hasMore": false
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `descriptionExcerpt` | string | **200** | Recorte |
| `interestedPreview[]` | array | **máx. 4** | Las fotos que la maqueta apila en la tarjeta |
| `interestedMoreCount` | int32 | — | El "+N" |
| `summaryText` | string | **120** | Texto exacto de la maqueta según el estado: *Aún sin interesados* → *N interesados · Compara y elige a quién pedirle* → *Elegiste a X* |
| `pickedProvider` | object \| null | — | Presente cuando `status = assigned` |

**No se devuelve nunca**: el `location` exacto (ni al dueño en esta lista), ni el teléfono de los
prestadores interesados.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `status` fuera del enum |
| 400 | `request.cursor_invalid` · `request.limit_exceeded` | |
| 401 | `auth.token_expired` | |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `service_requests` | `find({ clientId: sub, status? })` ordenado por `_id: -1`, con proyección **sin `location`** | **`{clientId: 1, createdAt: -1}`** |
| `interests` (Engagement) | Resumen por contrato: `IInterestSummaryContract.GetSummariesAsync(requestIds)` | `{requestId, providerId}` único |

El resumen de interesados vive en **Engagement**, así que se obtiene por **interfaz de contrato** con
timeout de 2 s y circuit breaker — Discovery no consulta la base de Engagement
(constitución §II.1 y §II.6).

Degradación: si Engagement no responde, se devuelven las solicitudes con `interestCount` del campo
desnormalizado y `interestedPreview: []`. La pantalla funciona, solo sin las fotitos.

`interestCount` está desnormalizado en `service_requests` y lo actualiza el worker al consumir
`interest.expressed` / `interest.withdrawn`.

## 6. Caché y rate limit

- **Sin caché**: es una pantalla personal y viva. `Cache-Control: private, no-store`.
- Rate limit **`user`** (120/min).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Lectura por `clientId` con índice compuesto |
| **Polly v8** | Timeout 2 s + circuit breaker en la llamada a Engagement |
| **Front Door CDN** | Foto del problema y avatares de los interesados |

## 8. Seguridad y privacidad

1. El filtro es `clientId: sub`: **solo las propias**. No hay parámetro de cliente.
2. La proyección excluye `location`: ni el dueño necesita su punto exacto en una lista.
3. De los prestadores interesados se devuelve lo público (nombre y foto); el teléfono exige la acción
   auditada "Copiar número".
4. La llamada entre módulos es de solo lectura, por contrato, con degradación elegante.
5. `summaryText` se compone en el servidor para que el texto de la maqueta no se reinvente en la app.
