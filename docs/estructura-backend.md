# Estructura backend — API REST modular por dominios

Actualizacion: 2026-11-06. Vigente desde la **enmienda E-001** de la [constitucion](../.specify/memory/constitution.md) (§3 y §4).

**Que cambio.** La estructura anterior (`nexolocal.Api.slnx` como G01 Gateway, ocho microservicios en
`services/<servicio>/src/{Domain,Application,Infrastructure,Api}`, nueve hosts HTTP y ocho
arquitecturas internas distintas) **ya no existe en disco**. Se sustituyo por una sola solucion con
una sola arquitectura interna, desplegada como una app con dos instancias. El motivo esta en
[E-001](../.specify/memory/constitution.md) §11: ocho microservicios no caben en el plan B1
disponible. El registro de lo retirado esta en [decisions/registro.md](decisions/registro.md).

Los identificadores `S01`–`S08` **se conservan como mapa de dominios**, no como unidades
desplegables. Ver [arquitectura/servicios/](arquitectura/servicios/).

## Estructura fisica actual

```text
Directory.Build.props                  # net10.0, nullable, warnings como errores
Directory.Packages.props               # versiones centralizadas: ningun csproj fija versiones
global.json                            # SDK anclado
docker-compose.yml                     # MongoDB en replica + Redis para desarrollo
src/
  Nexo.sln                             # UNICA solucion
  BuildingBlocks/
    Nexo.Common/                       # Result<T>, PagedResult<T>, GeoPoint, ICurrentUser
                                       #   Mongo/  tres bases keyed + health check
                                       #   Redis/  cache, revocacion, rate limit, idempotencia, backplane
    Nexo.Outbox/                       # OutboxMessage, IOutboxService, OutboxDispatcher
  Domains/
    Identity/                          # S01
    Discovery/                         # S02 + parte de S03, S08
    Engagement/                        # S04, S05, S06, S07
      Nexo.<Dominio>.Contracts/        # lo unico que otro dominio puede referenciar
      Nexo.<Dominio>.Domain/
      Nexo.<Dominio>.Application/
      Nexo.<Dominio>.Infrastructure/
      Nexo.<Dominio>.Endpoints/
    Screens/                           # composicion de pantallas: Application + Endpoints
  Host/
    Nexo.Host/                         # UNICO host: Program.cs de dos roles, Dockerfile
tests/
  Nexo.Architecture.Tests/             # NetArchTest: fronteras entre dominios
  Fixtures/                            # Good/BadDiscovery: el caso negativo de AC-004
  Nexo.Common.Unit/  Nexo.Identity.Unit/  Nexo.Discovery.Unit/  Nexo.Engagement.Unit/
  Nexo.Integration/                    # Testcontainers: MongoDB + Redis
```

## Arquitectura interna unica

La misma en los tres dominios, en lugar de las ocho de `architecture-selection.json`
(archivado en [decisions/archivo/](decisions/archivo/)):

```text
Contracts <- Domain <- Application <- Infrastructure <- Endpoints
```

| Capa | Contenido | No puede |
|---|---|---|
| `Contracts` | DTOs e interfaces que otros dominios consumen | depender de nada |
| `Domain` | Agregados, value objects, eventos | referenciar infraestructura |
| `Application` | Casos de uso y permisos, interfaces de repositorio | conocer MongoDB |
| `Infrastructure` | MongoDB, Redis, colas, Blob, adaptadores externos | contener reglas de negocio |
| `Endpoints` | Minimal APIs, un archivo por caso de uso | contener logica |

Un dominio solo puede referenciar `*.Contracts` de otro. Lo comprueba
`tests/Nexo.Architecture.Tests`, **con caso negativo**: `Nexo.Fixtures.BadDiscovery` referencia
`Nexo.Discovery.Domain` a proposito y la prueba debe fallar ante esa referencia.

## Un solo proceso

`Nexo.Host` atiende el pipeline HTTP (endpoints de los cuatro modulos, rate limiting,
`/health/live` y `/health/ready`) y corre el `OutboxDispatcher` en el mismo proceso.

Antes habia dos roles por `NEXO_ROLE`. Se unificaron porque no hay forma comoda de arrancar dos
instancias del mismo proyecto en local y la separacion no se estaba usando.

Con dos instancias en el plan no hay trabajo duplicado: el outbox reclama cada mensaje con una
actualizacion condicional atomica y solo una instancia se lo lleva. El costo es que un envio SMTP
lento ocupa el mismo proceso que atiende peticiones.

## Datos

Un cluster M0, tres bases, inyectadas por clave (`NexoDatabaseKeys`). Ningun dominio recibe el
cliente global, asi que no puede escribir en la base de otro por descuido.

| Dominio | Base |
|---|---|
| Identity | `nexo_identity` |
| Discovery | `nexo_discovery` |
| Engagement | `nexo_engagement` |
| Screens | ninguna: solo compone lecturas de los otros modulos |

`MaxConnectionPoolSize` se configura en `Mongo:MaxPoolSize`. **Pendiente**: confirmar el limite real
de conexiones de M0 en el panel de Atlas antes de fijarlo (nv-00 T-008).

## Estado compartido entre las dos instancias

Con dos instancias, lo siguiente **no puede vivir en memoria**. Todo en Redis:

| Estado | Por que |
|---|---|
| Revocacion de sesion (`jti`) | Sin esto el logout no surte efecto en la otra instancia (requisito 10) |
| Cache L2 | Requisito 9 |
| Contadores exactos de rate limit | Los limites por dia deben ser exactos entre replicas |
| Backplane de tiempo real | Emisor y receptor pueden estar en instancias distintas |
| Idempotencia de POST | Un reintento puede caer en la otra instancia |

Los limites por minuto contra abuso si van en memoria por replica: toleran ser aproximados.

## Verificacion

```bash
dotnet build src/Nexo.sln                      # 0 advertencias, 0 errores
dotnet test  src/Nexo.sln                      # unitarias y arquitectura
docker compose up -d                           # MongoDB replica + Redis
```

Autoridades: [constitucion](../.specify/memory/constitution.md),
[mapa de specs](../specs/arquitectura/README.md),
[arquitectura base nv-00](../specs/arquitectura/nv-00/spec.md),
[datos por propietario](modelo-datos.md), [orden SDD](orden-desarrollo-backend.md),
[calidad](../specs/arquitectura/nv-03/spec.md).
