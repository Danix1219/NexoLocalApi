# Comparar dos prestadores · `GET /v1/discovery/providers/compare`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **C** (autenticado) |
| Pantalla de la maqueta | 7.9 Comparar · `V.compare`, `ACT.cmp` |
| Spec SDD | nv-02 |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve dos perfiles alineados campo a campo para la tabla de comparación. La maqueta limita la
comparación a **máximo 2** prestadores (`S.cmp`), y el servidor lo hace cumplir.

El valor de este endpoint frente a dos llamadas a `GET /providers/{id}` es que calcula aquí las
filas de la tabla, incluidas las que exigen comparar: quién está más cerca, quién tiene el precio
mínimo más bajo y quién responde antes.

## 2. Petición

```http
GET /v1/discovery/providers/compare?ids=019400d4-...,019400e5-...&lat=20.0539&lng=-99.3403
Authorization: Bearer <access_token>
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `ids` | string | **2 uuid** separados por coma, 73 caracteres | sí | **Exactamente 2**, distintos entre sí |
| `lat` / `lng` | double | −90..90 / −180..180 | no | Para distancia y `attentionText` |

## 3. Respuesta

`200 OK`

```json
{
  "providers": [
    { "providerId": "019400d4-…", "name": "Plomería del Centro", "photoUrl": "…", "kind": "business" },
    { "providerId": "019400e5-…", "name": "Hugo Torres", "photoUrl": "…", "kind": "trade" }
  ],
  "rows": [
    { "key": "rating",       "label": "Calificación",        "values": ["4.6 (12)", "4.9 (7)"],              "best": 1 },
    { "key": "recs",         "label": "Recomendaciones",     "values": ["34", "12"],                          "best": 0 },
    { "key": "minPrice",     "label": "Precio desde",        "values": ["$350", "A cotizar"],                 "best": 0 },
    { "key": "visit",        "label": "Costo de visita",     "values": ["Sin costo", "$150"],                 "best": 0 },
    { "key": "distance",     "label": "Distancia",           "values": ["Su local está a 4 km", "a ~2 km"],   "best": 1 },
    { "key": "attention",    "label": "Tipo de atención",    "values": ["Local y domicilio", "A domicilio"],  "best": null },
    { "key": "status",       "label": "Ahora",               "values": ["Cerrado", "Abierto"],                "best": 1 },
    { "key": "urgent",       "label": "Atiende urgencias",   "values": ["Sí", "No"],                          "best": 0 },
    { "key": "responseTime", "label": "Responde",            "values": ["El mismo día", "En 24 h"],           "best": 0 },
    { "key": "warranty",     "label": "Garantía declarada",  "values": ["30 días en mano de obra", "—"],      "best": null },
    { "key": "payment",      "label": "Formas de pago",      "values": ["Efectivo, transferencia", "Efectivo"], "best": null },
    { "key": "phoneVerified","label": "Teléfono confirmado", "values": ["Sí", "Sí"],                          "best": null },
    { "key": "experience",   "label": "Experiencia",         "values": ["8 años", "15 años"],                 "best": 1 },
    { "key": "jobs",         "label": "Trabajos publicados", "values": ["18", "4"],                           "best": 0 }
  ]
}
```

| Campo | Tipo | Regla |
|---|---|---|
| `providers[]` | array | **Exactamente 2**, en el orden pedido |
| `rows[].values` | array\<string> | Ya formateados para la UI (`es-MX`, `priceTxt`, `kmTxt`, `status`) |
| `rows[].best` | int32 \| null | Índice del mejor valor, o `null` cuando **no hay mejor objetivo** |

`best: null` en atención, garantía, pago y teléfono confirmado es deliberado: no son mejores ni
peores, son distintos, y marcar un ganador ahí sería inventar una recomendación.

**No se devuelve nunca**: teléfono, punto exacto de prestadores a domicilio, ni estadísticas privadas.
Se aplican las mismas exclusiones de [`GET /providers/{id}`](get-providers-id.md).

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `compare.need_two_ids` | `ids` no trae exactamente 2, o trae el mismo dos veces |
| 404 | `provider.not_found` | Alguno no existe o está despublicado. **No se dice cuál**: la respuesta es la misma para los dos casos |
| 400 | `validation_failed` | uuid mal formado, `lat`/`lng` fuera de rango |
| 429 | `rate.limit_exceeded` | Política `user` |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `provider_profiles` | `find({ _id: { $in: [id1, id2] }, published: true })` — **una sola consulta**, no dos | `_id` |

- Una sola consulta con `$in`: dos viajes a la base para una pantalla que siempre pide dos perfiles
  sería desperdicio.
- La proyección excluye `location` por cada documento según su `attention`, igual que en el perfil
  público.
- Si el resultado trae menos de 2 documentos → `404`.
- `rating`, `recs` y `jobs` ya están desnormalizados en la colección: no se llama a Engagement.

## 6. Caché y rate limit

- **L2 (Redis) 60 s**, clave `cmp:{id1}:{id2}` con los ids **ordenados** para que A-B y B-A compartan
  entrada. Las filas que dependen de `lat`/`lng` (distancia) se componen fuera de la caché.
- Invalida con los tags `provider:{id1}` y `provider:{id2}`.
- Rate limit **`user`** (120/min).
- `Cache-Control: private, max-age=60`.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **MongoDB** | Lectura con `$in` y proyección condicional |
| **Redis** | Caché L2 de la tabla |
| **Front Door CDN** | Fotos |
| `CultureInfo("es-MX")` | Formato de moneda de `minPrice` y `visit` |

## 8. Seguridad y privacidad

1. Se exige JWT porque comparar es una de las acciones que la maqueta pone detrás del **gate de
   cuenta** (junto con contactar, guardar y recomendar).
2. El tope de **2** lo impone el servidor; no se confía en que la app respete `S.cmp`.
3. Mismas exclusiones de privacidad que el perfil público: **la comparación no es una vía lateral**
   para obtener el teléfono o la ubicación exacta (constitución del front §10 lo pide explícitamente).
4. `404` sin decir qué id falló: no se confirma la existencia de perfiles ajenos.
5. `best: null` donde no hay criterio objetivo: la plataforma registra señales, no emite juicios
   profesionales (constitución §2).
