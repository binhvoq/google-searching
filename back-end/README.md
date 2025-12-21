# GoogleSearching API (Back-end)

Web API .NET 8 để:
- Tìm kiếm địa điểm theo vùng/từ khoá (Google Maps API)
- Chat với A.I bằng Azure AI Foundry (gpt-4o-mini) và tự động gọi tool `search_places`

## Yêu cầu

- .NET 8 SDK
- Google Maps API Key
- Azure AI Foundry endpoint + key + deployment name

## Cấu hình

Ưu tiên cấu hình bằng biến môi trường (khuyến nghị cho production):

- `GoogleMapsApi__ApiKey`
- `AzureOpenAI__Endpoint` (vd: `https://<resource>.cognitiveservices.azure.com/`)
- `AzureOpenAI__ApiKey`
- `AzureOpenAI__DeploymentName` (vd: `gpt-4o-mini`)
- `AzureOpenAI__ApiVersion` (vd: `2024-08-01-preview`)

Bạn cũng có thể cấu hình trong `back-end/appsettings.json` (không nên commit secrets).

## Chạy local

```bash
dotnet restore back-end/GoogleSearching.Api.csproj
dotnet run --project back-end/GoogleSearching.Api.csproj
```

Mặc định:
- Swagger UI: `https://localhost:5001/swagger` (hoặc port khác tuỳ máy)
- API: `https://localhost:5001/api/...`

## API Endpoints

- `POST /api/Search` và `GET /api/Search?area=...&keyword=...`
- `POST /api/Chat`

### POST /api/Chat

Request body:
```json
{
  "sessionId": "s_...",
  "message": "Tìm bệnh viện gần Quận 1",
  "autoRunApi": true
}
```

Response:
```json
{
  "sessionId": "s_...",
  "assistantMessage": "...",
  "memorySummary": "...",
  "toolCalls": [
    { "name": "search_places", "status": "done", "detail": "10 results" }
  ]
}
```

