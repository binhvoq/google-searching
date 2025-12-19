import type { SearchResponse } from '../types';
import PlaceCard from './PlaceCard';

interface PlaceListProps {
  searchResult: SearchResponse | null;
  isLoading: boolean;
}

export default function PlaceList({ searchResult, isLoading }: PlaceListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-14">
        <div className="text-center">
          <svg
            className="animate-spin h-12 w-12 text-primary-600 mx-auto mb-4"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
          <p className="text-slate-600">Đang tìm kiếm địa điểm...</p>
        </div>
      </div>
    );
  }

  if (!searchResult) {
    return (
      <div className="text-center py-14">
        <div className="mx-auto mb-4 grid h-20 w-20 place-items-center rounded-2xl bg-white/70 ring-1 ring-black/5 shadow-sm">
          <svg className="w-10 h-10 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
        </div>
        <p className="text-slate-700 text-lg font-semibold">Nhập thông tin để bắt đầu tìm kiếm</p>
        <p className="mt-1 text-slate-500">Gợi ý: “Đà Lạt” + “cafe làm việc”</p>
      </div>
    );
  }

  if (searchResult.totalCount === 0) {
    return (
      <div className="text-center py-14">
        <div className="mx-auto mb-4 grid h-20 w-20 place-items-center rounded-2xl bg-white/70 ring-1 ring-black/5 shadow-sm">
          <svg className="w-10 h-10 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <p className="text-slate-800 text-lg font-semibold mb-2">Không tìm thấy địa điểm nào</p>
        <p className="text-slate-500">Thử đổi vùng tìm kiếm hoặc từ khoá.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5 p-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h3 className="text-base font-bold text-slate-900">Kết quả tìm kiếm</h3>
            <p className="text-sm text-slate-600 mt-1">
              Tìm thấy <span className="font-bold text-primary-700">{searchResult.totalCount}</span> địa điểm
              {searchResult.keyword && (
                <>
                  {' '}
                  cho từ khoá <span className="font-semibold">“{searchResult.keyword}”</span>
                </>
              )}{' '}
              tại <span className="font-semibold">{searchResult.area}</span>
            </p>
          </div>

          {searchResult.centerLocation && (
            <div className="rounded-xl bg-white px-4 py-3 ring-1 ring-black/5">
              <p className="text-xs text-slate-500">Vị trí trung tâm</p>
              <p className="mt-1 text-sm font-mono text-slate-800">
                {searchResult.centerLocation.latitude.toFixed(6)}, {searchResult.centerLocation.longitude.toFixed(6)}
              </p>
            </div>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {searchResult.places.map((place, index) => (
          <PlaceCard key={place.placeId} place={place} index={index + 1} />
        ))}
      </div>
    </div>
  );
}

