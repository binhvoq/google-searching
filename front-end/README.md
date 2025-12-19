# GoogleSearching (Front-end)

Ứng dụng React + Vite + Tailwind để:
- Tìm kiếm địa điểm theo vùng/từ khoá
- Chat với A.I (Azure OpenAI gpt-4o-mini) và tự động gọi API tìm kiếm

## Yêu cầu

- Node.js 18+
- Backend đang chạy (mặc định `http://localhost:5000` hoặc `https://localhost:5001`)

## Cấu hình

Tạo `front-end/.env`:
```bash
VITE_API_URL=http://localhost:5000
```

## Chạy

```bash
cd front-end
npm install
npm run dev
```

## Build

```bash
cd front-end
npm run build
```

## API sử dụng

- `POST /api/Search`
- `POST /api/Chat`

