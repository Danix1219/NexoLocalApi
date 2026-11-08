locals {
  etiquetas = {
    proyecto   = "nexo-local"
    entorno    = "produccion"
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
  sku_name            = var.sku

  # Balanceo entre replicas: es lo que obliga a que el estado compartido viva en Redis.
  worker_count = var.instancias

  tags = local.etiquetas
}

locals {
  # Los recursos de pago se encienden por bandera: el entorno efimero debe salir gratis.
  configuracion_comun = merge(
    {
      "Mongo__ConnectionString"               = var.mongo_connection_string
      "Mongo__MaxPoolSize"                    = "40"
      "Redis__ConnectionString"               = var.redis_connection_string
      "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.principal.connection_string
    },
    var.habilitar_colas ? {
      # Colas del outbox (constitucion §3).
      "Queues__ConnectionString" = azurerm_storage_account.colas[0].primary_connection_string
    } : {},
    var.habilitar_cdn ? {
      # Imagenes en Azure Blob, servidas por Front Door (requisito 15: CDN de la nube elegida).
      "Media__Provider"         = "AzureBlob"
      "Media__ConnectionString" = azurerm_storage_account.medios[0].primary_connection_string
      "Media__Container"        = azurerm_storage_container.imagenes[0].name
      "Media__PublicBase"       = "https://${azurerm_cdn_frontdoor_endpoint.medios[0].host_name}/imagenes"
    } : {}
  )
}

# Se despliega por codigo, no por contenedor: es lo que hace el workflow de despliegue con el
# perfil de publicacion, y asi el entorno efimero se parece a produccion en vez de inventar otra
# forma de publicar. Tambien evita el coste y la configuracion de un registro de contenedores.
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
    # El plan gratuito no admite `always_on`; en B1 si, y evita que la app se duerma.
    always_on           = var.sku != "F1"
    minimum_tls_version = "1.2"
    ftps_state          = "Disabled"

    # Lo que consulta el balanceador para sacar de rotacion una instancia enferma.
    health_check_path = "/health/ready"

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = local.configuracion_comun

  tags = local.etiquetas
}
