# Informe historico de base fisica: propuesta sustituida

**Este informe conserva la fase anterior y sus resultados. El usuario rechazo la solucion paralela y corrigio la estructura. No seguir sus rutas/comandos como estado vigente.** Usar [correccion actual](correccion-base-microservicios.md) y [mapa vigente](../estructura-backend.md).

Fecha: 2026-11-03. Encargo del usuario: **crear carpetas y proyectos base, sin logica de negocio**. La base se creo en la ubicacion solicitada `C:/Users/kevin/source/repos/nexolocal.Api`. No es una aplicacion funcional de identidad, una release ni una ratificacion de proveedores.

## Abrir y trabajar

Abrir **NexoLocal.Backend.slnx** en Visual Studio, dentro de nexolocal.Api. La solucion antigua nexolocal.Api.slnx sigue asociada a WeatherForecast y se conserva aparte; no es la solucion nueva.

```text
nexolocal.Api/
  NexoLocal.Backend.slnx                 # NUEVA: solo cuatro proyectos Identity
  global.json                            # SDK 10.0.302, latestPatch, sin prerelease
  Directory.Build.props                 # nullable/implicit usings/build determinista
  .editorconfig
  .gitignore
  AGENTS.md / CLAUDE.md                  # entradas hacia la autoridad SDD
  README.md
  src/Services/Identity/
    NexoLocal.Identity.Domain/
      NexoLocal.Identity.Domain.csproj
      README.md
    NexoLocal.Identity.Application/
      NexoLocal.Identity.Application.csproj
      README.md
    NexoLocal.Identity.Infrastructure/
      NexoLocal.Identity.Infrastructure.csproj
      README.md
    NexoLocal.Identity.Api/
      NexoLocal.Identity.Api.csproj
      Program.cs                        # solo inicia host; ninguna ruta
      README.md
  scripts/check_architecture.py
  tests/Architecture/test_project_references.py
  .github/workflows/backend-base.yml    # preparado, no publicado ni ejecutado en remoto
  nexolocal.Api.slnx                     # ORIGINAL conservada
  nexolocal.Api/                         # ORIGINAL conservada con WeatherForecast
```

Los bin/obj generados por restauracion/compilacion son locales e ignorados. No hay Class1, entidades, contratos HTTP inventados, controladores de negocio, repositorios, librerias externas o carpetas vacias de ocho servicios. Los cuatro proyectos son compilables aunque las bibliotecas no contengan clases.

## Referencias implementadas

| Proyecto | Referencias de proyecto |
|---|---|
| Domain | Ninguna |
| Application | Domain |
| Infrastructure | Application y Domain |
| Api | Application e Infrastructure; esta ultima para futura composicion |

Los proyectos apuntan a net10.0 y no tienen PackageReference. No se agregan YARP, MongoDB, Redis, eventos, SMS, ML/IA ni contenedor de DI externo. Tampoco gateway o servicios S02-S08 antes de sus historias.

El verificador comprueba las referencias declaradas, presencia de proyectos habilitados y ausencia de una dependencia de paquete/framework no revisada en Domain. No es una auditoria de dependencias entre clases o de reglas funcionales. Al habilitar otro servicio se revisa su dueño y se amplia la politica en ese cambio; no permitir referencias de proyecto/datos entre servicios.

## Comprobaciones realmente ejecutadas

| Comprobacion | Resultado | Limite |
|---|---|---|
| SDK instalado | 10.0.302 disponible; fijado en global.json | No se instala otro SDK ni se afirma que sea la ultima version |
| Restore solucion | Correcto, cuatro proyectos | Feed local vacio y packs del SDK, sin descargar paquetes de aplicacion |
| Build Release --no-restore | Correcto, 0 errores y 0 advertencias | Solo solucion nueva; no se compilo la plantilla original |
| check_architecture.py | ARCHITECTURE-OK | Referencias de proyectos habilitados, no semantica de negocio |
| unittest tests/Architecture | 7 pruebas OK | Pruebas del verificador, no tests C# de negocio/integracion |
| Host Api por dotnet DLL | Inicio correcto en loopback | HTTP local, sin TLS/autenticacion/rutas de aplicacion |
| GET / del host vacio | 404 esperado | No es prueba de endpoint de negocio ni health |
| Fin de prueba local | Host detenido al terminar | No queda un servicio/deploy iniciado por esta entrega |
| Preservacion de plantilla | SHA-256 de archivos originales y slnx sin cambios | Se conserva para referencia, no se atribuye al nuevo producto |
| Git | Sin .git nuevo en nexolocal.Api | No se inicializa ni publica otro repositorio |

Comandos habituales desde la raiz nexolocal.Api:

```powershell
dotnet restore NexoLocal.Backend.slnx
dotnet build NexoLocal.Backend.slnx --no-restore --configuration Release
python -B scripts/check_architecture.py
python -B -m unittest discover -s tests/Architecture -p "test_*.py"
dotnet run --project src/Services/Identity/NexoLocal.Identity.Api/NexoLocal.Identity.Api.csproj --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5187
```

Para la comprobacion ejecutada, restore uso un directorio temporal vacio mediante --source y el arranque uso un puerto loopback libre. No se eligio un proveedor ni se desactivo permanentemente auditoria NuGet. Python 3.9 o posterior es necesario para el verificador.

## CI, Docker y consolidacion

Workflow local backend-base.yml: restore/build, grafo y pruebas del verificador; checkout y setup-dotnet oficiales fijados a SHA, token contents:read, sin credenciales persistentes. SHA de setup-dotnet v5 consultado al repositorio oficial. No se ejecuto en GitHub ni se modificaron rulesets; no se declara CI remoto verde.

No se genero un Dockerfile nuevo ni se construyo imagen: el Dockerfile original sigue apuntando a nexolocal.Api.csproj/WeatherForecast. Contenerizar el nuevo host queda pendiente de la tarea de consolidacion, con rutas de contexto/publish y version de imagen revisadas. El arranque CLI/Visual Studio ya es verificable sin Docker.

API_NexoLocal sigue siendo el checkout Git con SDD, historial y controles existentes. El codigo esta en la carpeta de implementacion elegida por el usuario; la consolidacion al checkout queda pendiente. No copiar .git ni publicar este directorio como un segundo repositorio. Cuando Jira/Confluence se proporcionen, incorporar codigo/solucion/workflow al checkout por el flujo acordado y reemplazar el guard documental por validacion REAL en ese mismo PR; no eliminarlo sin sustitucion. Mientras el codigo siga fuera, el PASS documental no es el CI de este producto.

## Estado de las tareas SDD

nv-01 tiene evidencia parcial de su scaffold/referencias, no consolidacion completa: origen preservado, cuatro proyectos fuera del checkout, Docker y revision pendientes. nv-03 tiene comandos locales y workflow preparado, no pipeline remoto ni transicion de guard aplicada. Ninguna tarea se marca completa, Ready funcional o aceptacion de identidad por estas pruebas.

D01 conserva YARP/persistencia/eventos/versiones de proveedores pendientes y las decisiones propias de la historia; el encargo autoriza esta base .NET conforme al target existente. nv-02 formaliza contratos y nv-04 implementa el primer caso solo con entradas y revision pertinentes. Los estados de gobierno/ratificacion previos no se reescriben como aprobados por compilar.
