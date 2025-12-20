variable "location" {
  description = "Azure region để triển khai các tài nguyên"
  type        = string
  default     = "eastus"
}

variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
}

variable "resource_group_name" {
  description = "Tên của Resource Group có sẵn"
  type        = string
  default     = "rg-google-searching"
}

variable "ai_foundry_name" {
  description = "Tên của Azure AI Foundry resource (để trống sẽ tự động tạo)"
  type        = string
  default     = ""
}

variable "model_name" {
  description = "Tên model để deploy (ví dụ: gpt-4o-mini)"
  type        = string
  default     = "gpt-4o-mini"
}

variable "model_version" {
  description = "Phiên bản của model"
  type        = string
  default     = "2025-04-14"
}

variable "deployment_sku" {
  description = "SKU cho deployment (GlobalStandard hoặc GlobalStandardV2)"
  type        = string
  default     = "GlobalStandard"
}

variable "deployment_capacity" {
  description = "Capacity cho deployment (số lượng tokens/requests)"
  type        = number
  default     = 1
}

variable "tags" {
  description = "Tags để gán cho các resources"
  type        = map(string)
  default = {
    Environment = "dev"
    ManagedBy   = "Terraform"
  }
}

