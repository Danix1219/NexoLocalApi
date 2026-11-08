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

variable "acr_name" {
  description = "Nombre del Azure Container Registry que publica la imagen del host."
  type        = string
}

variable "instancias" {
  description = "Instancias del plan. El requisito 15 exige al menos 2."
  type        = number
  default     = 2

  validation {
    condition     = var.instancias >= 2
    error_message = "El requisito 15 exige al menos 2 instancias: no se permite bajar de 2."
  }
}
