# ---------------------------------------------------------------------------
# Imagenes: Azure Blob + Front Door (requisito 15: el CDN de la nube elegida)
#
# Costo: Front Door Standard ~35 USD/mes. Se asume a proposito para no incumplir
# el requisito 15, que pide el CDN del proveedor seleccionado. Ver infra/README.md.
# ---------------------------------------------------------------------------

resource "azurerm_storage_account" "medios" {
  name                = "st${var.prefijo}medios"
  location            = azurerm_resource_group.principal.location
  resource_group_name = azurerm_resource_group.principal.name

  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"

  https_traffic_only_enabled = true
  min_tls_version            = "TLS1_2"

  # Las imagenes se sirven por el CDN; el contenedor nunca se expone como publico anonimo.
  allow_nested_items_to_be_public = false

  tags = local.etiquetas
}

resource "azurerm_storage_container" "imagenes" {
  name                  = "imagenes"
  storage_account_id    = azurerm_storage_account.medios.id
  container_access_type = "private"
}

resource "azurerm_cdn_frontdoor_profile" "principal" {
  name                = "afd-${var.prefijo}"
  resource_group_name = azurerm_resource_group.principal.name
  sku_name            = "Standard_AzureFrontDoor"
  tags                = local.etiquetas
}

resource "azurerm_cdn_frontdoor_endpoint" "medios" {
  name                     = "fde-${var.prefijo}-medios"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.principal.id
  tags                     = local.etiquetas
}

resource "azurerm_cdn_frontdoor_origin_group" "medios" {
  name                     = "og-medios"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.principal.id

  load_balancing {
    sample_size                 = 4
    successful_samples_required = 3
  }

  health_probe {
    path                = "/"
    protocol            = "Https"
    request_type        = "HEAD"
    interval_in_seconds = 120
  }
}

resource "azurerm_cdn_frontdoor_origin" "medios" {
  name                          = "origen-blob"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.medios.id

  host_name                      = azurerm_storage_account.medios.primary_blob_host
  origin_host_header             = azurerm_storage_account.medios.primary_blob_host
  http_port                      = 80
  https_port                     = 443
  priority                       = 1
  weight                         = 1
  certificate_name_check_enabled = true
  enabled                        = true
}

resource "azurerm_cdn_frontdoor_route" "medios" {
  name                          = "ruta-imagenes"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.medios.id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.medios.id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.medios.id]

  supported_protocols    = ["Https"]
  patterns_to_match      = ["/imagenes/*"]
  forwarding_protocol    = "HttpsOnly"
  https_redirect_enabled = true
  link_to_default_domain = true

  cache {
    # Las URL llevan hash de contenido: una imagen publicada nunca cambia bajo la misma URL.
    compression_enabled       = true
    content_types_to_compress = ["image/svg+xml", "application/json"]
  }
}
