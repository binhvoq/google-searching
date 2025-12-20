terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azapi = {
      source  = "azure/azapi"
      version = "~> 1.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
    external = {
      source  = "hashicorp/external"
      version = "~> 2.0"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

provider "azapi" {
  # Azure API provider được sử dụng cho các resource mới như AI Foundry
}

variable "google_maps_api_key" {
  type        = string
  sensitive   = true
  description = "Google Maps API key used by the backend."
}

variable "ai_foundry_location" {
  type        = string
  default     = "eastus"
  description = "Region for Azure AI Foundry (may differ from the RG location)."
}

variable "ai_foundry_name" {
  type        = string
  default     = ""
  description = "Tên của Azure AI Foundry resource (để trống sẽ tự động tạo)."
}

variable "model_name" {
  type        = string
  default     = "gpt-4o-mini"
  description = "Tên model để deploy (ví dụ: gpt-4o-mini)."
}

variable "model_version" {
  type        = string
  default     = "2024-07-18"
  description = "Phiên bản của model."
}

variable "deployment_sku" {
  type        = string
  default     = "GlobalStandard"
  description = "SKU cho deployment (GlobalStandard hoặc GlobalStandardV2)."
}

variable "deployment_capacity" {
  type        = number
  default     = 1
  description = "Capacity cho deployment (số lượng tokens/requests)."
}

variable "openai_api_version" {
  type        = string
  default     = "2024-02-15-preview"
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

# Random string cho AI Foundry name nếu không được chỉ định
resource "random_string" "ai_foundry_unique" {
  length      = 5
  min_numeric = 5
  numeric     = true
  special     = false
  lower       = true
  upper       = false
}

# App Service Plan (Linux, free tier) cho API .NET
resource "azurerm_service_plan" "plan" {
  name                = "asp-google-searching"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "F1"
}

# Azure AI Foundry (Cognitive Services Account với kind AIServices)
resource "azapi_resource" "ai_foundry" {
  type                      = "Microsoft.CognitiveServices/accounts@2025-06-01"
  name                      = var.ai_foundry_name != "" ? var.ai_foundry_name : "aifoundry${random_string.ai_foundry_unique.result}"
  parent_id                 = azurerm_resource_group.rg.id
  location                  = var.ai_foundry_location
  schema_validation_enabled = false

  body = jsonencode({
    kind = "AIServices"
    sku = {
      name = "S0"
    }
    identity = {
      type = "SystemAssigned"
    }
    properties = {
      disableLocalAuth      = false
      allowProjectManagement = true
      customSubDomainName   = var.ai_foundry_name != "" ? var.ai_foundry_name : "aifoundry${random_string.ai_foundry_unique.result}"
    }
  })

  lifecycle {
    ignore_changes = [
      body.properties.customSubDomainName
    ]
  }
}

# Deploy model GPT-4o-mini
resource "azapi_resource" "model_deployment" {
  type      = "Microsoft.CognitiveServices/accounts/deployments@2023-05-01"
  name      = var.model_name
  parent_id = azapi_resource.ai_foundry.id

  depends_on = [
    azapi_resource.ai_foundry
  ]

  body = jsonencode({
    sku = {
      name     = var.deployment_sku
      capacity = var.deployment_capacity
    }
    properties = {
      model = {
        format  = "OpenAI"
        name    = var.model_name
        version = var.model_version
      }
    }
  })

  lifecycle {
    create_before_destroy = true
  }
}

# Lấy thông tin account từ azurerm (để lấy endpoint)
data "azurerm_cognitive_account" "ai_foundry" {
  name                = azapi_resource.ai_foundry.name
  resource_group_name = azurerm_resource_group.rg.name
  
  depends_on = [
    azapi_resource.ai_foundry
  ]
}

# Lấy API keys bằng cách gọi Azure CLI qua external data source
data "external" "cognitive_keys" {
  program = ["bash", "-c", <<-EOT
    KEYS=$(az cognitiveservices account keys list \
      --name "${azapi_resource.ai_foundry.name}" \
      --resource-group "${azurerm_resource_group.rg.name}" \
      --output json 2>/dev/null)
    if [ $? -eq 0 ] && [ -n "$KEYS" ]; then
      KEY1=$(echo "$KEYS" | grep -o '"key1": "[^"]*"' | cut -d'"' -f4)
      KEY2=$(echo "$KEYS" | grep -o '"key2": "[^"]*"' | cut -d'"' -f4)
      echo "{\"key1\":\"$KEY1\",\"key2\":\"$KEY2\"}"
    else
      echo '{"key1":"","key2":""}'
    fi
  EOT
  ]
  
  depends_on = [
    azapi_resource.ai_foundry
  ]
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
    "AzureOpenAI__Endpoint"       = data.azurerm_cognitive_account.ai_foundry.endpoint
    "AzureOpenAI__ApiKey"         = data.external.cognitive_keys.result.key1
    "AzureOpenAI__DeploymentName" = azapi_resource.model_deployment.name
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

output "ai_foundry_name" {
  description = "Tên của Azure AI Foundry resource"
  value       = azapi_resource.ai_foundry.name
}

output "ai_foundry_id" {
  description = "ID của Azure AI Foundry resource"
  value       = azapi_resource.ai_foundry.id
}

output "openai_endpoint" {
  description = "Endpoint URL của Azure AI Foundry"
  value       = data.azurerm_cognitive_account.ai_foundry.endpoint
}

output "openai_deployment_name" {
  description = "Tên của model deployment"
  value       = azapi_resource.model_deployment.name
}

output "openai_api_version" {
  description = "Azure OpenAI REST API version"
  value       = var.openai_api_version
}

output "openai_api_key" {
  description = "API Key của Azure AI Foundry (SENSITIVE)"
  value       = data.external.cognitive_keys.result.key1
  sensitive   = true
}

