output "resource_group_name" {
  description = "Tên của Resource Group"
  value       = data.azurerm_resource_group.main.name
}

output "ai_foundry_name" {
  description = "Tên của Azure AI Foundry resource"
  value       = azapi_resource.ai_foundry.name
}

output "ai_foundry_id" {
  description = "ID của Azure AI Foundry resource"
  value       = azapi_resource.ai_foundry.id
}

output "model_deployment_name" {
  description = "Tên của model deployment"
  value       = azapi_resource.model_deployment.name
}

output "model_deployment_id" {
  description = "ID của model deployment"
  value       = azapi_resource.model_deployment.id
}

output "endpoint_url" {
  description = "Endpoint URL thực tế từ Azure (cognitiveservices.azure.com)"
  value       = data.azurerm_cognitive_account.ai_foundry.endpoint
}

output "api_key" {
  description = "API Key 1 (SENSITIVE - không hiển thị trong console)"
  value       = data.external.cognitive_keys.result.key1
  sensitive   = true
}

output "api_key_2" {
  description = "API Key 2 (SENSITIVE - backup key)"
  value       = data.external.cognitive_keys.result.key2
  sensitive   = true
}

output "deployment_info" {
  description = "Thông tin đầy đủ về deployment để sử dụng trong code"
  value = {
    endpoint    = data.azurerm_cognitive_account.ai_foundry.endpoint
    model_name  = var.model_name
    api_version = "2024-02-15-preview"
    api_key     = "Sử dụng: terraform output -raw api_key"
  }
}

output "quick_start" {
  description = "Lệnh nhanh để export config"
  value = <<-EOT
    # Export API Key
    export AZURE_OPENAI_API_KEY=$(terraform output -raw api_key)
    export AZURE_OPENAI_ENDPOINT=$(terraform output -raw endpoint_url)
    
    # Hoặc dùng trong Python:
    # endpoint = "$(terraform output -raw endpoint_url)"
    # api_key = "$(terraform output -raw api_key)"
  EOT
}

