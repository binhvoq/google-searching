export interface LocationResponse {
  latitude: number;
  longitude: number;
  radius: number;
}

export interface PlaceResponse {
  placeId: string;
  name: string;
  rating: number | null;
  userRatingsTotal: number;
  address: string;
  vicinity: string;
  types: string[];
  location: LocationResponse | null;
}

export interface SearchRequest {
  area: string;
  keyword?: string;
}

export interface SearchResponse {
  places: PlaceResponse[];
  totalCount: number;
  area: string;
  keyword?: string;
  centerLocation: LocationResponse | null;
}

export interface ChatRequest {
  sessionId?: string;
  message: string;
  autoRunApi: boolean;
}

export interface ChatToolCall {
  name: string;
  status: 'queued' | 'running' | 'done' | 'error' | string;
  detail?: string;
}

export interface ChatResponse {
  sessionId: string;
  assistantMessage: string;
  memorySummary: string;
  toolCalls: ChatToolCall[];
}

