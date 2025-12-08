# Google Searching API - Backend

Web API .NET để tìm kiếm địa điểm theo vùng và từ khóa tại Việt Nam sử dụng Google Maps API.

## Tính năng

- Tìm kiếm địa điểm theo vùng (ví dụ: "Đà Lạt", "Quận 8, HCM", "Vũng Tàu")
- Tìm kiếm theo từ khóa (ví dụ: "khách sạn", "cafe làm việc", "bệnh viện")
- Tự động tính bán kính tìm kiếm dựa trên Viewport của Google Maps
- Lọc kết quả theo vùng (kiểm tra địa chỉ và khoảng cách)
- Sắp xếp kết quả theo số đánh giá
- Hỗ trợ CORS để front-end có thể gọi API

## Yêu cầu

- .NET 8.0 SDK
- Google Maps API Key (đã cấu hình trong `appsettings.json`)

## Cài đặt và chạy

1. Restore packages:
```bash
dotnet restore
```

2. Chạy ứng dụng:
```bash
dotnet run
```

3. Mở trình duyệt và truy cập:
   - Swagger UI: `http://localhost:5000/swagger`
   - API: `http://localhost:5000/api/search`

## API Endpoints

### POST /api/search
Tìm kiếm địa điểm theo vùng và từ khóa.

**Request Body:**
```json
{
  "area": "Gò Vấp",
  "keyword": "thịt nướng"
}
```

**Response:**
```json
{
  "places": [
    {
      "placeId": "...",
      "name": "...",
      "rating": 4.5,
      "userRatingsTotal": 100,
      "address": "...",
      "vicinity": "...",
      "types": ["restaurant", "food"],
      "location": {
        "latitude": 10.123,
        "longitude": 106.456,
        "radius": 0
      }
    }
  ],
  "totalCount": 10,
  "area": "Gò Vấp",
  "keyword": "thịt nướng",
  "centerLocation": {
    "latitude": 10.123,
    "longitude": 106.456,
    "radius": 5000
  }
}
```

### GET /api/search
Tìm kiếm địa điểm (GET method).

**Query Parameters:**
- `area` (required): Vùng tìm kiếm
- `keyword` (optional): Từ khóa tìm kiếm

**Example:**
```
GET /api/search?area=Gò Vấp&keyword=thịt nướng
```

## Cấu hình

Cấu hình Google Maps API Key trong `appsettings.json`:

```json
{
  "GoogleMapsApi": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

## CORS

API đã được cấu hình CORS để cho phép tất cả origins, methods và headers. Điều này cho phép front-end từ bất kỳ domain nào có thể gọi API.

