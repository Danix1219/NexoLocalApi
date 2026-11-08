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
| `pipeline.yml` | PR a `develop`, `main` o `release/**`; push a `develop` | Formato, vulnerabilidades, Terraform, compilación, fronteras de arquitectura y las 41 pruebas |
| `sdd.yml` | Cualquier push y cualquier PR | Documentación SDD: estructura, enlaces y trazabilidad. Arrastra el build como dependencia |
| `pr-governance.yml` | PR a `main`, `develop` o `release/**` | Trazabilidad del PR y dirección de rama |
| `deploy-azure.yml` | Push a `main` | Publica en App Service con OIDC |

`pipeline.yml` existe aparte de `sdd.yml` a propósito. El build real ya corría, pero escondido como
dependencia de un workflow llamado «SDD documentation»: quien mira la lista de checks de un PR no
tiene por qué saber eso. Ahora los checks de producto se llaman por lo que son.

### Etapas de `pipeline.yml`

Un trabajo por etapa, encadenados con `needs:`, para que la pestana Actions muestre el arbol
completo de una corrida en vez de una caja unica con todo dentro.

```
1 · Secretos (gitleaks)
2 · Compilacion y estilo
      |-- 3 · Dependencias (NuGet)
      |-- 3 · Infraestructura (Terraform)
      |-- 4 · Fronteras de arquitectura
      |-- 4 · Pruebas unitarias
      |-- 4 · Pruebas de integracion
                   `-- 5 · Puerta de calidad
```

| Etapa | Que comprueba | Bloquea |
|---|---|---|
| 1 · Secretos | gitleaks sobre **todo el historial**, no solo el arbol de trabajo | si |
| 2 · Compilacion y estilo | `dotnet build` en Release y `dotnet format --verify-no-changes` | si |
| 3 · Dependencias | Paquetes con vulnerabilidades conocidas, transitivas incluidas | si |
| 3 · Infraestructura | `terraform fmt -check` y `validate` sobre `infra/terraform` | si |
| 4 · Fronteras de arquitectura | NetArchTest, con su caso negativo | si |
| 4 · Pruebas unitarias | Las cuatro suites sin infraestructura | si |
| 4 · Pruebas de integracion | Mongo y Redis reales por Testcontainers | si |
| 5 · Puerta de calidad | Que ninguna etapa anterior quedara sin aprobar | si |

La puerta final existe porque un `needs` cancelado o saltado **no es un aprobado**: sin ella, una
etapa que no llega a correr deja el arbol en verde sin que nadie haya comprobado nada.

Con `TreatWarningsAsErrors` y los analizadores de .NET activos, el build **ya es un linter**: una
advertencia rompe la CI.

`terraform init` va con `-backend=false`: descarga los proveedores sin tocar el estado remoto, de
modo que la etapa no necesita credenciales de Azure ni puede modificar nada.

### Sin acciones de terceros

La politica del repositorio solo admite acciones propias o de GitHub. Lo que no viene en el runner
se resuelve sin accion: gitleaks corre como imagen de Docker y Terraform se baja con su version y
su huella SHA-256 clavadas en el propio paso.

### Falsos positivos de gitleaks

`.gitleaks.toml` mantiene enteras las reglas por omision y anota las excepciones una por una, con
su motivo: los marcadores de `.env.example`, la contrasena y la llave de las pruebas, la sonda que
genera su contrasena en cada ejecucion, y los `Idempotency-Key` de ejemplo de las fichas, que son
UUID que el cliente inventa por peticion y no autentican nada.

Nada se silencia en bloque: un escaner que grita en falso acaba ignorandose, y entonces deja de
servir el dia que acierta.

### Resultado de las pruebas

`dotnet test` escribe un `.trx` y `.ci/resumen-pruebas.py` lo convierte en el resumen que GitHub
muestra al abrir la ejecución: una fila por grupo con su marca, y el mensaje completo de cada
prueba que falló. Sin eso había que abrir el job y buscar el número en el log.

Corre también cuando las pruebas fallan (`if: always()`), que es justo cuando hace falta leerlo.
No usa ninguna acción de terceros: es un lector de XML de 80 líneas.

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
     5 · Puerta de calidad
     SDD / docs
     Git / flow
     ```

     Basta la puerta de calidad: ya depende de las siete etapas anteriores y falla si cualquiera
     de ellas no aprobo. Exigir cada una por separado seria la misma regla escrita siete veces, y
     habria que tocar la proteccion de rama cada vez que se anada una etapa.

     `PR / traceability` queda fuera a proposito mientras no existan Jira ni Confluence.

   - Los nombres solo aparecen en el buscador **después** de que el check haya corrido al menos una
     vez en el repositorio. Si no los ves, abrí un PR de prueba, esperá a que terminen y volvé.
4. Marcar **Do not allow bypassing the above settings**. Si los administradores pueden saltarse la
   regla, la regla es una sugerencia.
5. Marcar **Block force pushes**.

Con eso, el botón de merge queda deshabilitado mientras cualquiera de los checks esté en rojo o
pendiente.

### Comprobar que funciona

Abrí un PR con un fallo deliberado — por ejemplo, una línea mal indentada en cualquier `.cs` — y
verificá que `2 · Compilación y estilo` se pone en rojo, que `5 · Puerta de calidad` lo refleja y que el botón de merge queda bloqueado. Después
revertilo. Una protección que nadie probó no es una protección.

## Limitación conocida

En un PR a `develop`, el build corre **dos veces**: una desde `pipeline.yml` y otra desde `sdd.yml`, que
lo arrastra como dependencia. No se quitó porque el verificador documental
(`.ci/verify-sdd.ps1`) exige que `sdd.yml` dependa del workflow de producto, justamente para que la
CI documental no pueda ponerse verde ignorando el código. Cuesta unos minutos de runner; quitarlo
costaría ese gate.
