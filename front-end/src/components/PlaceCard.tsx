import type { PlaceResponse } from '../types';

interface PlaceCardProps {
  place: PlaceResponse;
  index: number;
}

export default function PlaceCard({ place, index }: PlaceCardProps) {
  const formatTypes = (types: string[]): string => {
    return types
      .slice(0, 3)
      .map(type => type.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase()))
      .join(', ');
  };

  const getGoogleMapsUrl = (placeId: string): string => {
    return `https://www.google.com/maps/place/?q=place_id:${placeId}`;
  };

  return (
    <div className="bg-white rounded-lg shadow-md hover:shadow-lg transition-shadow duration-200 p-6 border border-gray-100">
      <div className="flex items-start justify-between mb-3">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <span className="text-primary-600 font-bold text-lg">#{index}</span>
            <h3 className="text-xl font-semibold text-gray-800">{place.name}</h3>
          </div>
          
          {place.rating !== null && (
            <div className="flex items-center gap-2 mb-2">
              <div className="flex items-center">
                <svg className="w-5 h-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                </svg>
                <span className="ml-1 text-gray-700 font-semibold">{place.rating.toFixed(1)}</span>
              </div>
              {place.userRatingsTotal > 0 && (
                <span className="text-sm text-gray-500">
                  ({place.userRatingsTotal.toLocaleString('vi-VN')} đánh giá)
                </span>
              )}
            </div>
          )}

          {place.rating === null && (
            <p className="text-sm text-gray-500 mb-2">Chưa có đánh giá</p>
          )}
        </div>
      </div>

      <div className="space-y-2 mb-4">
        <div className="flex items-start gap-2">
          <svg className="w-5 h-5 text-gray-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          <p className="text-gray-600 text-sm flex-1">{place.address}</p>
        </div>

        {place.types.length > 0 && (
          <div className="flex items-start gap-2">
            <svg className="w-5 h-5 text-gray-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
            </svg>
            <p className="text-gray-600 text-sm">{formatTypes(place.types)}</p>
          </div>
        )}
      </div>

      <div className="flex gap-2">
        <a
          href={getGoogleMapsUrl(place.placeId)}
          target="_blank"
          rel="noopener noreferrer"
          className="flex-1 bg-primary-600 hover:bg-primary-700 text-white text-center py-2 px-4 rounded-lg transition duration-200 text-sm font-medium"
        >
          Xem trên Google Maps
        </a>
        {place.location && (
          <button
            onClick={() => {
              const url = `https://www.google.com/maps?q=${place.location!.latitude},${place.location!.longitude}`;
              window.open(url, '_blank');
            }}
            className="px-4 py-2 border border-primary-600 text-primary-600 hover:bg-primary-50 rounded-lg transition duration-200 text-sm font-medium"
          >
            📍 Vị trí
          </button>
        )}
      </div>
    </div>
  );
}

