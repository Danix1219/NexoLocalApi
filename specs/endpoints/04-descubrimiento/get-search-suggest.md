# Autocompletado del buscador · `GET /v1/discovery/search/suggest`

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **P/C** (público) |
| Pantalla de la maqueta | 7.6 Buscar, sugerencias al escribir · `ACT.qsug` |
| Spec SDD | nv-04 |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve sugerencias mientras se escribe: especialidades, categorías y nombres de prestadores que
empiezan con el texto. Es una consulta muy frecuente y muy barata, por eso va a Atlas Search
`autocomplete` y a caché de 5 minutos.

## 2. Petición

```http
GET /v1/discovery/search/suggest?q=calen&lat=20.0539&lng=-99.3403&limit=8
```

| Parámetro | Tipo | Tamaño o rango | Requerido | Regla |
|---|---|---|---|---|
| `q` | string | **2–40** | sí | Menos de 2 caracteres → respuesta vacía, sin consultar |
| `lat` / `lng` | double | −90..90 / −180..180 | no | Si vienen, los nombres de prestador se limitan a los que atienden ese punto |
| `limit` | int32 | **1–10** | no | Por defecto **8** |

## 3. Respuesta

`200 OK`

```json
{
  "suggestions": [
    { "kind": "specialty", "slug": "calentadores", "label": "Calentadores", "category": "plomeria" },
    { "kind": "category",  "slug": "plomeria",     "label": "Plomería",     "category": "plomeria" },
    { "kind": "provider",  "slug": "019400d4-…",   "label": "Calentadores del Valle", "category": "plomeria" }
  ]
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `kind` | string | enum | `specialty` \| `category` \| `provider` |
| `slug` | string | **40** | Slug, o el `providerId` cuando `kind = provider` |
| `label` | string | **80** | Texto a mostrar |
| `category` | string | **20** | Categoría asociada; la app la usa para preseleccionar el chip |

Orden: primero `specialty`, luego `category`, luego `provider`. Las especialidades y categorías
pesan más porque llevan a un listado útil; un nombre de prestador lleva a un solo perfil.

**No se devuelve**: nada más. Ni distancia, ni foto, ni estado. Es una lista de texto.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 400 | `validation_failed` | `q` de más de 40 caracteres o `limit` mayor que 10 |
| 429 | `rate.limit_exceeded` | Política `public` |

Con `q` de menos de 2 caracteres se responde `200` con `suggestions: []`, no un error: es el estado
normal mientras alguien empieza a escribir.

## 5. Datos

| Fuente | Operación | Índice |
|---|---|---|
| `categories` | Coincidencia por prefijo sobre `name` y `specialties[].name`, **en memoria** desde la caché del catálogo | — |
| `provider_profiles` | Atlas Search `autocomplete` sobre `name` | **Atlas Search** `providers_search`, campo `name` con tipo `autocomplete` (`edgeGram`, min 2, max 15) |

Las especialidades y categorías **no consultan la base**: el catálogo entero son 7 categorías con 5
especialidades cada una, ya está en la caché de
[`GET /categories`](get-categories.md) y se filtra en memoria. Solo los nombres de prestador van a
Atlas Search.

`readPreference: secondaryPreferred`.

## 6. Caché y rate limit

- **L2 (Redis) 5 min**, clave `sugg:{q}:{geohash6}`. Las consultas de autocompletado se repiten mucho
  entre usuarios, así que el acierto de caché es alto.
- Invalida con el tag `provider:{id}` solo si cambió un nombre (poco frecuente) y con `categories`.
- Rate limit **`public`** (60/min por IP). La app aplica *debounce* de **300 ms** y no consulta con
  menos de 2 caracteres: sin eso, escribir "calentador" dispararía 10 peticiones.
- `Cache-Control: public, max-age=300`.

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Atlas Search** `autocomplete` | Prefijos sobre el nombre del prestador, con `edgeGram` |
| **Redis** | Caché L2 de 5 min |
| **HybridCache** | L1 del catálogo de categorías, que se filtra en memoria |

## 8. Seguridad y privacidad

1. La respuesta es solo texto público: nombres de perfiles publicados y etiquetas del catálogo.
2. No se sugieren **correos, teléfonos ni nombres de clientes**. Solo prestadores, que son perfiles
   públicos por definición.
3. Con `lat`/`lng` los nombres se limitan a quienes atienden ese punto: evita usar el autocompletado
   como un volcado alfabético del directorio completo.
4. El `q` acotado a 40 caracteres y el mínimo de 2 protegen el índice de prefijos de consultas
   degeneradas.
5. No se registra el texto buscado asociado al usuario; si se mide, es en agregado y sin `sub`.
