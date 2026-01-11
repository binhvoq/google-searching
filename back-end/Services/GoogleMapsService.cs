using System.Net.Http.Json;
using System.Text.Json;
using GoogleSearching.Api.Models;

namespace GoogleSearching.Api.Services;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleMapsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GoogleMapsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleMapsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string ApiKey => _configuration["GoogleMapsApi:ApiKey"] ?? string.Empty;
    private string GeocodingUrl => _configuration["GoogleMapsApi:GeocodingBaseUrl"] ?? string.Empty;

    // Places API (New) endpoints (REST v1)
    private string PlacesSearchNearbyUrl =>
        _configuration["GoogleMapsApi:PlacesSearchNearbyUrl"] ?? "https://places.googleapis.com/v1/places:searchNearby";

    private string PlacesSearchTextUrl =>
        _configuration["GoogleMapsApi:PlacesSearchTextUrl"] ?? "https://places.googleapis.com/v1/places:searchText";

    // Field Mask cho Text Search (có nextPageToken)
    private const string TextSearchFieldMask =
        "places.id,places.displayName,places.formattedAddress,places.rating,places.userRatingCount,places.types,places.primaryType,places.location,nextPageToken";

    public async Task<LocationResponse?> GetLocationAndRadiusAsync(string area)
    {
        // Kiểm tra xem area đã có thông tin quốc gia chưa
        var areaLower = area.ToLower();
        var hasCountry = areaLower.Contains("việt nam") || 
                         areaLower.Contains("vietnam") || 
                         areaLower.Contains("france") || 
                         areaLower.Contains("pháp") ||
                         areaLower.Contains("usa") ||
                         areaLower.Contains("mỹ") ||
                         areaLower.Contains("japan") ||
                         areaLower.Contains("nhật");

        var fullAddress = hasCountry ? area : $"{area}, Việt Nam";
        var url = $"{GeocodingUrl}?address={Uri.EscapeDataString(fullAddress)}&key={ApiKey}&language=vi";

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GeocodingResponse>(json, JsonOptions);

            if (data == null || data.Status != "OK" || data.Results.Count == 0)
            {
                _logger.LogWarning("Geocoding failed: {Status} for area: {Area}", data?.Status, area);
                return null;
            }

            var result = data.Results[0];
            var location = result.Geometry.Location;
            var viewport = result.Geometry.Viewport;

            double radius = 5000; // Mặc định 5km

            if (viewport != null && viewport.Northeast != null)
            {
                var neLat = viewport.Northeast.Lat;
                var neLng = viewport.Northeast.Lng;
                radius = CalculateDistance(location.Lat, location.Lng, neLat, neLng) * 1.2;
                radius = Math.Min(radius, 50000); // Tối đa 50km
                radius = Math.Max(radius, 2000); // Tối thiểu 2km
            }

            return new LocationResponse
            {
                Latitude = location.Lat,
                Longitude = location.Lng,
                Radius = radius
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for area: {Area}", area);
            return null;
        }
    }

    public async Task<List<PlaceResult>> SearchPlacesAsync(
        string area,
        string? keyword,
        LocationResponse location)
    {
        string effectiveKeyword = keyword ?? string.Empty;
        
        _logger.LogInformation("Final Strategy: Area='{Area}', Keyword='{Key}'", area, effectiveKeyword);

        return await SearchTextAsync(area, effectiveKeyword, location);
    }

    private async Task<List<PlaceResult>> SearchTextAsync(
        string area,
        string keyword,
        LocationResponse location)
    {
        var allResults = new List<PlaceResult>();
        var seenPlaceIds = new HashSet<string>();
        string? nextPageToken = null;

        // Text Search (New): locationRestriction chỉ hỗ trợ viewport, circle chỉ dùng được qua locationBias
        var textQuery = $"{keyword} {area}".Trim();

        var regionCode = area.ToLower().Contains("france") || area.ToLower().Contains("pháp") ? "FR" : "VN";

        while (true)
        {
            var body = new
            {
                textQuery,
                maxResultCount = 20,
                languageCode = "vi",
                regionCode = regionCode,
                locationBias = new
                {
                    circle = new
                    {
                        center = new
                        {
                            latitude = location.Latitude,
                            longitude = location.Longitude
                        },
                        radius = location.Radius
                    }
                },
                pageToken = nextPageToken
            };

            var data = await SendPlacesRequestAsync(PlacesSearchTextUrl, body, TextSearchFieldMask);
            
            if (data == null || data.Places.Count == 0)
            {
                break;
            }

            foreach (var place in data.Places)
            {
                var mapped = MapV1Place(place);
                if (string.IsNullOrWhiteSpace(mapped.PlaceId) || !seenPlaceIds.Add(mapped.PlaceId))
                {
                    continue;
                }

                allResults.Add(mapped);
            }

            nextPageToken = data.NextPageToken;
            if (string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }

            await Task.Delay(2000);
        }

        return allResults;
    }

    private async Task<PlacesV1SearchResponse?> SendPlacesRequestAsync(string url, object body, string fieldMask)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            _logger.LogWarning("GoogleMapsApi:ApiKey is empty.");
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };

        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", ApiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", fieldMask);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Places API (New) failed: {StatusCode}. Body: {Body}", (int)response.StatusCode, json);
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PlacesV1SearchResponse>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Places API (New) response. Body: {Body}", json);
            return null;
        }
    }

    private static PlaceResult MapV1Place(PlacesV1Place place)
    {
        var lat = place.Location?.Latitude;
        var lng = place.Location?.Longitude;

        return new PlaceResult
        {
            PlaceId = place.Id ?? string.Empty,
            Name = place.DisplayName?.Text ?? string.Empty,
            Rating = place.Rating,
            UserRatingsTotal = place.UserRatingCount ?? 0,
            FormattedAddress = place.FormattedAddress,
            Vicinity = null,
            Types = place.Types ?? new List<string>(),
            PrimaryType = place.PrimaryType,
            Geometry = new Geometry
            {
                Location = new Location
                {
                    Lat = lat ?? 0,
                    Lng = lng ?? 0
                },
                Viewport = null
            }
        };
    }
}

