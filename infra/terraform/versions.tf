# nv-14-A: infraestructura de Nexo Local en Azure.
# AC-413 pide acreditar nube, balanceo y TLS, y crear/eliminar infraestructura con
# procedimiento seguro. Terraform es la recomendacion del requisito, no una imposicion.

terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.14"
    }
  }
}

provider "azurerm" {
  features {
    key_vault {
      # El ciclo crear/eliminar del requisito 15 necesita poder purgar y recrear.
      purge_soft_delete_on_destroy = true
    }
  }
}
