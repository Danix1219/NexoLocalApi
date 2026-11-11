# Catálogo de categorías · `GET /v1/discovery/categories`

> Refinamiento ejecutable 2026-11-10: [contrato y l?mites del incremento](implementacion-directorio.md) prevalece sobre los ejemplos hist?ricos de índices/caché/hora fija y especialidades de esta ficha. Autoridad de arquitectura: nv-06.

| Campo | Valor |
|---|---|
| Módulo | Discovery |
| Quién | **P** (público, sin token) |
| Pantalla de la maqueta | Chips de categoría en 7.6 Buscar, 7.10 Publicar y paso `services` del registro · `CATS`, `SPEC` |
| Spec SDD | nv-03 |
| Idempotente | Sí (lectura) |

## 1. Función

Devuelve las **7 categorías** con sus especialidades, colores e iconos. Es el catálogo que la app
guarda en local (drift) y refresca por ETag; alimenta los chips de búsqueda, el selector de categoría
al publicar y la inferencia de categorías del registro de negocios.

Las 7 categorías de la maqueta: `plomeria`, `electricidad`, `carpinteria`, `pintura`, `jardineria`,
`cerrajeria`, `otros`. **Cinco especialidades por categoría.**

## 2. Petición

```http
GET /v1/discovery/categories
If-None-Match: "7c1f9a2b3d4e5f60"
```

Sin parámetros.

## 3. Respuesta

`200 OK` (o `304`)

```json
{
  "categories": [
    {
      "slug": "plomeria",
      "name": "Plomería",
      "icon": "drop",
      "gradient": { "from": "#2E8B8B", "to": "#17695D" },
      "specialties": [
        { "slug": "fugas", "name": "Fugas" },
        { "slug": "calentadores", "name": "Calentadores" },
        { "slug": "tuberias", "name": "Tuberías" },
        { "slug": "banos", "name": "Baños" },
        { "slug": "bombas", "name": "Bombas de agua" }
      ],
      "businessLabel": "Plomería",
      "tradeLabel": "Plomero"
    }
  ],
  "version": "2026-10-20"
}
```

| Campo | Tipo | Tamaño | Regla |
|---|---|---|---|
| `slug` | string | **20** | Identificador estable. **Nunca cambia**: se guarda en los documentos |
| `name` | string | **40** | Etiqueta en español |
| `icon` | string | **20** | Clave del diccionario `IC` de la maqueta |
| `gradient.from` / `.to` | string | **7** | Hex `#RRGGBB`; el degradado por categoría de la maqueta |
| `specialties[]` | array | **5** | |
| `businessLabel` | string | **40** | Etiqueta de giro (`GIRO`) |
| `tradeLabel` | string | **40** | Etiqueta de oficio (`OFIC`) |
| `version` | string | **10** | Fecha del catálogo; la app compara para decidir si refresca su copia local |

**No se devuelve**: las palabras clave (`keywords[]`) que usa `inferCats`. Se quedan en el servidor:
publicarlas facilitaría colocarse en una categoría que no corresponde.

## 4. Errores

| HTTP | `code` | Cuándo |
|---|---|---|
| 429 | `rate.limit_exceeded` | Política `public` |
| 503 | `server.dependency_unavailable` | Mongo caído y sin caché. Raro: el catálogo vive 24 h en Redis |

## 5. Datos

| Colección | Operación | Índice |
|---|---|---|
| `categories` | `find({})` ordenado por `order` | `_id` = slug |

Documento:

```js
{
  _id: "plomeria", name: "Plomería", icon: "drop", order: 1,
  gradient: { from: "#2E8B8B", to: "#17695D" },
  keywords: ["fuga", "tuberia", "plomer", "calentador", "wc", "lavabo"],   // privado
  specialties: [{ slug: "fugas", name: "Fugas" }, …],
  businessLabel: "Plomería", tradeLabel: "Plomero"
}
```

Es **datos de semilla**: los crea `db/nexo-mongo-init.js` y solo cambian por despliegue, no por API.
No hay endpoint de escritura.

## 6. Caché y rate limit

- **L1 1 h / L2 (Redis) 24 h**, clave `cat:all:v1`.
- **Output cache 1 h** + `ETag`. `Cache-Control: public, max-age=3600`.
- Invalida con el tag `categories`, solo en un despliegue que cambie la semilla.
- Rate limit **`public`** (60/min por IP).

## 7. Tecnologías

| Pieza | Para qué |
|---|---|
| **Redis** | Caché L2 de 24 h |
| **HybridCache** | L1 + output cache |
| **MongoDB** | Colección de semilla |
| **drift** (app) | Copia local; la app funciona sin conexión con el último catálogo |

## 8. Seguridad y privacidad

1. Endpoint **público sin token**: el visitante necesita los chips para explorar.
2. Las `keywords` de inferencia **no se publican**: son la heurística que decide en qué categoría cae
   un negocio, y conocerlas permitiría manipularla.
3. Sin datos personales, por lo que la caché puede ser `public` y compartida.
4. Los `slug` son parte del contrato: cambiar uno rompería los documentos existentes, así que un
   cambio de catálogo exige migración y ADR.
