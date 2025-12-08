import { useState } from 'react';
import SearchForm from './components/SearchForm';
import PlaceList from './components/PlaceList';
import { searchService } from './services/api';
import type { SearchResponse } from './types';

function App() {
  const [searchResult, setSearchResult] = useState<SearchResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSearch = async (area: string, keyword?: string) => {
    setIsLoading(true);
    setError(null);
    setSearchResult(null);

    try {
      const result = await searchService.searchPlaces({ area, keyword });
      setSearchResult(result);
    } catch (err: any) {
      console.error('Search error:', err);
      const errorMessage = err.response?.data?.error || err.message || 'Đã xảy ra lỗi khi tìm kiếm';
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50">
      <div className="container mx-auto px-4 py-8 max-w-7xl">
        {/* Header */}
        <header className="text-center mb-8">
          <h1 className="text-4xl md:text-5xl font-bold text-gray-800 mb-3">
            🗺️ Tìm Kiếm Địa Điểm
          </h1>
          <p className="text-gray-600 text-lg">
            Tìm kiếm địa điểm theo vùng và từ khóa tại Việt Nam
          </p>
        </header>

        {/* Search Form */}
        <SearchForm onSearch={handleSearch} isLoading={isLoading} />

        {/* Error Message */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-red-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              <p className="text-red-800 font-medium">Lỗi: {error}</p>
            </div>
          </div>
        )}

        {/* Results */}
        <PlaceList searchResult={searchResult} isLoading={isLoading} />

        {/* Footer */}
        <footer className="mt-12 text-center text-gray-500 text-sm">
          <p>Powered by Google Maps API</p>
        </footer>
      </div>
    </div>
  );
}

export default App;

