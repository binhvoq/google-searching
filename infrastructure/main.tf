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

# Resource Group chứa toàn bộ hạ tầng
resource "azurerm_resource_group" "rg" {
  name     = "rg-google-searching"
  location = "Southeast Asia"
}

# App Service Plan (Linux, free tier) cho API .NET
resource "azurerm_service_plan" "plan" {
  name                = "asp-google-searching"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "F1"
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
    # app_settings = {
    #   "GoogleMapsApiKey" = "YOUR_KEY_HERE"
    # }
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

# Random ID tránh trùng tên Web App toàn cầu
resource "random_id" "server" {
  byte_length = 4
}

# Outputs phục vụ cho cấu hình CI/CD
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

