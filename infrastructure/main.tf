terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

variable "google_maps_api_key" {
  type        = string
  sensitive   = true
  description = "Google Maps API key used by the backend."
}

variable "openai_location" {
  type        = string
  default     = "East US"
  description = "Region for Azure OpenAI (may differ from the RG location)."
}

variable "openai_sku_name" {
  type        = string
  default     = "S0"
  description = "SKU for Azure OpenAI cognitive account."
}

variable "openai_deployment_name" {
  type        = string
  default     = "gpt4omini"
  description = "Deployment name used by the app."
}

variable "openai_model_name" {
  type        = string
  default     = "gpt-4o-mini"
  description = "Azure OpenAI model name."
}

variable "openai_model_version" {
  type        = string
  default     = "2024-07-18"
  description = "Azure OpenAI model version (may vary by region/subscription)."
}

variable "openai_api_version" {
  type        = string
  default     = "2024-08-01-preview"
  description = "Azure OpenAI REST API version used by the backend."
}

# Resource Group chứa toàn bộ hạ tầng
resource "azurerm_resource_group" "rg" {
  name     = "rg-google-searching"
  location = "Southeast Asia"
}

# Random ID tránh trùng tên Web App/OpenAI toàn cầu
resource "random_id" "server" {
  byte_length = 4
}

# App Service Plan (Linux, free tier) cho API .NET
resource "azurerm_service_plan" "plan" {
  name                = "asp-google-searching"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "F1"
}

# Azure OpenAI (Azure AI Foundry / Azure OpenAI behind the scenes)
resource "azurerm_cognitive_account" "openai" {
  name                = "aoaigooglesearch${random_id.server.hex}"
  location            = var.openai_location
  resource_group_name = azurerm_resource_group.rg.name

  kind     = "OpenAI"
  sku_name = var.openai_sku_name
}

resource "azurerm_cognitive_deployment" "chat" {
  name                 = var.openai_deployment_name
  cognitive_account_id = azurerm_cognitive_account.openai.id

  model {
    format  = "OpenAI"
    name    = var.openai_model_name
    version = var.openai_model_version
  }

  scale {
    type     = "Standard"
    capacity = 1
  }
}

# Web App chạy API .NET 8
resource "azurerm_linux_web_app" "api" {
  name                = "api-googlesearching-${random_id.server.hex}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.plan.id

  site_config {
    # Tắt Always On vì gói Free (F1) không hỗ trợ
    always_on = false

    application_stack {
      dotnet_version = "8.0"
    }
    cors {
      allowed_origins = ["*"] # Khi có domain front-end, thay * bằng URL cụ thể
    }
  }

  app_settings = {
    "GoogleMapsApi__ApiKey"       = var.google_maps_api_key
    "AzureOpenAI__Endpoint"       = azurerm_cognitive_account.openai.endpoint
    "AzureOpenAI__ApiKey"         = azurerm_cognitive_account.openai.primary_access_key
    "AzureOpenAI__DeploymentName" = azurerm_cognitive_deployment.chat.name
    "AzureOpenAI__ApiVersion"     = var.openai_api_version
  }
}

# Static Web App để host front-end React (dùng resource mới, không deprecated)
resource "azurerm_static_web_app" "web" {
  name                = "web-googlesearching"
  resource_group_name = azurerm_resource_group.rg.name
  location            = "East Asia" # Static Web App hỗ trợ region này
  sku_tier            = "Free"
  sku_size            = "Free"
}

# Outputs phục vụ cấu hình CI/CD
output "api_endpoint" {
  value = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "api_webapp_name" {
  value = azurerm_linux_web_app.api.name
}

output "frontend_url" {
  value = "https://${azurerm_static_web_app.web.default_host_name}"
}

output "static_webapp_deployment_token" {
  value     = azurerm_static_web_app.web.api_key
  sensitive = true
}

output "openai_endpoint" {
  value = azurerm_cognitive_account.openai.endpoint
}

output "openai_deployment_name" {
  value = azurerm_cognitive_deployment.chat.name
}

output "openai_api_version" {
  value = var.openai_api_version
}

output "openai_api_key" {
  value     = azurerm_cognitive_account.openai.primary_access_key
  sensitive = true
}

