import { useState, FormEvent } from 'react';

interface SearchFormProps {
  onSearch: (area: string, keyword?: string) => void;
  isLoading: boolean;
}

export default function SearchForm({ onSearch, isLoading }: SearchFormProps) {
  const [area, setArea] = useState('');
  const [keyword, setKeyword] = useState('');

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (area.trim()) {
      onSearch(area.trim(), keyword.trim() || undefined);
    }
  };

  return (
    <div className="rounded-2xl bg-white/70 backdrop-blur-md shadow-lg ring-1 ring-black/5 p-6">
      <div className="flex items-start justify-between gap-4 mb-5">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Tìm kiếm địa điểm</h2>
          <p className="mt-1 text-sm text-slate-600">Nhập khu vực và (tuỳ chọn) từ khoá để tìm địa điểm.</p>
        </div>
        <div className="hidden sm:flex items-center gap-2 rounded-xl bg-white/70 px-3 py-2 text-xs font-semibold text-slate-700 ring-1 ring-black/5">
          <span className="inline-block h-2 w-2 rounded-full bg-primary-500" />
          Google Maps API
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="area" className="block text-sm font-semibold text-slate-800 mb-2">
            Vùng tìm kiếm <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="area"
            value={area}
            onChange={(e) => setArea(e.target.value)}
            placeholder="Ví dụ: Đà Lạt, Quận 8, Vũng Tàu..."
            className="w-full rounded-xl border border-black/10 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-primary-300 focus:ring-4 focus:ring-primary-100 disabled:bg-slate-50"
            required
            disabled={isLoading}
          />
          <p className="mt-1 text-sm text-slate-500">Nhập tên vùng, quận/huyện hoặc thành phố.</p>
        </div>

        <div>
          <label htmlFor="keyword" className="block text-sm font-semibold text-slate-800 mb-2">
            Từ khoá (tuỳ chọn)
          </label>
          <input
            type="text"
            id="keyword"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            placeholder="Ví dụ: khách sạn, cafe làm việc, bệnh viện..."
            className="w-full rounded-xl border border-black/10 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-primary-300 focus:ring-4 focus:ring-primary-100 disabled:bg-slate-50"
            disabled={isLoading}
          />
          <p className="mt-1 text-sm text-slate-500">Để trống nếu muốn tìm tất cả địa điểm trong vùng.</p>
        </div>

        <button
          type="submit"
          disabled={isLoading || !area.trim()}
          className="w-full rounded-xl bg-primary-600 hover:bg-primary-700 text-white font-semibold py-3 px-6 transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 shadow-sm"
        >
          {isLoading ? (
            <>
              <svg
                className="animate-spin h-5 w-5 text-white"
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
              Đang tìm kiếm...
            </>
          ) : (
            <>
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                />
              </svg>
              Tìm kiếm
            </>
          )}
        </button>
      </form>
    </div>
  );
}

