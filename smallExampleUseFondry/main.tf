# Tạo random string để đảm bảo tên resource là unique
resource "random_string" "unique" {
  length      = 5
  min_numeric = 5
  numeric     = true
  special     = false
  lower       = true
  upper       = false
}

# Sử dụng Resource Group có sẵn
data "azurerm_resource_group" "main" {
  name = var.resource_group_name
}

# Tạo Azure AI Foundry (Cognitive Services Account với kind AIServices)
resource "azapi_resource" "ai_foundry" {
  type                      = "Microsoft.CognitiveServices/accounts@2025-06-01"
  name                      = var.ai_foundry_name != "" ? var.ai_foundry_name : "aifoundry${random_string.unique.result}"
  parent_id                 = data.azurerm_resource_group.main.id
  location                  = var.location
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
      customSubDomainName   = var.ai_foundry_name != "" ? var.ai_foundry_name : "aifoundry${random_string.unique.result}"
    }
  })

  tags = var.tags

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
  resource_group_name = data.azurerm_resource_group.main.name
  
  depends_on = [
    azapi_resource.ai_foundry
  ]
}

# Lấy API keys bằng cách gọi Azure CLI qua external data source
# Note: External data source cần tất cả values là string (không phải nested object)
data "external" "cognitive_keys" {
  program = ["bash", "-c", <<-EOT
    KEYS=$(az cognitiveservices account keys list \
      --name "${azapi_resource.ai_foundry.name}" \
      --resource-group "${data.azurerm_resource_group.main.name}" \
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

