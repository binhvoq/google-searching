# Infrastructure (Terraform)

Terraform sẽ tạo:
- Resource Group + App Service (backend .NET)
- Static Web App (frontend)
- Azure OpenAI (Cognitive Account kind `OpenAI`) + model deployment `gpt-4o-mini`

## Yêu cầu

- Azure CLI (`az`) đã login
- Terraform
- Quyền tạo resource trên subscription

## Provision hạ tầng

```bash
az login
az account set --subscription "<SUBSCRIPTION_ID_OR_NAME>"
```

Tạo file `infrastructure/terraform.tfvars` từ mẫu:
```bash
cd infrastructure
copy terraform.tfvars.example terraform.tfvars
```

Sửa `terraform.tfvars` (bắt buộc điền `google_maps_api_key`).

Chạy Terraform:
```bash
cd infrastructure
terraform init
terraform plan -out tfplan
terraform apply tfplan
```

Xem outputs:
```bash
cd infrastructure
terraform output
```

## Chạy app local (dùng Azure OpenAI đã tạo)

PowerShell:
```powershell
cd infrastructure
$env:AzureOpenAI__Endpoint = terraform output -raw openai_endpoint
$env:AzureOpenAI__ApiKey = terraform output -raw openai_api_key
$env:AzureOpenAI__DeploymentName = terraform output -raw openai_deployment_name
$env:AzureOpenAI__ApiVersion = terraform output -raw openai_api_version
$env:GoogleMapsApi__ApiKey = "<YOUR_GOOGLE_MAPS_API_KEY>"
```

Chạy backend:
```powershell
dotnet run --project ..\\back-end\\GoogleSearching.Api.csproj
```

Chạy frontend:
```powershell
cd ..\\front-end
$env:VITE_API_URL="http://localhost:5000"
npm run dev
```

## Ghi chú

- Nếu `terraform apply` báo không hỗ trợ `openai_model_version` ở region/subscription của bạn, hãy đổi biến `openai_model_version` (hoặc `openai_location`) trong `terraform.tfvars`.

