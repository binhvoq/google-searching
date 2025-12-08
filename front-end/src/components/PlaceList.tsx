import type { SearchResponse } from '../types';
import PlaceCard from './PlaceCard';

interface PlaceListProps {
  searchResult: SearchResponse | null;
  isLoading: boolean;
}

export default function PlaceList({ searchResult, isLoading }: PlaceListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <svg className="animate-spin h-12 w-12 text-primary-600 mx-auto mb-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p className="text-gray-600">Đang tìm kiếm địa điểm...</p>
        </div>
      </div>
    );
  }

  if (!searchResult) {
    return (
      <div className="text-center py-12">
        <div className="text-gray-400 mb-4">
          <svg className="w-24 h-24 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        <p className="text-gray-600 text-lg">Nhập thông tin để bắt đầu tìm kiếm</p>
      </div>
    );
  }

  if (searchResult.totalCount === 0) {
    return (
      <div className="text-center py-12">
        <div className="text-gray-400 mb-4">
          <svg className="w-24 h-24 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
        <p className="text-gray-600 text-lg font-semibold mb-2">Không tìm thấy địa điểm nào</p>
        <p className="text-gray-500">Thử thay đổi vùng tìm kiếm hoặc từ khóa</p>
      </div>
    );
  }

  return (
    <div>
      <div className="bg-white rounded-lg shadow-md p-4 mb-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-semibold text-gray-800">
              📋 Kết quả tìm kiếm
            </h3>
            <p className="text-sm text-gray-600 mt-1">
              Tìm thấy <span className="font-bold text-primary-600">{searchResult.totalCount}</span> địa điểm
              {searchResult.keyword && (
                <> cho từ khóa "<span className="font-semibold">{searchResult.keyword}</span>"</>
              )}
              {' '}tại <span className="font-semibold">{searchResult.area}</span>
            </p>
          </div>
          {searchResult.centerLocation && (
            <div className="text-right">
              <p className="text-xs text-gray-500">Vị trí trung tâm</p>
              <p className="text-sm font-mono text-gray-700">
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

