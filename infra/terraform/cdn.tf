# ---------------------------------------------------------------------------
# Imagenes: Azure Blob + Front Door (requisito 15: el CDN de la nube elegida)
#
# Costo: Front Door Standard ~35 USD/mes. Se asume a proposito para no incumplir
# el requisito 15, que pide el CDN del proveedor seleccionado. Ver infra/README.md.
# ---------------------------------------------------------------------------

resource "azurerm_storage_account" "medios" {
  count               = var.habilitar_cdn ? 1 : 0
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
  count                 = var.habilitar_cdn ? 1 : 0
  name                  = "imagenes"
  storage_account_id    = azurerm_storage_account.medios[0].id
  container_access_type = "private"
}

resource "azurerm_cdn_frontdoor_profile" "principal" {
  count               = var.habilitar_cdn ? 1 : 0
  name                = "afd-${var.prefijo}"
  resource_group_name = azurerm_resource_group.principal.name
  sku_name            = "Standard_AzureFrontDoor"
  tags                = local.etiquetas
}

resource "azurerm_cdn_frontdoor_endpoint" "medios" {
  count                    = var.habilitar_cdn ? 1 : 0
  name                     = "fde-${var.prefijo}-medios"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.principal[0].id
  tags                     = local.etiquetas
}

resource "azurerm_cdn_frontdoor_origin_group" "medios" {
  count                    = var.habilitar_cdn ? 1 : 0
  name                     = "og-medios"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.principal[0].id

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
  count                         = var.habilitar_cdn ? 1 : 0
  name                          = "origen-blob"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.medios[0].id

  host_name                      = azurerm_storage_account.medios[0].primary_blob_host
  origin_host_header             = azurerm_storage_account.medios[0].primary_blob_host
  http_port                      = 80
  https_port                     = 443
  priority                       = 1
  weight                         = 1
  certificate_name_check_enabled = true
  enabled                        = true
}

resource "azurerm_cdn_frontdoor_route" "medios" {
  count                         = var.habilitar_cdn ? 1 : 0
  name                          = "ruta-imagenes"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.medios[0].id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.medios[0].id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.medios[0].id]

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
