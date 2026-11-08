# Infraestructura — nv-14-A

Fase 0 del [orden de trabajo](../specs/arquitectura/README.md): la plataforma se termina **al
principio**. Lo de aqui se aplica una vez y no se vuelve a tocar al acercarse la entrega.

## Que crea

| Recurso | Para que | Requisito |
|---|---|---|
| `azurerm_service_plan` **B1, `worker_count = 2`** | Balanceo entre dos instancias | 15 |
| `azurerm_linux_web_app` **api** | Rol `api`, `https_only`, TLS 1.2, health check a `/health/ready` | 13, 15 |
| `azurerm_linux_web_app` **worker** | Rol `worker` en el **mismo plan**, `always_on` | 15 |
| `azurerm_application_insights` + Log Analytics | Observabilidad | 15 |
| `azurerm_storage_account` **colas** + 2 colas | Outbox y cola de fallidos; nunca publico anonimo | 15 |
| `azurerm_storage_account` **medios** + contenedor privado | Imagenes; nunca publico anonimo | 15, 16 |
| Front Door Standard + ruta `/imagenes/*` | CDN **de Azure**: cumple el requisito 15 sin discusion | 15 |
| `azurerm_role_assignment` AcrPull | Las apps extraen del ACR con identidad administrada, sin contrasena | 7 |

**MongoDB Atlas no se crea aqui**: vive fuera de Azure y se administra en su propio panel. Su cadena
entra como variable sensible.

## Costo real, sin maquillaje

| Concepto | USD/mes |
|---|---|
| Plan B1, 2 instancias (~13 c/u) | **~26** |
| App worker (mismo plan) | 0 |
| Atlas M0 | 0 |
| Blob de imagenes (pocos GB) | ~1 |
| **Front Door Standard** | **~35** |
| Storage de colas | ~1 |
| Application Insights (bajo volumen) | ~0–3 |
| **Total** | **~63–66** |

> **Aviso de costo.** Front Door Standard (~35 USD/mes) es mas caro que el resto de la plataforma
> junta y consume el credito de Azure for Students (100 USD) en **mes y medio**, con la evaluacion
> el 2 de diciembre. Se eligio a proposito porque el requisito 15 exige el CDN *de la nube
> seleccionada*: Cloudflare R2 salia a 0 USD pero no es de Azure.
>
> **Mitigacion recomendada:** crear el perfil de Front Door **al final**, cerca de la evaluacion,
> y mientras tanto servir las imagenes desde Blob. El codigo no cambia: la base publica se inyecta
> por `Media__PublicBase`.

## Uso

```bash
cd infra/terraform
az login
cp terraform.tfvars.example terraform.tfvars    # rellenar prefijo, region y acr_name

# Los secretos NO van al archivo: se exportan.
export TF_VAR_mongo_connection_string='mongodb+srv://...'
export TF_VAR_redis_connection_string='...'

terraform init
terraform plan -out plan.tfplan                 # revisar ANTES de aplicar
terraform apply plan.tfplan
```

Las salidas dan los nombres que el workflow espera como variables de repositorio:
`AZURE_APP_PLAN`, `AZURE_WEBAPP_API`, `AZURE_WEBAPP_WORKER`.

### Ciclo crear / eliminar (AC-413)

```bash
terraform destroy    # elimina todo el grupo de recursos
```

`purge_soft_delete_on_destroy` permite recrear con el mismo nombre sin esperar el borrado suave.

## Seguridad

- **El `tfstate` guarda las cadenas de Atlas, Redis y la del storage de colas en texto plano.**
  Esta ignorado en `.gitignore`.
  Versionarlo publica los secretos. Para trabajo en equipo conviene un backend remoto con cifrado.
- Las apps extraen la imagen del ACR con **identidad administrada**: no hay usuario ni contrasena de
  registro en la configuracion.
- `https_only`, TLS minimo 1.2 y FTPS deshabilitado en las dos apps.
- El contenedor de imagenes es **privado**: se sirve por el CDN, no por URL anonima del blob.
- Las colas no son legibles de forma anonima.

## Pendiente

- **Sin aplicar.** Nada de esto existe todavia: no se ha ejecutado `terraform apply`.
- Decidir cuando crear Front Door, segun el aviso de costo de arriba.
- `terraform validate` no se ha corrido: la CLI de Terraform no esta instalada en el equipo de
  desarrollo. Hay que validarlo antes de aplicar.
