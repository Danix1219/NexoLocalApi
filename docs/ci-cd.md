# CI/CD

## Stack detectado

| | |
|---|---|
| Lenguaje | C# 14 sobre **.NET 10** (`global.json` fija el SDK 10.0.403) |
| Framework | ASP.NET Core Minimal APIs, OpenIddict 7.8, MongoDB Driver 3.13, StackExchange.Redis |
| Dependencias | NuGet con versiones centralizadas en `Directory.Packages.props` |
| Solución | `src/Nexo.sln`, 28 proyectos: 20 de producto y 8 de pruebas |
| Pruebas | xUnit; las de integración levantan Mongo y Redis reales con Testcontainers |
| Despliegue | Azure App Service, plan B1 con dos instancias |

## Qué corre y cuándo

| Workflow | Se dispara con | Comprueba |
|---|---|---|
| `ci.yml` | PR a `develop`, `main` o `release/**`; push a `develop` | Formato, vulnerabilidades, compilación, fronteras de arquitectura y las 41 pruebas |
| `sdd.yml` | Cualquier push y cualquier PR | Documentación SDD: estructura, enlaces y trazabilidad. Arrastra el build como dependencia |
| `pr-governance.yml` | PR a `main`, `develop` o `release/**` | Trazabilidad del PR y dirección de rama |
| `deploy-azure.yml` | Push a `main` | Publica en App Service con OIDC |

`ci.yml` existe aparte de `sdd.yml` a propósito. El build real ya corría, pero escondido como
dependencia de un workflow llamado «SDD documentation»: quien mira la lista de checks de un PR no
tiene por qué saber eso. Ahora los checks de producto se llaman por lo que son.

### Validaciones de `ci.yml`

1. **Estilo** — `dotnet format --verify-no-changes` contra `.editorconfig`. No reformatea: falla y
   señala el archivo, para que el arreglo quede en el commit de quien lo rompió.
2. **Dependencias** — `dotnet list package --vulnerable --include-transitive`. Incluye las
   transitivas porque así llegó la última: Snappier dentro de MongoDB.Driver.
3. **Compilación** — `dotnet build --configuration Release`. Con `TreatWarningsAsErrors` y los
   analizadores de .NET activos, el build **ya es un linter**: una advertencia rompe la CI.
4. **Fronteras de arquitectura** — NetArchTest, con caso negativo: `Nexo.Fixtures.BadDiscovery`
   cruza un dominio a propósito y la prueba debe detectarlo.
5. **Pruebas** — las 41 de la solución, con Mongo y Redis reales.

## Variables y secretos

**El repositorio no contiene ningún secreto.** En local se leen de un `.env` que está en
`.gitignore`; en Azure, de los app settings del sitio. Ver [configuracion-secretos.md](configuracion-secretos.md).

### Lo que necesita GitHub

Solo el despliegue necesita credenciales. La CI de producto **no necesita ninguna**: las pruebas de
integración levantan sus propios contenedores.

| Dónde | Nombre | Para qué |
|---|---|---|
| Secret | `AZURE_CLIENT_ID` | OIDC: aplicación registrada en Entra ID |
| Secret | `AZURE_TENANT_ID` | OIDC |
| Secret | `AZURE_SUBSCRIPTION_ID` | OIDC |
| Variable | `AZURE_WEBAPP_NAME` | Nombre del App Service |
| Variable | `AZURE_PLAN_NAME` | Nombre del plan |
| Variable | `AZURE_RESOURCE_GROUP` | Grupo de recursos |

Es OIDC, no un perfil de publicación: GitHub pide un token de corta vida en cada ejecución y no hay
contraseña guardada que robar. Requiere una credencial federada en Entra ID apuntando a este
repositorio.

Las variables de la aplicación (`Mongo__*`, `Redis__*`, `Identity__*`, `Mail__*`) **no** viven en
GitHub: se cargan una sola vez en los app settings del App Service.

## Bloquear el merge si la CI falla

Los checks no bloquean nada por sí solos: hay que exigirlos en la protección de rama. En GitHub:

**Settings → Branches → Add branch ruleset** (o *Add rule* si usás la pantalla clásica).

1. **Branch name pattern**: `develop`. Repetir después para `main`.
2. Marcar **Require a pull request before merging**.
   - *Required approvals*: **1**. Sin esto, cualquiera mergea su propio PR.
   - Marcar **Dismiss stale pull request approvals when new commits are pushed**: una aprobación
     no debe cubrir código que nadie miró.
3. Marcar **Require status checks to pass before merging**.
   - Marcar **Require branches to be up to date before merging**. Sin esto, dos PR que pasan por
     separado pueden romper `develop` al juntarse.
   - En el buscador, añadir **exactamente** estos nombres:

     ```
     CI / estilo
     CI / dependencias
     CI / build y pruebas / Backend / build
     SDD / docs
     Git / flow
     PR / traceability
     ```

     El tercero lleva dos nombres encadenados porque es un workflow reutilizable: GitHub une el
     del trabajo que llama con el del trabajo llamado.

   - Los nombres solo aparecen en el buscador **después** de que el check haya corrido al menos una
     vez en el repositorio. Si no los ves, abrí un PR de prueba, esperá a que terminen y volvé.
4. Marcar **Do not allow bypassing the above settings**. Si los administradores pueden saltarse la
   regla, la regla es una sugerencia.
5. Marcar **Block force pushes**.

Con eso, el botón de merge queda deshabilitado mientras cualquiera de los checks esté en rojo o
pendiente.

### Comprobar que funciona

Abrí un PR con un fallo deliberado — por ejemplo, una línea mal indentada en cualquier `.cs` — y
verificá que `CI / estilo` se pone en rojo y que el botón de merge queda bloqueado. Después
revertilo. Una protección que nadie probó no es una protección.

## Limitación conocida

En un PR a `develop`, el build corre **dos veces**: una desde `ci.yml` y otra desde `sdd.yml`, que
lo arrastra como dependencia. No se quitó porque el verificador documental
(`.ci/verify-sdd.ps1`) exige que `sdd.yml` dependa del workflow de producto, justamente para que la
CI documental no pueda ponerse verde ignorando el código. Cuesta unos minutos de runner; quitarlo
costaría ese gate.
