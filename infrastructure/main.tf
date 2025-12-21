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

variable "azure_openai_api_key" {
  type        = string
  sensitive   = true
  default     = ""
  description = "Azure OpenAI API key. If empty, will use the key from AI Foundry account."
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

# Random string cho Key Vault name (phải unique toàn Azure)
resource "random_string" "kv_unique" {
  length  = 6
  special = false
  upper   = false
}

# App Service Plan (Linux, free tier) cho API .NET
resource "azurerm_service_plan" "plan" {
  name                = "asp-google-searching"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "F1"
}

# Azure Key Vault để lưu trữ secrets
resource "azurerm_key_vault" "kv" {
  name                = "kv-gs-${random_string.kv_unique.result}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  # Cho phép truy cập từ App Service qua Managed Identity
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete",
      "Recover",
      "Backup",
      "Restore"
    ]
  }

  # Network ACLs - cho phép truy cập từ mọi nơi (có thể hạn chế sau)
  network_acls {
    default_action = "Allow"
    bypass         = "AzureServices"
  }

  # Soft delete và purge protection (tùy chọn, có thể bật nếu cần)
  soft_delete_retention_days = 7
  purge_protection_enabled    = false
}

# Data source để lấy thông tin client config (tenant_id, object_id)
data "azurerm_client_config" "current" {}

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

# Key Vault Secret cho Google Maps API Key
resource "azurerm_key_vault_secret" "google_maps_key" {
  name         = "GoogleMapsApi--ApiKey"
  value        = var.google_maps_api_key
  key_vault_id = azurerm_key_vault.kv.id

  depends_on = [
    azurerm_key_vault.kv
  ]
}

# Key Vault Secret cho Azure OpenAI API Key
# Sử dụng key từ variable nếu có, nếu không thì dùng key từ AI Foundry account
resource "azurerm_key_vault_secret" "openai_key" {
  name         = "AzureOpenAI--ApiKey"
  value        = var.azure_openai_api_key != "" ? var.azure_openai_api_key : data.external.cognitive_keys.result.key1
  key_vault_id = azurerm_key_vault.kv.id

  depends_on = [
    azurerm_key_vault.kv,
    data.external.cognitive_keys
  ]
}

# Key Vault Secret cho Azure OpenAI Endpoint (không phải secret nhưng để quản lý tập trung)
resource "azurerm_key_vault_secret" "openai_endpoint" {
  name         = "AzureOpenAI--Endpoint"
  value        = data.azurerm_cognitive_account.ai_foundry.endpoint
  key_vault_id = azurerm_key_vault.kv.id

  depends_on = [
    azurerm_key_vault.kv,
    data.azurerm_cognitive_account.ai_foundry
  ]
}

# Web App chạy API .NET 8
resource "azurerm_linux_web_app" "api" {
  name                = "api-googlesearching-${random_id.server.hex}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.plan.id

  # Bật Managed Identity để App Service có thể truy cập Key Vault
  identity {
    type = "SystemAssigned"
  }

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

  # Sử dụng Key Vault Reference thay vì giá trị trực tiếp
  app_settings = {
    "GoogleMapsApi__ApiKey"        = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.google_maps_key.versionless_id})"
    "AzureOpenAI__Endpoint"       = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.openai_endpoint.versionless_id})"
    "AzureOpenAI__ApiKey"         = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.openai_key.versionless_id})"
    "AzureOpenAI__DeploymentName" = azapi_resource.model_deployment.name
    "AzureOpenAI__ApiVersion"     = var.openai_api_version
  }

  depends_on = [
    azurerm_key_vault_secret.google_maps_key,
    azurerm_key_vault_secret.openai_key,
    azurerm_key_vault_secret.openai_endpoint
  ]
}

# Phân quyền cho App Service (Managed Identity) truy cập Key Vault
# LƯU Ý: Chạy terraform apply 2 LẦN:
#   Lần 1: Tạo identity cho App Service (role assignment này sẽ bị skip)
#   Lần 2: Tạo role assignment này sau khi identity đã có
resource "azurerm_role_assignment" "kv_secrets_user" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_web_app.api.identity[0].principal_id

  depends_on = [
    azurerm_linux_web_app.api,
    azurerm_key_vault.kv
  ]
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

output "key_vault_name" {
  description = "Tên của Azure Key Vault"
  value       = azurerm_key_vault.kv.name
}

output "key_vault_uri" {
  description = "URI của Azure Key Vault"
  value       = azurerm_key_vault.kv.vault_uri
}

