output "url_api" {
  description = "URL publica de la API. Aqui debe responder /health/ready con 200."
  value       = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "url_cdn_imagenes" {
  description = "Endpoint de Front Door para las imagenes."
  value       = "https://${azurerm_cdn_frontdoor_endpoint.medios.host_name}/imagenes"
}

output "instancias_del_plan" {
  description = "Debe ser 2 o mas para cumplir el requisito 15."
  value       = azurerm_service_plan.principal.worker_count
}

output "nombre_webapp_api" {
  description = "Se pasa al workflow como vars.AZURE_WEBAPP_API."
  value       = azurerm_linux_web_app.api.name
}

output "nombre_plan" {
  description = "Se pasa al workflow como vars.AZURE_APP_PLAN."
  value       = azurerm_service_plan.principal.name
}
