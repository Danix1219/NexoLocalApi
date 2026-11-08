# PLAN — nv-00

Spec: [spec.md](spec.md). Tareas: [task.md](task.md). Estado: diseño, sin implementación.

## 1. Contexto

Dejar construible la **API REST modular por dominios**: tres dominios de frontera estricta, un binario con dos roles, tres bases en un cluster M0 y dos instancias en un plan B1.

Dependencias: ninguna. Entrada: constitución §3, §4 y enmienda E-001.

## 2. Diseño técnico

### Estructura objetivo

```
src/
├── Nexo.sln
├── BuildingBlocks/
│   ├── Nexo.Common/              # Result<T>, PagedResult<T>, GeoPoint, ICurrentUser
│   └── Nexo.Outbox/              # OutboxMessage, IOutboxService, OutboxDispatcher
├── Domains/
│   ├── Identity/                 # ← conserva services/identity (FR-008)
│   │   ├── Nexo.Identity.Contracts/
│   │   ├── Nexo.Identity.Domain/
│   │   ├── Nexo.Identity.Application/
│   │   ├── Nexo.Identity.Infrastructure/
│   │   └── Nexo.Identity.Endpoints/
│   ├── Discovery/                # misma estructura de 5 capas
│   ├── Engagement/               # misma estructura de 5 capas
│   └── Screens/
│       ├── Nexo.Screens.Application/
│       └── Nexo.Screens.Endpoints/
├── Host/
│   └── Nexo.Host/                # Program.cs, appsettings, Dockerfile
└── tests/
    ├── Architecture/             # NetArchTest: fronteras entre dominios
    ├── Identity.Unit/            # ← conserva services/identity/tests
    ├── Discovery.Unit/
    ├── Engagement.Unit/
    └── Integration/              # Testcontainers: MongoDB + Redis
```

**Un solo `Nexo.Host`**, no uno por dominio. Cada `*.Endpoints` aporta su `MapGroup` y el host los monta:

```csharp
app.MapIdentityEndpoints();     // /v1/identity/*
app.MapDiscoveryEndpoints();    // /v1/discovery/*
app.MapEngagementEndpoints();   // /v1/engagement/*
app.MapScreensEndpoints();      // /v1/screens/*
```

Así cada API de dominio es un proyecto propio con sus rutas y sus contratos, y el despliegue es uno. Extraer un dominio a su propio host más adelante es mover el proyecto y añadir un `Program.cs`: **no se reescribe lógica**.

### Dos roles, un binario (FR-002)

```csharp
var role = builder.Configuration["NEXO_ROLE"] ?? "api";
if (role == "worker")
{
    builder.Services.AddHostedService<OutboxDispatcher>();
    builder.Services.AddHostedService<ImagePipeline>();
}
else
{
    // pipeline HTTP, endpoints, rate limiting, health
}
```

En B1 el worker se publica como **WebJob continuo en el mismo plan**: sin costo adicional (NFR-004).

### Arquitectura interna única (FR-001)

`Contracts ← Domain ← Application ← Infrastructure ← Endpoints`, igual en los tres dominios. Sustituye las 8 arquitecturas distintas de `docs/architecture-selection.json`, que se archiva.

| Capa | Contenido | No puede |
|---|---|---|
| `Contracts` | DTOs e interfaces que otros dominios consumen | depender de nada |
| `Domain` | Agregados, value objects, eventos | referenciar infraestructura |
| `Application` | Casos de uso y permisos, interfaces de repos | conocer MongoDB |
| `Infrastructure` | MongoDB, Redis, colas, Blob, adaptadores externos | contener reglas de negocio |
| `Endpoints` | Minimal APIs, un archivo por caso de uso | contener lógica |

La hexagonal de Identity **ya es esta forma**; el traslado conserva su código (FR-008).

### Datos (FR-005)

Un cluster M0, tres bases. Cada dominio recibe **su propio `IMongoDatabase`** inyectado; no existe un cliente global que permita escribir en otra base por descuido.

```csharp
services.AddKeyedSingleton<IMongoDatabase>("identity",   (sp,_) => client.GetDatabase("nexo_identity"));
services.AddKeyedSingleton<IMongoDatabase>("discovery",  (sp,_) => client.GetDatabase("nexo_discovery"));
services.AddKeyedSingleton<IMongoDatabase>("engagement", (sp,_) => client.GetDatabase("nexo_engagement"));
```

`maxPoolSize = 40` por instancia (2 instancias + worker ≈ 120 conexiones), por debajo del límite del cluster. **Verificar el límite real en el panel de Atlas antes de fijarlo**: las fuentes públicas dan 100 y 500 sin coincidir.

### Estado compartido entre instancias (FR-006, NFR-003)

Lo que **no** puede vivir en memoria, porque hay dos instancias:

| Estado | Dónde | Por qué |
|---|---|---|
| Revocación de sesión (`jti`) | Redis | Sin esto el logout no surte efecto en la otra instancia (requisito 10) |
| Caché L2 | Redis | Requisito 9: caché a nivel backend |
| Contadores exactos de rate limit | Redis | Límites de negocio por día deben ser exactos entre réplicas |
| Backplane de tiempo real | Redis pub/sub | Emisor y receptor pueden estar en instancias distintas |
| Idempotencia de POST | Redis | Un reintento puede caer en la otra instancia |

Los límites por minuto (abuso) sí van en memoria por réplica: toleran ser aproximados y no dependen de Redis.

### Qué se retira (FR-009)

| Elemento | Estado actual |
|---|---|
| `services/directory`, `messaging`, `orders`, `relations`, `trust` | 1 archivo de 4–5 líneas cada uno |
| `services/notifications` | 2 archivos, 10 líneas |
| `services/community` | 3 archivos, 363 líneas → se revisa qué migra a Discovery |
| ~20 de los 27 `.csproj` | Andamiaje sin contenido |
| `docs/architecture-selection.json` | Se archiva: definía 8 arquitecturas |
| `deploy-azure.yml` | Apunta a Container Apps por servicio; se reescribe para App Service |

`services/identity` **no se borra: se mueve** con su historial.

## 3. Pipeline / orden de ejecución

1. **T-001** inventario de lo retirado, antes de borrar nada.
2. **T-002/T-003** solución y proyectos vacíos que compilan.
3. **T-004/T-005** bloques comunes con sus pruebas.
4. **T-006** prueba de fronteras: **antes** de mover Identity, para que el movimiento la valide.
5. **T-007** host con los dos roles.
6. **T-008/T-009** bases y Redis.
7. **T-010** entorno local.
8. **T-011** traslado de Identity, con sus pruebas en verde.
9. **T-012** retirada de los 7 servicios y registro de decisión.
10. **T-013/T-014/T-015** seguridad, evidencia y configuración de despliegue.

[Gates backend](../../../docs/roles-y-gates.md) desde esta primera vertical, con comandos reales porque aquí **ya hay proyecto**.

## 4. Riesgos / Deuda

| Riesgo | Mitigación |
|---|---|
| **Cambios sin commitear de Johann** (`Program.cs`, `.csproj`, `RedisRateLimiting.cs` sin añadir) | **Bloqueante.** No iniciar T-011 hasta que estén commiteados; `RedisRateLimiting.cs` es trabajo aprovechable en FR-006 |
| Límite de conexiones de M0 sin confirmar | Verificar en el panel antes de fijar `maxPoolSize` (T-008) |
| La enmienda E-001 es propuesta, no ratificada | Revisión de Johann antes de Ready |
| `services/community` tiene 363 líneas reales | Revisar qué migra a Discovery antes de borrar (T-001) |
| B1 no tiene autoescalado | El requisito 15 pide *"al menos 2 instancias"*, no autoescalado. Escalado manual a 2 cumple |
| Atlas M0 puede pausarse por inactividad | Mantener actividad antes del 2 de diciembre; el requisito de evaluación en vivo lo exige |

## 5. Verificación

Comandos reales desde T-002:

```bash
dotnet build src/Nexo.sln
dotnet test src/Nexo.sln --filter "Category=Unit"
docker compose up -d                                   # MongoDB réplica + Redis
dotnet test src/Nexo.sln --filter "Category=Integration"
dotnet run --project src/Host/Nexo.Host                 # NEXO_ROLE=api
```

Pruebas por tipo:

| Tipo | Qué cubre |
|---|---|
| Unitaria | `Result<T>`, `PagedResult<T>`, idempotencia del despachador |
| Arquitectura | Fronteras entre dominios, **caso positivo y negativo** (AC-004) |
| Integración | Testcontainers con MongoDB y Redis; aislamiento de bases (AC-005) |
| Dos instancias | Revocación propagada entre réplicas (AC-006) — con dos contenedores en local |
| Degradación | `/health/ready` en 503 con Redis caído (AC-007) |

Evidencia con versión, entorno, resultado y revisor real. Esta spec es la primera con comandos ejecutables: `scripts/check_sdd.ps1` ya no basta.

## Integridad constitucional y orden de tareas

Cada FR tiene tarea y AC en la tabla de trazabilidad de [spec.md](spec.md), que es la autoridad de IDs. T-006 precede a T-011 porque la prueba de fronteras debe existir antes de mover código a ellas. T-001 precede a cualquier borrado. Ready depende de la revisión de Johann sobre E-001, no del código que esta spec habilita.

Tipo de evidencia: ejecutable desde T-002; documental solo para T-001, T-012 y T-015.
