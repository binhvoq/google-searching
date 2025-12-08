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

