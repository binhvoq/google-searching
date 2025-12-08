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
    <div className="bg-white rounded-lg shadow-lg p-6 mb-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">
        🔍 Tìm Kiếm Địa Điểm
      </h2>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="area" className="block text-sm font-medium text-gray-700 mb-2">
            Vùng tìm kiếm <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="area"
            value={area}
            onChange={(e) => setArea(e.target.value)}
            placeholder="Ví dụ: Đà Lạt, Quận 8, Vũng Tàu..."
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none transition"
            required
            disabled={isLoading}
          />
          <p className="mt-1 text-sm text-gray-500">
            Nhập tên vùng, quận, huyện hoặc thành phố
          </p>
        </div>

        <div>
          <label htmlFor="keyword" className="block text-sm font-medium text-gray-700 mb-2">
            Từ khóa (tùy chọn)
          </label>
          <input
            type="text"
            id="keyword"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            placeholder="Ví dụ: khách sạn, cafe làm việc, bệnh viện..."
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none transition"
            disabled={isLoading}
          />
          <p className="mt-1 text-sm text-gray-500">
            Để trống nếu muốn tìm tất cả địa điểm trong vùng
          </p>
        </div>

        <button
          type="submit"
          disabled={isLoading || !area.trim()}
          className="w-full bg-primary-600 hover:bg-primary-700 text-white font-semibold py-3 px-6 rounded-lg transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          {isLoading ? (
            <>
              <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Đang tìm kiếm...
            </>
          ) : (
            <>
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              Tìm Kiếm
            </>
          )}
        </button>
      </form>
    </div>
  );
}

