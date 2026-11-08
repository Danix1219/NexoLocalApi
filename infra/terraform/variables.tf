variable "prefijo" {
  description = "Prefijo de los recursos. Debe ser unico global para el storage y las web apps."
  type        = string
  default     = "nexolocal"

  validation {
    condition     = can(regex("^[a-z0-9]{3,11}$", var.prefijo))
    error_message = "Solo minusculas y digitos, 3 a 11 caracteres: el nombre del storage no admite mas."
  }
}

variable "region" {
  description = "Region de Azure. centralus y eastus2 son las que tienen cuota B1 para Azure for Students."
  type        = string
  default     = "centralus"
}

variable "mongo_connection_string" {
  description = "Cadena de MongoDB Atlas. Se pasa por TF_VAR_mongo_connection_string; nunca se versiona."
  type        = string
  sensitive   = true
}

variable "redis_connection_string" {
  description = "Cadena de Redis. Se pasa por TF_VAR_redis_connection_string; nunca se versiona."
  type        = string
  sensitive   = true
}

# El requisito 15 pide al menos 2 instancias. La cuota de Azure for Students limita las VMs B1 a
# 1 y no admite solicitudes de aumento, de modo que la validacion no puede exigir 2: haria
# imposible ejecutar `apply` y el ciclo crear/eliminar quedaria sin demostrar. La desviacion
# esta declarada en Memoria.md y en docs/infraestructura.md.
variable "instancias" {
  description = "Instancias del plan. El requisito 15 pide 2; la cuota de estudiante permite 1."
  type        = number
  default     = 2

  validation {
    condition     = var.instancias >= 1
    error_message = "El plan necesita al menos una instancia."
  }
}

variable "sku" {
  description = "Nivel del plan. F1 es gratuito y no consume cuota de VMs B1."
  type        = string
  default     = "B1"

  validation {
    condition     = contains(["F1", "B1", "B2", "S1"], var.sku)
    error_message = "Niveles admitidos: F1, B1, B2 o S1."
  }
}

variable "habilitar_cdn" {
  # Front Door y la cuenta de medios cuestan; el entorno efimero debe salir gratis.
  description = "Crea Front Door y el almacenamiento de imagenes."
  type        = bool
  default     = false
}

variable "habilitar_colas" {
  description = "Crea la cuenta de almacenamiento con las colas del outbox."
  type        = bool
  default     = false
}
