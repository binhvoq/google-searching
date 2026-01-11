using GoogleSearching.Api.Models;

namespace GoogleSearching.Api.Services;

public class SearchService : ISearchService
{
    private readonly IGoogleMapsService _googleMapsService;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IGoogleMapsService googleMapsService,
        ILogger<SearchService> logger)
    {
        _googleMapsService = googleMapsService;
        _logger = logger;
    }

    public async Task<SearchResponse> SearchPlacesAsync(SearchRequest request)
    {
        // Bước 1: Lấy tọa độ và bán kính
        var location = await _googleMapsService.GetLocationAndRadiusAsync(request.Area);
        if (location == null)
        {
            return new SearchResponse
            {
                Area = request.Area,
                Keyword = request.Keyword,
                Places = new List<PlaceResponse>(),
                TotalCount = 0
            };
        }

        // Bước 2: Tìm kiếm địa điểm
        var places = await _googleMapsService.SearchPlacesAsync(request.Area, request.Keyword, location);

        // Bước 3: Chuyển đổi sang PlaceResponse và sắp xếp
        var placeResponses = places
            .Select(p => new PlaceResponse
            {
                PlaceId = p.PlaceId,
                Name = p.Name,
                Rating = p.Rating,
                UserRatingsTotal = p.UserRatingsTotal,
                Address = FormatAddress(p, request.Area),
                Vicinity = p.Vicinity ?? string.Empty,
                Types = p.Types ?? new List<string>(),
                PrimaryType = p.PrimaryType,
                Location = p.Geometry?.Location != null
                    ? new LocationResponse
                    {
                        Latitude = p.Geometry.Location.Lat,
                        Longitude = p.Geometry.Location.Lng,
                        Radius = 0
                    }
                    : null
            })
            .OrderByDescending(p => p.UserRatingsTotal)
            .ToList();

        return new SearchResponse
        {
            Area = request.Area,
            Keyword = request.Keyword,
            Places = placeResponses,
            TotalCount = placeResponses.Count,
            CenterLocation = location
        };
    }

    private string FormatAddress(PlaceResult place, string searchArea)
    {
        var address = place.FormattedAddress ?? place.Vicinity ?? "N/A";

        if (address == "N/A")
        {
            return "N/A";
        }

        var addressLower = address.ToLower();
        var fullAddressIndicators = new[]
        {
            "việt nam", "vietnam", "viet nam",
            "hồ chí minh", "ho chi minh", "hcm", "tp.hcm",
            "hà nội", "ha noi", "hn",
            "đà lạt", "da lat", "lâm đồng", "lam dong",
            "vũng tàu", "vung tau", "bà rịa", "ba ria",
            "đà nẵng", "da nang",
            "cần thơ", "can tho",
            "huế", "hue", "thừa thiên", "thua thien"
        };

        var isFullAddress = fullAddressIndicators.Any(indicator => addressLower.Contains(indicator));

        if (!isFullAddress && !string.IsNullOrEmpty(searchArea))
        {
            address = $"{address}, {searchArea}, Việt Nam";
        }

        return address;
    }
}

