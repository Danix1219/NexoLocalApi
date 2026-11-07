# Correccion de la base: solucion original y microservicios

> **Documento historico.** Describe la estructura de ocho microservicios que la **enmienda E-001** retiro el 2026-11-06. Ya no refleja lo que hay en disco: no existen `nexolocal.Api.slnx`, `services/` ni los hosts por servicio. Se conserva como evidencia del trabajo ejecutado y de por que se cambio. **Estado vigente:** [estructura-backend.md](../estructura-backend.md) y [mapa de specs](../../specs/arquitectura/README.md).


Fecha: 2026-11-03. El usuario corrige la creacion de una solucion paralela y confirma **Gateway G01; los microservicios tendran sus propios hosts**. Se reutiliza el proyecto nexolocal.Api existente y la unica solucion activa es nexolocal.Api.slnx. NexoLocal.Backend.slnx se retiro.

## Estructura aplicada

- G01: nexolocal.Api/nexolocal.Api.csproj, nombre/ruta del proyecto original conservados. Se agrupa como Gateway dentro de la solucion, sin referencias a proyectos de negocio de servicios.
- S01: services/identity/src/Domain, Application, Infrastructure y Api; el host es NexoLocal.Identity.Api. Las capas pertenecen a Identity, no son los microservicios de todo Nexo.
- Resto de dueños: services/<servicio>/src/... al implementar su incremento; no servicios vacios ni modelos globales de todos los dominios.
- Solucion, workflow, scripts, README/AGENTS y rutas SDD ajustados. Estructura de referencia previa: gateway y services/<servicio>/src/{Domain,Application,Infrastructure,Api}; el host existente conserva su ruta para respetar la base del usuario.

Se intento mover la carpeta original a gateway/nexolocal.Api, pero el IDE la mantenia bloqueada. No se forzo el cierre de Visual Studio, no se copio otro host y la carpeta quedo en su ruta original. La agrupacion Gateway en la solucion y la responsabilidad G01 hacen explicita la arquitectura sin un traslado forzado.

## Cambios sobre la plantilla

WeatherForecast y el controller se retiraron del proyecto activo; copias textuales y snapshots del Program/csproj/Dockerfile/HTTP/perfiles originales quedan en docs/reference-template. Se conservaron el nombre de proyecto, target .NET, identificador de user-secrets, appsettings y perfiles/puertos; se desactivo abrir navegador automaticamente hacia el ejemplo. Los paquetes de plantilla OpenAPI y tooling de contenedores no usados por el host vacio se retiraron; no se instalo ningun proveedor.

Program de G01 solo arranca el host, sin endpoints ni proxy. Program de S01 hace lo mismo de forma independiente. YARP y sus rutas siguen por implementar/ratificar; llamar Gateway al host no acredita un proxy operativo. Los accesos a datos y reglas se implementaran en su propietario, nunca como libreria de todos los servicios dentro de G01.

## Validacion ejecutada

| Comprobacion | Resultado | Limite |
|---|---|---|
| Restore nexolocal.Api.slnx --force | Correcto, cinco proyectos, feed local vacio/packs SDK | Sin descarga de paquetes de aplicacion |
| Build Release --no-restore | Correcto, 0 errores y 0 advertencias | Base sin negocio |
| check_architecture.py | Correcto, G01 sin referencias S01 y capas propias | Verificacion de referencias, no semantica completa |
| Pruebas del verificador | 8 tests OK | Incluye rechazo G01→Application de Identity; no unitarias de negocio |
| Publish de G01 | artifacts/publish/gateway/nexolocal.Api.dll | Publicacion LOCAL de archivos, no GitHub ni despliegue |
| Publish de S01 | artifacts/publish/identity/NexoLocal.Identity.Api.dll | Host/artefacto separado de G01 |
| Aislamiento del artefacto G01 | No incluye NexoLocal.Identity*.dll | No prueba aislamiento operativo de bases todavia inexistentes |
| Arranque simultaneo de ambos hosts | Correcto, procesos y puertos loopback distintos | GET / da 404 en cada uno; no proxy o negocio configurado |
| Limpieza de smoke | Ambos procesos propios detenidos | No se detuvieron procesos del usuario/IDE |

Existen Dockerfiles separados de G01 y S01 con contexto raiz, restores/publicaciones y entrypoints propios. Se prepararon rutas coherentes, pero NO se construyeron imagenes ni se ejecutaron contenedores. SDK/image/runtime seleccionados para la base requieren su ensayo de entrega; no hay nube/proveedores/CD ni aceptacion productiva.

## Uso correcto

Abrir nexolocal.Api.slnx en la carpeta nexolocal.Api. Desde su raiz:

```powershell
dotnet restore nexolocal.Api.slnx
dotnet build nexolocal.Api.slnx --no-restore --configuration Release
python -B scripts/check_architecture.py
python -B -m unittest discover -s tests/Architecture -p "test_*.py"
```

Hosts independientes:

```powershell
# G01, proyecto original
dotnet run --project nexolocal.Api/nexolocal.Api.csproj --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5187
# S01, otra terminal
dotnet run --project services/identity/src/Api/NexoLocal.Identity.Api.csproj --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5188
```

## Conciliacion y pendientes

[Mapa vigente](../estructura-backend.md) y [orden SDD](../orden-desarrollo-backend.md). Se corrigieron rutas de las 26 specs a services/<dueño>/src y a G01 existente, sin cambiar los 101 endpoints/propietarios ni requisitos de negocio. Las pruebas previas de cuatro proyectos/otra solucion quedan como historia en el informe anterior, no instrucciones actuales.

El codigo permanece en la ubicacion local elegida, sin segundo .git y sin consolidacion/publicacion en el checkout API_NexoLocal. Jira/Confluence siguen pospuestos. Ready funcional, contratos, proveedores y transicion de CI/guard/controles remotos siguen pendientes; no se marcan tareas o revisiones como aceptadas por estas comprobaciones de base. El workflow local ya apunta a la solucion original, pero no se ejecuto en GitHub por este cambio.
