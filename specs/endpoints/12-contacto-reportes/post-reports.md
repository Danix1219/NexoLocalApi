# Reportar · `POST /v1/engagement/reports`

| Campo | Valor |
|---|---|
| Módulo | Engagement |
| Quién | **C/Pr** (autenticado) |
| Pantalla de la maqueta | Hoja de reporte, desde el menú "⋯" de perfil, trabajo, solicitud o chat · `ACT.report` |
| Spec SDD | nv-13 |
| Idempotente | Sí — `Idempotency-Key`; el mismo objetivo no se reporta dos veces |

## 1. Función

Envía un reporte para **revisión humana**. Complementa el bloqueo: bloquear corta el contacto de
inmediato, reportar pide que alguien revise y actúe sobre el contenido o la cuenta.

En la maqueta el aviso de reporte no se guardaba en ningún lado; aquí **se persiste y se revisa**
(constitución §9).

## 2. Petición

```http
POST /v1/engagement/reports
Authorization: Bearer <access_token>
Idempotency-Key: 8c5a2e9f-3b16-4d07-a1e8-4f6b8d0a2c35
Content-Type: application/json
```

```json
{
  "targetType": "provider",
  "targetId": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01",
  "category": "scam",
  "reason": "Pidió un anticipo por transferencia y dejó de responder.",
  "evidenceConversationId": "01940309-…"
}
```

| Campo | Tipo | Tamaño | Requerido | Regla |
|---|---|---|---|---|
| `targetType` | string | enum | sí | `provider` \| `work` \| `request` \| `conversation` \| `review` |
| `targetId` | uuid v7 | — | sí | Debe existir |
| `category` | string | enum | sí | `scam` \| `inappropriateContent` \| `harassment` \| `fakeProfile` \| `notAService` \| `other` |
| `reason` | string | **10–300** | sí | **Se modera** (un reporte no es vía para insultar) pero **no se bloquea por ello**: ver abajo |
| `evidenceConversationId` | uuid v7 | — | no | Conversación de la que el revisor podrá leer el contexto. El `sub` debe ser parte |

## 3. Respuesta

`202 Accepted`

```json
{
  "reportId": "0194050d-1289-73b4-c5d6-e7f8091a2b3c",
  "status": "received",
  "message": "Recibimos tu reporte. Lo revisará una persona del equipo.",
  "createdAt": "2026-11-05T20:30:00Z",
  "alsoBlocked": false
}
```

`202` y no `201`: lo que se acepta es la **solicitud de revisión**, cuyo resultado llegará después.

**No se devuelve nunca**: el resultado de la revisión, ni si el objetivo ya tenía otros reportes.
Decirle a quien reporta que hay más reportes sobre esa cuenta expondría a los demás denunciantes.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `resource.not_found` | El objetivo no existe |
| 409 | `report.already_reported` | Ya reportó **ese mismo objetivo**. El cuerpo trae la fecha |
| 403 | `report.own_content` | No se reporta el propio contenido |
| 403 | `conversation.not_participant` | La conversación de evidencia no es suya |
| 400 | `validation_failed` | `reason` de menos de 10 o más de 300, enums inválidos |
| 429 | `rate.limit_exceeded` | Política `user`, más un tope de **20 reportes al día** |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `reports` | `insertOne` | **`{status, createdAt}}`**; único `{reporterId, targetType, targetId}` |
| `audit_events` | Evento `report.created` | `{actorId, occurredAt}` |

Documento:

```js
{
  _id: UUID_v7, reporterId: UUID, targetType: "provider", targetId: UUID,
  category: "scam", reason: "…", evidenceConversationId: UUID?,
  status: "pending",          // pending | reviewing | actioned | dismissed
  resolution: null, reviewedBy: null, reviewedAt: null,
  createdAt: ISODate
}
```

Escrituras derivadas:

- `outbox`: **`report.created`** `{ reportId, targetType, targetId, category }` → el worker:
  - avisa al equipo de moderación (correo con **MailKit** a la bandeja interna);
  - si el objetivo acumula **3 o más reportes de cuentas distintas**, lo marca
    `moderation.status: "pending"` en Discovery, lo que lo **oculta del público** hasta la revisión.
    Es una medida automática y reversible, no una sanción.
- La decisión humana se registra después en el mismo documento (`status`, `resolution`,
  `reviewedBy`, `reviewedAt`): **decisiones auditables**, como exige el documento oficial.

### Sobre moderar el `reason`

El texto pasa por Content Safety, pero un veredicto negativo **no rechaza el reporte**: se marca
`reasonFlagged: true` y se guarda igual. Alguien enfadado describiendo un fraude puede usar palabras
fuertes, y perder ese reporte sería peor que recibirlo con malos modos.

## 6. Caché y rate limit

- Sin caché.
- Rate limit **`user`** + tope propio de **20 al día** en Redis.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | `reports` con único por denunciante y objetivo |
| **Azure AI Content Safety** | Marcado del `reason` (sin rechazarlo) |
| **MailKit** | Aviso al equipo de moderación |
| **Redis** | Idempotencia y tope diario |
| `audit_events` | Registro del reporte |

## 8. Seguridad y privacidad

1. **El reportado no se entera de quién lo reportó.** Nunca se le notifica el reporte ni la identidad
   del denunciante; solo se le comunica la decisión si hay acción sobre su contenido.
2. El índice único `{reporterId, targetType, targetId}` evita el brigadeo por repetición desde una
   sola cuenta.
3. El umbral de **3 reportes de cuentas distintas** para ocultar contenido es automático y
   **reversible**: oculta, no borra, y la revisión humana decide.
4. La conversación de evidencia se valida: solo se puede aportar un chat del que se es parte, para
   que el reporte no sirva de pretexto para abrir chats ajenos a un revisor.
5. Las decisiones quedan registradas con quién y cuándo: **moderación auditable**, no arbitraria.
6. El `reason` se guarda aunque esté marcado: no se descarta una denuncia por su tono.
