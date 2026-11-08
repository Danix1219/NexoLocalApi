# ---------------------------------------------------------------------------
# Colas: Azure Storage Queues + patron outbox/inbox (constitucion §3)
#
# Esta cuenta NO sirve imagenes: esas viven en la cuenta "medios" detras del CDN (cdn.tf).
# Solo guarda las colas que consume el rol worker.
# ---------------------------------------------------------------------------

resource "azurerm_storage_account" "colas" {
  name                = "st${var.prefijo}colas"
  location            = azurerm_resource_group.principal.location
  resource_group_name = azurerm_resource_group.principal.name

  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"

  https_traffic_only_enabled = true
  min_tls_version            = "TLS1_2"

  # Una cola nunca debe ser legible de forma anonima.
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = true

  tags = local.etiquetas
}

resource "azurerm_storage_queue" "outbox" {
  name               = "outbox"
  storage_account_id = azurerm_storage_account.colas.id
}

# Mensajes que agotaron sus reintentos: se revisan a mano, no se pierden.
resource "azurerm_storage_queue" "outbox_fallidos" {
  name               = "outbox-fallidos"
  storage_account_id = azurerm_storage_account.colas.id
}
