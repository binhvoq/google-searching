# Google Searching - Frontend

Ứng dụng React để tìm kiếm địa điểm theo vùng và từ khóa tại Việt Nam, sử dụng Google Maps API.

## Tính năng

- 🔍 Tìm kiếm địa điểm theo vùng (quận, huyện, thành phố)
- 🏷️ Tìm kiếm với từ khóa (khách sạn, cafe, bệnh viện...)
- 📍 Hiển thị thông tin chi tiết: tên, đánh giá, địa chỉ, loại địa điểm
- 🗺️ Liên kết đến Google Maps để xem vị trí
- 🎨 UI hiện đại, responsive với Tailwind CSS

## Yêu cầu

- Node.js 18+ 
- npm hoặc yarn
- Backend API đang chạy tại `http://localhost:5000`

## Cài đặt

```bash
# Cài đặt dependencies
npm install

# Hoặc sử dụng yarn
yarn install
```

## Chạy ứng dụng

```bash
# Chạy development server
npm run dev

# Hoặc
yarn dev
```

Ứng dụng sẽ chạy tại `http://localhost:3000`

## Build cho production

```bash
npm run build
```

## Cấu trúc dự án

```
front-end/
├── src/
│   ├── components/      # React components
│   │   ├── SearchForm.tsx
│   │   ├── PlaceCard.tsx
│   │   └── PlaceList.tsx
│   ├── services/        # API services
│   │   └── api.ts
│   ├── types/           # TypeScript types
│   │   └── index.ts
│   ├── App.tsx          # Main App component
│   ├── main.tsx         # Entry point
│   └── index.css        # Global styles
├── public/              # Static files
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
└── tailwind.config.js
```

## API Endpoints

Ứng dụng sử dụng các endpoint sau từ backend:

- `POST /api/Search` - Tìm kiếm địa điểm
- `GET /api/Search?area={area}&keyword={keyword}` - Tìm kiếm địa điểm (GET method)

## Cấu hình

Có thể cấu hình API URL thông qua biến môi trường:

Tạo file `.env`:
```
VITE_API_URL=http://localhost:5000
```

## Công nghệ sử dụng

- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **Tailwind CSS** - Styling
- **Axios** - HTTP client

