# Estructura backend sobre el proyecto nexolocal.Api

Actualizacion: 2026-11-03. Correccion solicitada por el usuario: conservar su solucion/proyecto existente y la arquitectura por microservicios. Decision explicita: **nexolocal.Api es G01 Gateway; los microservicios tienen hosts propios**. La solucion paralela NexoLocal.Backend.slnx generada anteriormente se retiro; no forma parte del trabajo actual.

Autoridades: [constitucion](../.specify/memory/constitution.md), [datos por propietario](modelo-datos.md), [orden SDD](orden-desarrollo-backend.md), [fundacion](../specs/nv-01/spec.md) y [calidad](../specs/nv-03/spec.md). La estructura anterior de referencia usa gateway y services/<servicio>/src/{Domain,Application,Infrastructure,Api}; se adapta preservando la ruta del host ya creado por el usuario, sin duplicarlo.

## Estructura fisica actual

En C:/Users/kevin/source/repos/nexolocal.Api:

```text
nexolocal.Api.slnx                         # UNICA solucion, la original
nexolocal.Api/                            # G01, proyecto existente y nombre conservado
  nexolocal.Api.csproj                    # host Gateway, no servicio de negocio universal
  Program.cs
  appsettings.json
  appsettings.Development.json
  Properties/launchSettings.json
  nexolocal.Api.http
  Dockerfile
services/
  identity/                              # S01: limite de microservicio
    src/
      Domain/NexoLocal.Identity.Domain.csproj
      Application/NexoLocal.Identity.Application.csproj
      Infrastructure/NexoLocal.Identity.Infrastructure.csproj
      Api/NexoLocal.Identity.Api.csproj   # host propio de Identity
      Api/Program.cs
      Api/Dockerfile
    README.md
scripts/check_architecture.py
tests/Architecture/test_project_references.py
.github/workflows/backend-base.yml
docs/reference-template/                # copia textual del ejemplo anterior
AGENTS.md / CLAUDE.md / README.md
global.json / Directory.Build.props / .editorconfig / .gitignore / .dockerignore
```

La solucion agrupa el proyecto original bajo Gateway y S01 bajo Services/Identity. El host G01 conserva la ruta nexolocal.Api/ en lugar de forzar mover una carpeta bloqueada por el IDE; no se cerro Visual Studio ni se creo otro proyecto Gateway. La separacion arquitectonica no depende de renombrar la carpeta original.

Hay cinco proyectos .NET, pero dos hosts: G01 y S01.Api. Domain/Application/Infrastructure son capas internas de S01; **no son microservicios ni sustituyen su separacion**. Los hosts se publican/ejecutan por separado; una solucion comun no los convierte en monolito.

## Responsabilidad y referencias

| Limite/proyecto | Funcion | Referencias de proyecto |
|---|---|---|
| G01 nexolocal.Api | Entrada HTTP/WS y futura composicion/proxy | Ninguna a proyectos de negocio de servicios |
| S01 Domain | Reglas/estados/invariantes propios | Ninguna |
| S01 Application | Casos/puertos/autorizacion del caso | Domain de S01 |
| S01 Infrastructure | Adaptadores propios de datos/proveedores | Application/Domain de S01 |
| S01 Api | HTTP/DTO/composicion de S01 | Application/Infrastructure de S01 |

Gateway no concentra identidades, pedidos o acceso a todas las bases. Futura comunicacion Gateway→servicios por contrato HTTP/WS, y entre dueños por contratos/eventos versionados; no referencias a sus DLL ni acceso a sus bases. Cada servicio escribe solo sus datos. Flutter consume la entrada/API autorizada, nunca MongoDB/Neo4j.

Ambos hosts estan vacios y sin rutas de negocio/proxy. Se retiraron WeatherForecast y sus paquetes de ejemplo del proyecto activo, conservando referencia textual. YARP/persistencia/Redis/eventos/SMS no se instalan ni se dan por ratificados con esta correccion. Un host denominado Gateway no acredita proxy funcionando hasta implementar y probar su incremento.

## Servicios previstos: se agregan por incremento

| Codigo | Carpeta futura o actual | Dueño y arranque |
|---|---|---|
| G01 | nexolocal.Api/ existente | Gateway; nv-01 y composicion pertinente |
| S01 | services/identity/ actual | Identidad nv-04, cuenta nv-16, coordinacion borrado nv-12 |
| S02 | services/directory/ futura | Directorio/catalogo/busqueda nv-17/nv-06/nv-07 |
| S03 | services/community/ futura | Comunidad/experiencias/guardados nv-08/nv-19/nv-18 |
| S04 | services/orders/ futura | Solicitudes/intereses/pedidos nv-09/nv-20/nv-10 |
| S05 | services/messaging/ futura | Chat nv-11 y politicas pertinentes |
| S06 | services/trust/ futura | Moderacion/medios/admin nv-05/nv-24 |
| S07 | services/notifications/ futura | Dispositivos/push nv-21 y cierre nv-12 |
| S08 | services/relations/ condicional | Proyeccion publica nv-15; sin PII ni autoridad de permisos |

Cada nuevo dueño usa services/<nombre>/src/Api como host propio y sus capas necesarias; tests del dueño en services/<nombre>/tests/ cuando haya historias reales. No generar ocho servicios o entidades vacios para aparentar una arquitectura distribuida. No duplicar servicio por cada una de las 26 specs. Contratos API siguen en el SDD como autoridad hasta una migracion expresa, no crear otra copia divergente en cada host.

## Codigo y SDD/Git

La ubicacion fisica actual del codigo es la solicitada por el usuario. API_NexoLocal sigue siendo el checkout Git con constitucion/specs/contratos; la consolidacion revisada codigo/SDD permanece pendiente, sin segundo .git ni publicacion. Al consolidar: una sola solucion original, conservar historial/controles y mover el conjunto coherente de hosts/referencias/CI/Docker, sin copiar .git o sobrescribir cambios.

CI local preparada compila nexolocal.Api.slnx y verifica arquitectura. Dockerfiles de cada host tienen contexto raiz y publish/entrypoint propios; no se declara imagen construida ni deploy. La CI documental del checkout sigue rechazando csproj hasta reemplazar ese bloqueo por gates de producto reales en el mismo cambio. Datos Jira/Confluence siguen pospuestos; no se inventan para publicar.

## Orden de arranque

Seguir [paquete nv-00](inicio-backend/README.md) y [orden SDD](orden-desarrollo-backend.md): gobierno/decisiones → fundacion y contrato de la historia → gates reales → primera funcionalidad Identity. El usuario autorizo esqueleto fisico sin negocio; eso no firma Ready o ratificacion funcional. nv-01 posee estructura/referencias, nv-02 contratos, nv-03 CI, nv-04 identidad. G01 se mantiene separado de S01 desde el arranque.

Comandos y resultados actuales: [correccion y evidencia](inicio-backend/correccion-base-microservicios.md). El [informe anterior](inicio-backend/base-fisica.md) se conserva como historia del cambio descartado, no instruccion para abrir otra solucion.

## Perfiles por propietario

El [catálogo de arquitectura](arquitectura/README.md) define estructura, patrones, puertos, consistencia y fallos de G01 y S01–S08. Aplicar sus políticas comunes al refinar cada historia; no crea proyectos, ratifica proveedores ni reemplaza contratos.

## Ampliación física vigente (2026-11-03)

Por encargo explícito posterior, la solución original incorpora la base de S01–S08, además de G01, y Worker S07. [Inventario y evidencia de la ampliación](inicio-backend/base-todos-microservicios.md). Son 33 proyectos; patrones de negocio y proveedores pendientes. Esta ampliación reemplaza el límite previo de crear únicamente G01/S01; S08 continúa condicional.

## Estructura vigente revisada por caso de uso

La [selección de arquitecturas](arquitectura/seleccion-arquitecturas.md) y los perfiles sustituyen las rutas comunes Domain/Application/Infrastructure de los apartados anteriores donde no aplican. Solución original con 28 proyectos: S01/S04 conservan cuatro capas; otros usan núcleos por funcionalidad, módulo, procesamiento o proyección y Adapters. Los nombres históricos describen el scaffold previo; no son instrucciones de crear capas adicionales.
