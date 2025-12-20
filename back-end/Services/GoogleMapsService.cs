using System.Text.Json;
using GoogleSearching.Api.Models;
using Microsoft.Extensions.Options;

namespace GoogleSearching.Api.Services;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleMapsService> _logger;

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
    private string PlacesUrl => _configuration["GoogleMapsApi:PlacesBaseUrl"] ?? string.Empty;

    public async Task<LocationResponse?> GetLocationAndRadiusAsync(string area)
    {
        var fullAddress = $"{area}, Việt Nam";
        var url = $"{GeocodingUrl}?address={Uri.EscapeDataString(fullAddress)}&key={ApiKey}&language=vi";

        try
        {
            _logger.LogInformation("Google geocode. area={Area} baseUrl={BaseUrl}", area, GeocodingUrl);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GeocodingResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

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

    public async Task<List<PlaceResult>> SearchPlacesAsync(string area, string? keyword, LocationResponse location)
    {
        var allResults = new List<PlaceResult>();
        var seenPlaceIds = new HashSet<string>();
        string? nextPageToken = null;
        int pageNum = 1;

        while (true)
        {
            var url = $"{PlacesUrl}?location={location.Latitude},{location.Longitude}&radius={(int)location.Radius}&key={ApiKey}&language=vi";

            if (!string.IsNullOrEmpty(keyword))
            {
                url += $"&keyword={Uri.EscapeDataString(keyword)}";
            }

            if (nextPageToken != null)
            {
                url += $"&pagetoken={Uri.EscapeDataString(nextPageToken)}";
                await Task.Delay(2000); // Đợi 2 giây trước khi query next page
            }

            try
            {
                _logger.LogInformation(
                    "Google places. area={Area} keyword={Keyword} page={Page} hasPageToken={HasToken} baseUrl={BaseUrl}",
                    area,
                    string.IsNullOrWhiteSpace(keyword) ? "" : keyword,
                    pageNum,
                    nextPageToken != null,
                    PlacesUrl);
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<PlacesResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null || data.Status != "OK")
                {
                    if (data?.Status != "ZERO_RESULTS")
                    {
                        _logger.LogWarning("Places search failed: {Status}", data?.Status);
                    }
                    break;
                }

                foreach (var place in data.Results)
                {
                    if (!string.IsNullOrEmpty(place.PlaceId) && !seenPlaceIds.Contains(place.PlaceId))
                    {
                        seenPlaceIds.Add(place.PlaceId);
                        
                        // Lọc theo vùng
                        if (IsPlaceInArea(place, area, location))
                        {
                            allResults.Add(place);
                        }
                    }
                }

                _logger.LogInformation("Google places page done. area={Area} page={Page} pageResults={PageResults} totalKept={TotalKept} hasNext={HasNext}",
                    area, pageNum, data.Results.Count, allResults.Count, !string.IsNullOrEmpty(data.NextPageToken));
                nextPageToken = data.NextPageToken;
                if (string.IsNullOrEmpty(nextPageToken))
                {
                    break;
                }

                pageNum++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching places");
                break;
            }
        }

        return allResults;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Bán kính Trái Đất (mét)
        
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var deltaPhi = (lat2 - lat1) * Math.PI / 180;
        var deltaLambda = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private bool IsPlaceInArea(PlaceResult place, string areaQuery, LocationResponse centerLocation)
    {
        var address = place.Vicinity ?? place.FormattedAddress ?? string.Empty;
        var addressLower = address.ToLower();
        var areaNormalized = NormalizeAreaName(areaQuery);
        var areaKeywords = areaNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var addressNormalized = NormalizeAreaName(address);
        var hasAreaInAddress = areaKeywords
            .Where(k => k.Length > 2)
            .Any(keyword => addressNormalized.Contains(keyword));

        var withinDistance = true;
        if (place.Geometry?.Location != null)
        {
            var distance = CalculateDistance(
                centerLocation.Latitude,
                centerLocation.Longitude,
                place.Geometry.Location.Lat,
                place.Geometry.Location.Lng);
            withinDistance = distance <= centerLocation.Radius;
        }

        return hasAreaInAddress || withinDistance;
    }

    private string NormalizeAreaName(string area)
    {
        var replacements = new Dictionary<string, string>
        {
            { "á", "a" }, { "à", "a" }, { "ả", "a" }, { "ã", "a" }, { "ạ", "a" },
            { "ă", "a" }, { "ắ", "a" }, { "ằ", "a" }, { "ẳ", "a" }, { "ẵ", "a" }, { "ặ", "a" },
            { "â", "a" }, { "ấ", "a" }, { "ầ", "a" }, { "ẩ", "a" }, { "ẫ", "a" }, { "ậ", "a" },
            { "é", "e" }, { "è", "e" }, { "ẻ", "e" }, { "ẽ", "e" }, { "ẹ", "e" },
            { "ê", "e" }, { "ế", "e" }, { "ề", "e" }, { "ể", "e" }, { "ễ", "e" }, { "ệ", "e" },
            { "í", "i" }, { "ì", "i" }, { "ỉ", "i" }, { "ĩ", "i" }, { "ị", "i" },
            { "ó", "o" }, { "ò", "o" }, { "ỏ", "o" }, { "õ", "o" }, { "ọ", "o" },
            { "ô", "o" }, { "ố", "o" }, { "ồ", "o" }, { "ổ", "o" }, { "ỗ", "o" }, { "ộ", "o" },
            { "ơ", "o" }, { "ớ", "o" }, { "ờ", "o" }, { "ở", "o" }, { "ỡ", "o" }, { "ợ", "o" },
            { "ú", "u" }, { "ù", "u" }, { "ủ", "u" }, { "ũ", "u" }, { "ụ", "u" },
            { "ư", "u" }, { "ứ", "u" }, { "ừ", "u" }, { "ử", "u" }, { "ữ", "u" }, { "ự", "u" },
            { "ý", "y" }, { "ỳ", "y" }, { "ỷ", "y" }, { "ỹ", "y" }, { "ỵ", "y" },
            { "đ", "d" }
        };

        var text = area.ToLower();
        foreach (var (old, newChar) in replacements)
        {
            text = text.Replace(old, newChar);
        }

        return text.Trim();
    }
}

