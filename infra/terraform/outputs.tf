output "url_api" {
  description = "URL publica de la API. Aqui debe responder /health/ready con 200."
  value       = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "url_cdn_imagenes" {
  description = "Endpoint de Front Door para las imagenes. Vacio si el CDN esta apagado."
  value       = var.habilitar_cdn ? "https://${azurerm_cdn_frontdoor_endpoint.medios[0].host_name}/imagenes" : ""
}

output "instancias_del_plan" {
  # El requisito 15 pide 2; la cuota de Azure for Students permite 1. Se expone para que la
  # desviacion se vea en cada ejecucion en vez de quedar escondida en el portal.
  description = "Instancias del plan. El requisito 15 pide 2 o mas."
  value       = azurerm_service_plan.principal.worker_count
}

output "nivel_del_plan" {
  description = "Nivel del plan. F1 es el entorno efimero gratuito; B1 el de produccion."
  value       = azurerm_service_plan.principal.sku_name
}

output "nombre_webapp_api" {
  description = "Nombre del App Service creado."
  value       = azurerm_linux_web_app.api.name
}

output "nombre_plan" {
  description = "Nombre del plan de App Service creado."
  value       = azurerm_service_plan.principal.name
}

output "grupo_de_recursos" {
  description = "Grupo que agrupa todo lo creado. Es lo que `destroy` elimina por completo."
  value       = azurerm_resource_group.principal.name
}
