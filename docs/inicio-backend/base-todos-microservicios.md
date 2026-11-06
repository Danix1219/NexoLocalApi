# Base física de todos los microservicios — registro previo de 33 proyectos

> **Documento historico.** Describe la estructura de ocho microservicios que la **enmienda E-001** retiro el 2026-11-06. Ya no refleja lo que hay en disco: no existen `nexolocal.Api.slnx`, `services/` ni los hosts por servicio. Se conserva como evidencia del trabajo ejecutado y de por que se cambio. **Estado vigente:** [estructura-backend.md](../estructura-backend.md) y [mapa de specs](../../specs/arquitectura/README.md).


**Sustituido por la [revisión por caso de uso](../arquitectura/seleccion-arquitecturas.md): estado vigente 28 proyectos.** Este documento conserva evidencia ejecutada del scaffold anterior, sin atribuirla a la revisión posterior.

## Encargo y alcance vigente

El usuario encarga aplicar la base física de todos los microservicios después de definir sus perfiles. Este encargo amplía el alcance previo limitado a G01/S01: permite los proyectos y carpetas, sin implementar negocio ni seleccionar proveedores. No equivale a Ready funcional, aprobación humana o despliegue de S08.

Raíz física: `C:/Users/kevin/source/repos/nexolocal.Api`. Solución única: `nexolocal.Api.slnx`. G01 conserva `nexolocal.Api/nexolocal.Api.csproj`.

| Dueño | Carpeta | Proyectos |
|---|---|---|
| S01 | services/identity | Domain, Application, Infrastructure, Api |
| S02 | services/directory | Domain, Application, Infrastructure, Api |
| S03 | services/community | Domain, Application, Infrastructure, Api |
| S04 | services/orders | Domain, Application, Infrastructure, Api |
| S05 | services/messaging | Domain, Application, Infrastructure, Api |
| S06 | services/trust | Domain, Application, Infrastructure, Api |
| S07 | services/notifications | Domain, Application, Infrastructure, Api, Worker |
| S08 | services/relations | Application, Infrastructure, Api; condicional |

Total con G01: 33 proyectos; nueve hosts HTTP independientes y un Worker S07. S08 es una base de proyección sin Domain ficticio y sin activación funcional. Worker usa hosting del framework compartido instalado; no hay PackageReference nuevos.

## Estructura y patrones

Se reservan carpetas por capacidad según el [catálogo de perfiles](../arquitectura/README.md), puertos en Application y adaptadores propios en Infrastructure. Cada carpeta lleva README de responsabilidad y estado pendiente. Api y Worker componen únicamente sus dependencias. No hay ProjectReference entre propietarios.

Máquinas de estados, outbox/inbox, deduplicación, moderación, cache-aside y proyecciones tienen ubicación definida. Su comportamiento todavía no está implementado: requiere contrato, almacenamiento y pruebas específicas de la historia. No se agregan interfaces o implementaciones ficticias para aparentar funcionamiento.

`scaffold.json` de la raíz física registra proyectos/puertos; `scripts/check_architecture.py` verifica límites, rutas y cobertura de la solución; `tests/Architecture` contiene pruebas del verificador. `scripts/Test-ScaffoldHosts.ps1` comprueba arranque simultáneo de hosts vacíos y detiene solo sus procesos. Dockerfile independiente por host con contexto raíz; imágenes pendientes de ensayo.

La guía física `docs/base-microservicios.md` en esa raíz define comandos y limitaciones. La consolidación de código al checkout Git SDD y transición a CI real siguen pendientes; el workflow local está preparado para compilar/verificar todo, sin ejecución remota. Jira/Confluence permanecen pospuestos, sin publicación nueva.

## Evidencia de esta fase

Resultados se registran al ejecutar las comprobaciones. El lint SDD no acredita funcionalidad. Todas las revisiones humanas y aceptación funcional permanecen pendientes.

### Resultados ejecutados

- SDK local: 10.0.302. Restore de los 33 proyectos con feed local vacío y packs instalados: PASS. Comando: `dotnet restore nexolocal.Api.slnx --source artifacts/empty-feed --force --verbosity minimal -m:1 -nodeReuse:false`.
- Release: `dotnet build nexolocal.Api.slnx --no-restore --configuration Release -m:1 -nodeReuse:false --verbosity minimal`: PASS, 0 errores/0 advertencias.
- Verificador de arquitectura: PASS; once pruebas de sus reglas: PASS. No son pruebas de negocio.
- Registro físico: 33 proyectos, diez recetas de host, nueve puertos HTTP únicos; ningún PackageReference; todos los orígenes COPY de Docker existen. Revisión estática, sin build de imágenes.
- `scripts/Test-ScaffoldHosts.ps1`: PASS; nueve hosts HTTP concurrentes con 404 esperado y Worker S07 activo. Los procesos propios se detuvieron en finally. Logs: `artifacts/smoke/ad1fae333c04409192db6c91388ca05e` en la raíz física; contiene solo tráfico de prueba sin datos personales.
- SDD: lint de 26 specs PASS; integridad 484 tareas/467 requisitos/322 criterios PASS; ocho pruebas del validador PASS. No aceptación de producto.

No ejecutados: tests de negocio/DB/broker/proveedores, imágenes Docker, despliegue, CI remoto ni aceptación humana. El código permanece en su ubicación física, separado del checkout Git SDD, sin crear otro repositorio ni publicar.
