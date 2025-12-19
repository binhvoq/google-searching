import { useState } from 'react';
import SearchForm from './components/SearchForm';
import PlaceList from './components/PlaceList';
import TabBar, { TabKey } from './components/TabBar';
import ChatWithAI from './components/ChatWithAI';
import { searchService } from './services/api';
import type { SearchResponse } from './types';

function App() {
  const [activeTab, setActiveTab] = useState<TabKey>('search');
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
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-indigo-50">
      <div className="mx-auto max-w-7xl px-4 py-8">
        <header className="mb-8 flex flex-col gap-5 md:flex-row md:items-end md:justify-between">
          <div>
            <div className="inline-flex items-center gap-2 rounded-full bg-white/70 px-3 py-1 text-xs font-semibold text-slate-700 ring-1 ring-black/5 shadow-sm">
              <span className="inline-block h-2 w-2 rounded-full bg-emerald-500" />
              GoogleSearching
            </div>
            <h1 className="mt-3 text-3xl md:text-4xl font-extrabold tracking-tight text-slate-900">
              Tìm kiếm địa điểm
            </h1>
            <p className="mt-2 text-slate-600">
              Tìm địa điểm theo vùng và từ khoá tại Việt Nam — hoặc chuyển sang “Chat với A.I”.
            </p>
          </div>

          <TabBar
            activeTab={activeTab}
            onChange={(tab) => {
              setActiveTab(tab);
              setError(null);
            }}
          />
        </header>

        {activeTab === 'search' ? (
          <div className="space-y-6">
            <SearchForm onSearch={handleSearch} isLoading={isLoading} />

            {error && (
              <div className="rounded-2xl bg-red-50 ring-1 ring-red-200 p-4">
                <div className="flex items-start gap-3">
                  <svg className="mt-0.5 w-5 h-5 text-red-600" fill="currentColor" viewBox="0 0 20 20">
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <div>
                    <p className="text-red-900 font-semibold">Có lỗi xảy ra</p>
                    <p className="mt-1 text-red-800 text-sm leading-6">{error}</p>
                  </div>
                </div>
              </div>
            )}

            <PlaceList searchResult={searchResult} isLoading={isLoading} />
          </div>
        ) : (
          <ChatWithAI />
        )}

        <footer className="mt-12 text-center text-slate-500 text-sm">
          <p>Powered by Google Maps API</p>
        </footer>
      </div>
    </div>
  );
}

export default App;

