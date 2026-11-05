# Spec — CD-01 · Despliegue continuo a Azure (Linux dockerizado)

Versión: 0.1.0 | Fecha: 2026-11-05 | Estado: Borrador documental | Rama prevista: cd-01

## 1. Objetivo

Publicar cada servicio del backend como contenedor Linux en **Azure Container Apps**, construido y desplegado por GitHub Actions desde `main`, sin secretos versionados y sin cambiar la arquitectura interna de los servicios (hexagonal/slices/módulos según `scaffold.json`). El despliegue es infraestructura: no introduce lógica de negocio ni endpoints nuevos.

## 2. Alcance / No-alcance

**Alcance.** Imagen por servicio desde el `Dockerfile` ya existente en `services/<svc>/src/Api/Dockerfile`; registro en Azure Container Registry (ACR); una Container App por servicio; identidad federada (OIDC) GitHub→Azure; configuración por variables de entorno y secrets de la Container App; verificación post-despliegue contra `/health/ready`; contrato navegable en `/swagger`.

**No-alcance.** Terraform/Bicep (el alta de recursos es runbook manual una vez, §7); dominio propio y certificados; Front Door/WAF; autoescalado afinado; base de datos gestionada (Mongo Atlas sigue siendo externo); despliegue de S02–S08 mientras no tengan operaciones; blue/green o canary (Container Apps ya hace revisiones y traspaso de tráfico por defecto al 100 % de la última).

## 3. Contratos API

Ninguno nuevo. Las operaciones expuestas son las de `specs/endpoints/ep-01..ep-07`. El despliegue solo añade superficie de infraestructura, no de negocio:

| Ruta | Origen | Uso |
|---|---|---|
| `GET /health/ready` | `Program.cs` (health check de Mongo) | sonda de readiness de la Container App y verificación del workflow |
| `GET /swagger` y `/swagger/v1/swagger.json` | Swashbuckle en la capa Api | contrato navegable para la app móvil y el expediente de evaluación |

## 4. Modelo de datos

Sin cambios. Cada servicio conserva su propia base (`Mongo:Database`); la cadena de conexión no se versiona y llega solo como secret de la Container App.

## 5. Seguridad

1. **Sin secretos en el repositorio ni en la imagen.** `appsettings.json` solo lleva valores no sensibles (`Mongo:Database`). `Mongo__ConnectionString` se inyecta como variable de entorno referenciada a un *secret* de la Container App (`secretref:mongo-connection`), cargado por runbook o desde Key Vault.
2. **Sin credenciales de larga vida en GitHub.** `azure/login` usa OIDC con una federated credential del App Registration; no hay `AZURE_CREDENTIALS` con secreto de cliente. El workflow pide `id-token: write` y `contents: read`, nada más.
3. **Secrets vs variables de GitHub.** Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `MONGO_CONNECTION_STRING` (solo si se automatiza la carga). Variables: `AZURE_ACR_NAME`, `AZURE_RESOURCE_GROUP`. Ningún valor se imprime en los logs.
4. **Entorno protegido.** El job de despliegue corre bajo el environment `produccion`, con revisores requeridos; `main` protegido exige el job de build previo.
5. **Contenedor.** Imagen base `mcr.microsoft.com/dotnet/aspnet:10.0` (Linux), usuario no root (`USER $APP_UID`), sin SDK en la capa final, puerto 8080 sin TLS propio (TLS lo termina el ingress de Container Apps).
6. **Arquitectura limpia preservada.** El despliegue no añade referencias entre capas; `scripts/check_architecture.py` sigue siendo gate obligatorio antes de publicar.

## 6. Tests / Gates

| Gate | Comando | Evidencia |
|---|---|---|
| Build y fronteras | `.github/workflows/backend-base.yml` (restore, build Release, `check_architecture.py`, unittest, `Test-ScaffoldHosts.ps1`) | job `base` en verde |
| Imagen reproducible | `docker build --platform linux/amd64 -f services/<svc>/src/Api/Dockerfile .` | digest publicado en ACR |
| Arranque real | `GET /health/ready` = 200 sobre el FQDN de la revisión, hasta 5 min | paso *Verify revision health* |
| Contrato publicado | `GET /swagger/v1/swagger.json` = 200 | revisión manual tras el despliegue |

El smoke local (`Test-ScaffoldHosts.ps1`) usa una cadena Mongo ficticia de loopback con `serverSelectionTimeoutMS=500`: comprueba que el host arranca, nunca que hay base; no es evidencia de integración.

## 7. Plan de entrega

### 7.1 Alta de recursos en Azure (una sola vez, Azure CLI)

```bash
RG=rg-nexolocal
LOC=mexicocentral
ACR=acrnexolocal           # debe ser único en Azure
ENV=cae-nexolocal
APP=ca-nexolocal-identity

az group create -n $RG -l $LOC
az acr create -n $ACR -g $RG --sku Basic
az containerapp env create -n $ENV -g $RG -l $LOC

# Imagen inicial de arranque; la real la publica el workflow.
az containerapp create -n $APP -g $RG --environment $ENV \
  --image mcr.microsoft.com/k8se/quickstart:latest \
  --target-port 8080 --ingress external \
  --min-replicas 1 --max-replicas 3 --cpu 0.5 --memory 1Gi

# La cadena de Atlas entra como secret: nunca al repositorio ni a la imagen.
az containerapp secret set -n $APP -g $RG \
  --secrets mongo-connection="mongodb+srv://USUARIO:CLAVE@cluster.mongodb.net/?retryWrites=true&w=majority"

az containerapp update -n $APP -g $RG \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
                 Mongo__ConnectionString=secretref:mongo-connection

# Sondas: readiness contra el health check propio.
az containerapp update -n $APP -g $RG --set-env-vars ASPNETCORE_HTTP_PORTS=8080
```

Permitir la salida a Atlas: en Atlas, Network Access → añadir las IP salientes estáticas del entorno (`az containerapp env show -n $ENV -g $RG --query properties.staticIp`).

### 7.2 Identidad federada GitHub → Azure (sin secreto de cliente)

```bash
APPID=$(az ad app create --display-name gh-nexolocal-deploy --query appId -o tsv)
az ad sp create --id $APPID
SUB=$(az account show --query id -o tsv)
az role assignment create --assignee $APPID --role Contributor \
  --scope /subscriptions/$SUB/resourceGroups/$RG
az role assignment create --assignee $APPID --role AcrPush \
  --scope /subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.ContainerRegistry/registries/$ACR

# Una credencial federada por sujeto que vaya a desplegar.
az ad app federated-credential create --id $APPID --parameters '{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:Danix1219/NexoLocalApi:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
az ad app federated-credential create --id $APPID --parameters '{
  "name": "github-env-produccion",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:Danix1219/NexoLocalApi:environment:produccion",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

Permitir que la Container App lea del ACR con la misma identidad o con identidad administrada:
`az containerapp registry set -n $APP -g $RG --server $ACR.azurecr.io --identity system`
(previo `az containerapp identity assign -n $APP -g $RG --system-assigned` y rol `AcrPull`).

### 7.3 Configuración en GitHub

Settings → Environments → `produccion` (revisores requeridos) y Settings → Secrets and variables → Actions:

| Clave | Tipo | Valor |
|---|---|---|
| `AZURE_CLIENT_ID` | secret | `$APPID` |
| `AZURE_TENANT_ID` | secret | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | secret | `$SUB` |
| `AZURE_ACR_NAME` | variable | `acrnexolocal` |
| `AZURE_RESOURCE_GROUP` | variable | `rg-nexolocal` |

### 7.4 Pipeline

`.github/workflows/deploy-azure.yml`: `push` a `main` o manual → job `base` (reutiliza `backend-base.yml`) → por servicio: login OIDC, `az acr login`, `docker build --platform linux/amd64` + push etiquetado con el SHA, `az containerapp update --image` con `--revision-suffix`, y espera a `200` en `/health/ready`. Añadir un servicio es añadir una entrada a la matriz.

Reversión: `az containerapp revision list -n $APP -g $RG -o table` y `az containerapp ingress traffic set -n $APP -g $RG --revision-weight <revision-anterior>=100`.

## Aprobación

Estado: Borrador documental; infraestructura descrita y pipeline presente, sin evidencia de despliegue real.
- [ ] Revisor humano identificado — nombre y fecha pendientes.
- [ ] CHECKLIST_PR.md completado con evidencia propia.
- [ ] Sin conflicto con constitución y requisitos del proyecto.
