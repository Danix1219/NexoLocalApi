# Infraestructura

El lineamiento 15 pide que **crear y eliminar la infraestructura sean un paso del proceso**, no una
sesión manual en el portal. Lo hace el workflow **Infraestructura**, que ejecuta Terraform desde
`infra/terraform/`.

## Qué crea

Un **entorno efímero**, separado de producción:

| | |
|---|---|
| Grupo de recursos | `rg-<prefijo>`, por omisión `rg-nexoefimero` |
| Plan de App Service | **F1**, gratuito |
| Instancias | 1 |
| App Service | Linux, .NET 10, por código |
| Observabilidad | Log Analytics y Application Insights |
| CDN y colas | Apagados por omisión |

**Producción no se toca desde aquí.** El sitio `NexoLocal-Back` y su grupo `Nexolocal` se
crearon a mano y siguen su propio camino; este workflow solo opera su propio grupo.

### Por qué F1 y no B1

B1 cuesta 13 USD al mes por instancia, y sobre todo **consume la cuota de VMs B1**, que en Azure
for Students está limitada a una y ya la gasta producción. Un entorno efímero en B1 no se podría
crear.

F1 es gratuito, no consume esa cuota y basta para demostrar el ciclo completo: crear, desplegar,
comprobar y eliminar. No admite `always_on` ni escalado, y eso está asumido.

## Requisito previo, una sola vez

Hace falta una identidad con permiso sobre la suscripción. El perfil de publicación **no sirve**:
solo autoriza subir archivos a un sitio, no crear recursos.

### 1. Aplicación en Entra ID

`portal.azure.com` → **Microsoft Entra ID** → **App registrations** → **New registration**.
Nombre: `github-nexolocal-infra`. Anotar **Application (client) ID** y **Directory (tenant) ID**.

### 2. Credencial federada

En esa app → **Certificates & secrets** → **Federated credentials** → **Add credential**:

- Escenario: **GitHub Actions deploying Azure resources**
- Organization: `Danix1219` · Repository: `NexoLocalApi`
- Entity type: **Environment** · Environment name: `infraestructura`

El tipo **Environment** no es opcional: el workflow declara `environment: infraestructura`, y eso
cambia el sujeto del token. Si aquí se elige *Branch*, el login falla con `AADSTS70021`.

### 3. Permiso sobre la suscripción

Terraform **crea su propio grupo de recursos**, así que el permiso no puede ser del grupo:

```bash
az role assignment create \
  --assignee <client-id> \
  --role Contributor \
  --scope /subscriptions/<subscription-id>
```

### 4. Estado remoto

Sin estado compartido, un `apply` y un `destroy` en ejecuciones distintas no se verían:

```bash
az group create --name rg-tfstate --location centralus
az storage account create --name sttfstatenexo --resource-group rg-tfstate \
  --sku Standard_LRS --allow-blob-public-access false
az storage container create --name tfstate --account-name sttfstatenexo --auth-mode login
```

El nombre de la cuenta debe ser único en todo Azure.

### 5. Secretos y variables en GitHub

**Settings → Secrets and variables → Actions**

| Tipo | Nombre |
|---|---|
| Secret | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` |
| Secret | `MONGO_CONNECTION_STRING`, `REDIS_CONNECTION_STRING` |
| Variable | `TFSTATE_RESOURCE_GROUP`, `TFSTATE_STORAGE_ACCOUNT` |

Las cadenas de Mongo y Redis se pasan a Terraform por `TF_VAR_*` y acaban como app settings del
sitio efímero. Nunca se versionan.

## Cómo se usa

**Actions → Infraestructura → Run workflow.**

| Acción | Qué hace |
|---|---|
| `plan` | Muestra lo que haría, sin tocar nada. Siempre primero |
| `apply` | Crea el entorno y publica su URL en el resumen |
| `destroy` | Elimina el grupo completo. **Exige escribir `destruir`** en la confirmación |

La confirmación se comprueba **antes de autenticar**: destruir por un clic distraído es el
accidente que esa casilla evita.

El campo `prefijo` permite varios entornos a la vez; cada uno guarda su estado en
`<prefijo>.tfstate`.

## Comprobar que funciona

1. `plan` — termina sin error y lista los recursos a crear.
2. `apply` — al terminar, el resumen trae la URL. `GET <url>/health/ready` debe responder 200,
   lo que acredita que la app levantó y alcanza Atlas y Redis.
3. `destroy` con `confirmacion: destruir` — el grupo desaparece del portal.
4. Comprobar que `Nexolocal` y `NexoLocal-Back` **siguen intactos**.

Ese ciclo es la evidencia del lineamiento 15 y conviene capturarlo.

## Lo que no cubre

**El requisito de dos instancias.** La cuota de Azure for Students limita las VMs B1 a una y no
admite solicitudes de aumento. La variable `instancias` tenía una validación que prohibía bajar de
2; se relajó a 1 porque hacía imposible ejecutar `apply`, y entonces el ciclo crear/eliminar no se
podía demostrar en absoluto. El valor por omisión sigue siendo 2 y la salida
`instancias_del_plan` lo expone en cada ejecución, para que la desviación se vea.

La arquitectura sí soporta varias instancias: todo el estado compartido vive en Redis, el outbox
reclama cada mensaje con una actualización condicional atómica y `RevocacionEntreInstanciasTests`
levanta dos hosts contra el mismo Redis para comprobarlo.

**El despliegue de la aplicación al entorno efímero.** Terraform crea el sitio vacío. Publicar
código en él exigiría su propio perfil de publicación; para demostrar el ciclo de infraestructura
basta con que el sitio responda.
