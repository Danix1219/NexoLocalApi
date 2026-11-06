locals {
  etiquetas = {
    proyecto  = "nexo-local"
    entorno   = "produccion"
    gestionado = "terraform"
  }
}

resource "azurerm_resource_group" "principal" {
  name     = "rg-${var.prefijo}"
  location = var.region
  tags     = local.etiquetas
}

# ---------------------------------------------------------------------------
# Observabilidad (requisito 15: observabilidad)
# ---------------------------------------------------------------------------

resource "azurerm_log_analytics_workspace" "principal" {
  name                = "log-${var.prefijo}"
  location            = azurerm_resource_group.principal.location
  resource_group_name = azurerm_resource_group.principal.name
  sku                 = "PerGB2018"
  # El plan gratuito de Azure for Students no sobrevive a retencion larga.
  retention_in_days = 30
  tags              = local.etiquetas
}

resource "azurerm_application_insights" "principal" {
  name                = "appi-${var.prefijo}"
  location            = azurerm_resource_group.principal.location
  resource_group_name = azurerm_resource_group.principal.name
  workspace_id        = azurerm_log_analytics_workspace.principal.id
  application_type    = "web"
  tags                = local.etiquetas
}

# ---------------------------------------------------------------------------
# Plan B1 con DOS instancias (requisito 15)
# ---------------------------------------------------------------------------

resource "azurerm_service_plan" "principal" {
  name                = "plan-${var.prefijo}"
  location            = azurerm_resource_group.principal.location
  resource_group_name = azurerm_resource_group.principal.name
  os_type             = "Linux"
  sku_name            = "B1"

  # Balanceo entre replicas: es lo que obliga a que el estado compartido viva en Redis.
  worker_count = var.instancias

  tags = local.etiquetas
}

data "azurerm_container_registry" "principal" {
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.principal.name
}

locals {
  configuracion_comun = {
    "Mongo__ConnectionString"               = var.mongo_connection_string
    "Mongo__MaxPoolSize"                    = "40"
    "Redis__ConnectionString"               = var.redis_connection_string
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.principal.connection_string
    "DOCKER_REGISTRY_SERVER_URL"            = "https://${data.azurerm_container_registry.principal.login_server}"

    # Colas del outbox (constitucion §3).
    "Queues__ConnectionString" = azurerm_storage_account.colas.primary_connection_string

    # Imagenes en Azure Blob, servidas por Front Door (requisito 15: CDN de la nube elegida).
    "Media__Provider"         = "AzureBlob"
    "Media__ConnectionString" = azurerm_storage_account.medios.primary_connection_string
    "Media__Container"        = azurerm_storage_container.imagenes.name
    "Media__PublicBase"       = "https://${azurerm_cdn_frontdoor_endpoint.medios.host_name}/imagenes"
  }
}

resource "azurerm_linux_web_app" "api" {
  name                = "app-${var.prefijo}-api"
  location            = azurerm_resource_group.principal.location
  resource_group_name = azurerm_resource_group.principal.name
  service_plan_id     = azurerm_service_plan.principal.id

  # Requisito 13: TLS obligatorio. El puerto 80 redirige y no sirve contenido.
  https_only = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on              = true
    minimum_tls_version    = "1.2"
    ftps_state             = "Disabled"
    health_check_path      = "/health/ready"
    container_registry_use_managed_identity = true

    application_stack {
      docker_image_name   = "nexolocal-api:latest"
      docker_registry_url = "https://${data.azurerm_container_registry.principal.login_server}"
    }
  }

  app_settings = merge(local.configuracion_comun, {
    "WEBSITES_PORT" = "8080"
  })

  tags = local.etiquetas

  lifecycle {
    # La imagen la mueve el workflow en cada despliegue; Terraform no la revierte.
    ignore_changes = [site_config[0].application_stack[0].docker_image_name]
  }
}

# La app extrae del ACR con su identidad administrada: sin usuario ni contrasena.
resource "azurerm_role_assignment" "api_acr_pull" {
  scope                = data.azurerm_container_registry.principal.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_linux_web_app.api.identity[0].principal_id
}

