# Perfil público de un prestador · `GET /v1/discovery/providers/{id}`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **P/C/Pr** (público) |
| Pantalla de la maqueta | 7.8 Perfil de prestador · `V.profile`, `ACT.open` |
| Spec SDD | nv-02 |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve el perfil tal como lo ve otra persona. Es **el endpoint donde más importa la privacidad**:
aquí se decide qué se oculta.

Lo que la maqueta muestra en esta pantalla: tarjeta con avatar, nombre, tipo, insignias, zona y
atención; descripción; servicios y especialidades; horario con "Abierto ahora"; precios; condiciones
declaradas; portafolio; y las pestañas de trabajos y opiniones (que tienen sus propios endpoints).

## 2. Petición

```http
GET /v1/discovery/providers/019400d4-9e5a-7b23-8c91-4f6a7b8c9d01?lat=20.0539&lng=-99.3403
If-None-Match: "a1b2c3d4e5f60718"
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `id` (ruta) | uuid v7 | — | sí | |
| `lat` / `lng` | double | −90..90 / −180..180 | no | Punto del usuario. Sin ellos no se calcula la distancia ni el texto de atención |

## 3. Respuesta

`200 OK` (o `304` con `If-None-Match` coincidente)

```json
{
  "providerId": "019400d4-9e5a-7b23-8c91-4f6a7b8c9d01",
  "kind": "business",
  "name": "Plomería del Centro",
  "owner": "Carlos Ramírez",
  "activity": "Reparación de fugas y tuberías",
  "categories": ["plomeria"],
  "specialties": ["fugas", "calentadores"],
  "bio": "Atendemos fugas…",
  "experience": 8,
  "crew": "team",
  "attention": "both",
  "attentionText": "Su local está a 4 km",
  "distanceKm": 4.2,
  "shopLocation": { "lat": 20.0539, "lng": -99.3403 },
  "publicZone": { "label": "Centro", "point": { "lat": 20.0536, "lng": -99.3398 } },
  "radiusKm": 10,
  "servesYou": true,
  "schedule": { "days": [ … ], "urgent": true },
  "status": { "open": false, "text": "Cerrado · abre mañana a las 8:00" },
  "visit": { "mode": "free" },
  "prices": [{ "service": "Cambio de llave", "fromCents": 35000, "unit": "from", "suffix": "" }],
  "minPrice": { "fromCents": 35000 },
  "payment": ["cash", "transfer"],
  "responseTime": "sameDay",
  "warranty": "30 días en mano de obra",
  "invoice": true,
  "web": "https://plomeriadelcentro.mx",
  "photoUrl": "https://cdn.nexolocal.mx/av/9f3a1c7e.webp",
  "coverUrl": null,
  "portfolio": [{ "url": "…", "thumbUrl": "…", "w": 1200, "h": 1200 }],
  "badges": [
    { "key": "phoneVerified", "level": "verifiedByNexo", "label": "Teléfono confirmado" },
    { "key": "isNew", "level": "declared", "label": "Perfil nuevo" }
  ],
  "trust": {
    "declared": ["experience", "payment", "warranty", "responseTime", "invoice"],
    "verifiedByNexo": ["phone"],
    "userOpinion": { "rating": { "avg": 4.6, "count": 12 }, "recs": 34 }
  },
  "channels": { "chat": true, "call": true },
  "jobs": 18,
  "updatedAt": "2026-11-05T12:00:00Z",
  "updatedText": "Actualizado hace 2 días"
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `attentionText` | string 60 | `attTxt()`: "Va a domicilio" dentro del radio; "Su local está a X km" fuera del radio y a ≤ 10 km |
| `shopLocation` | Point | **Solo si `attention` es `shop` o `both`.** Es el punto que el negocio publica a propósito, y habilita "Cómo llegar" |
| `servesYou` | bool | `serves(p, punto)`: `dist ≤ radiusKm`, o con local `dist ≤ LOCAL_KM (10 km)` |
| `status.text` | string 60 | `status()` de la maqueta: usa el día real y hora fija 9:41 |
| `minPrice` | object | `minItem()`: el precio más bajo publicado |
| `badges[]` | array | Cada insignia **declara su nivel de confianza** |
| `trust` | object | Los **tres niveles separados** y etiquetados |

### Lo que NO se devuelve nunca

| Campo | Por qué |
|---|---|
| `phone` | Constitución §III.6. Solo por la acción explícita "Copiar número", que es [otro endpoint](../12-contacto-reportes/post-contacts-phone.md) auditado |
| `location` (punto exacto) | Si `attention = home`, la proyección lo excluye. Solo sale como `shopLocation` cuando hay local |
| `accountId` | No se expone el id de cuenta; el id público es el del perfil |
| `email` | Nunca |
| Estadísticas de visitas y clics | Son del dueño |

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 404 | `provider.not_found` | No existe, está despublicado, o la cuenta está en `pendingDeletion` |
| 400 | `validation_failed` | `lat`/`lng` fuera de rango |
| 429 | `rate.limit_exceeded` | Política `public`: 60/min por IP |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `provider_profiles` | `findOne({ _id: id, published: true })` con **proyección que excluye `location`** si `attention = home` | `_id` |

La proyección se arma **antes** de la consulta según `attention`, para que el punto exacto no salga
ni de la base cuando no corresponde.

`stats.views` se incrementa con un `$inc` **sin esperar el resultado** (fire-and-forget con
`writeConcern: 0`) o, mejor, se acumula en Redis (`INCR nexo:views:{providerId}`) y el worker lo
baja a Mongo cada 5 minutos: así una visita no cuesta una escritura en el cluster Free.

`rating` y `recs` están **desnormalizados** aquí; los actualiza el worker al consumir
`review.created` y `work.recommended` de Engagement.

## 6. Caché y rate limit

- **L1 2 min / L2 (Redis) 10 min**, clave `prov:{id}:v1`. `ETag` del cuerpo; `304` con
  `If-None-Match`. `Cache-Control: public, max-age=120`.
- La distancia y `attentionText` dependen de `lat`/`lng`, así que **se calculan fuera de la entrada
  cacheada**: se cachea el perfil y se compone la parte geográfica por petición.
- Invalida con el tag `provider:{id}` (perfil editado, opinión nueva, trabajo nuevo).
- Rate limit **`public`** (60/min por IP).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Lectura por `_id` con proyección condicional |
| **Redis** | Caché L2 y contador de visitas |
| **HybridCache** | L1 + protección contra *stampede* (un perfil viral no dispara 100 lecturas) |
| **Front Door CDN** | Foto, portada y portafolio |
| Output cache + ETag | Respuestas `304` para la app |

## 8. Seguridad y privacidad

1. **Los tres niveles de confianza van separados y etiquetados** en `trust` y en cada insignia. La API
   no entrega un "verificado" genérico: el nivel `verifiedByNexo` contiene solo el teléfono
   (constitución §IV.3).
2. El **punto exacto** solo sale como `shopLocation` y solo con local. Para los prestadores a
   domicilio se entrega la zona difuminada, nunca el punto.
3. El teléfono **no está en esta respuesta** ni enmascarado: `channels.call: true` solo indica que la
   acción "Copiar número" está disponible.
4. `404` también para perfiles de cuentas en baja pendiente: no se expone un perfil que el dueño
   quiso retirar.
5. El contador de visitas es agregado y no registra quién visitó.
6. `warranty`, `payment`, `experience` y `responseTime` aparecen en `trust.declared`: la UI está
   obligada a mostrarlos como declaración del prestador.
