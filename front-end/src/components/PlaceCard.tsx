import type { PlaceResponse } from '../types';

interface PlaceCardProps {
  place: PlaceResponse;
  index: number;
  onTagClick?: (tag: string) => void;
  activeTag?: string | null;
}

function formatTypeLabel(type: string): string {
  return type.replace(/_/g, ' ').replace(/\b\w/g, (l) => l.toUpperCase());
}

function getGoogleMapsUrl(placeId: string): string {
  return `https://www.google.com/maps/place/?q=place_id:${placeId}`;
}

export default function PlaceCard({ place, index, onTagClick, activeTag }: PlaceCardProps) {
  // Hiển thị tối đa 6 type
  const displayTypes = (place.types || []).slice(0, 6);

  return (
    <div className="group relative overflow-hidden rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5 transition hover:-translate-y-0.5 hover:shadow-xl">
      <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-primary-500 via-indigo-500 to-fuchsia-500 opacity-70" />
      <div className="p-6">
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2 mb-2">
              <span className="inline-flex items-center rounded-full bg-primary-50 px-2.5 py-1 text-xs font-bold text-primary-700 ring-1 ring-primary-200">
                #{index}
              </span>
              <h3 className="truncate text-lg font-bold text-slate-900">{place.name}</h3>
            </div>

            {place.rating !== null ? (
              <div className="flex items-center gap-2">
                <div className="flex items-center">
                  <svg className="w-5 h-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                  <span className="ml-1 text-slate-800 font-semibold">{place.rating.toFixed(1)}</span>
                </div>
                {place.userRatingsTotal > 0 && (
                  <span className="text-sm text-slate-500">
                    ({place.userRatingsTotal.toLocaleString('vi-VN')} đánh giá)
                  </span>
                )}
              </div>
            ) : (
              <p className="text-sm text-slate-500">Chưa có đánh giá</p>
            )}
          </div>
        </div>

        <div className="space-y-2 mb-5">
          <div className="flex items-start gap-2">
            <svg className="w-5 h-5 text-slate-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
              />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            <p className="text-slate-700 text-sm leading-6 flex-1">{place.address}</p>
          </div>

          {displayTypes.length > 0 && (
            <div className="flex items-start gap-2">
              <svg className="w-5 h-5 text-slate-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"
                />
              </svg>
              <div className="flex flex-wrap gap-2">
                {displayTypes.map((type) => {
                  const label = formatTypeLabel(type);
                  const isActive = activeTag === type;
                  
                  const baseClass =
                    'rounded-full border px-3 py-1 text-xs font-semibold transition focus:outline-none focus:ring-2 focus:ring-primary-200';
                  
                  const stateClass = isActive
                    ? 'bg-slate-900 text-white border-slate-900'
                    : 'bg-primary-50 text-primary-700 border-primary-200 hover:bg-primary-100 cursor-pointer';

                  return (
                    <button
                      key={type}
                      type="button"
                      onClick={() => onTagClick?.(type)}
                      aria-pressed={isActive}
                      className={`${baseClass} ${stateClass}`}
                      title="Click để tìm các địa điểm cùng loại"
                    >
                      {label}
                    </button>
                  );
                })}
              </div>
            </div>
          )}
        </div>

        <div className="flex gap-2">
          <a
            href={getGoogleMapsUrl(place.placeId)}
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 rounded-xl bg-primary-600 hover:bg-primary-700 text-white text-center py-2.5 px-4 transition duration-200 text-sm font-semibold shadow-sm"
          >
            Mở Google Maps
          </a>
          {place.location && (
            <button
              type="button"
              onClick={() => {
                const url = `https://www.google.com/maps?q=${place.location!.latitude},${place.location!.longitude}`;
                window.open(url, '_blank');
              }}
              className="rounded-xl border border-primary-600 text-primary-700 hover:bg-primary-50 px-4 py-2.5 transition duration-200 text-sm font-semibold"
            >
              Vị trí
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

