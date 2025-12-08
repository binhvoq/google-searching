import axios from 'axios';
import type { SearchRequest, SearchResponse } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

// Debug: Log ra để kiểm tra giá trị thực tế
console.log('🔍 Debug API URL:', {
  'import.meta.env.VITE_API_URL': import.meta.env.VITE_API_URL,
  'API_BASE_URL (final)': API_BASE_URL,
  'All env vars': import.meta.env
});

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const searchService = {
  /**
   * Tìm kiếm địa điểm theo vùng và từ khóa (POST)
   */
  searchPlaces: async (request: SearchRequest): Promise<SearchResponse> => {
    const response = await apiClient.post<SearchResponse>('/api/Search', request);
    return response.data;
  },

  /**
   * Tìm kiếm địa điểm theo vùng và từ khóa (GET)
   */
  searchPlacesGet: async (area: string, keyword?: string): Promise<SearchResponse> => {
    const params = new URLSearchParams({ area });
    if (keyword) {
      params.append('keyword', keyword);
    }
    const response = await apiClient.get<SearchResponse>(`/api/Search?${params.toString()}`);
    return response.data;
  },
};

