namespace GoogleSearching.Api.Models;

// Models cho Google Geocoding API Response
public class GeocodingResponse
{
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<GeocodingResult> Results { get; set; } = new();
}

public class GeocodingResult
{
    public Geometry Geometry { get; set; } = new();
    public string FormattedAddress { get; set; } = string.Empty;
}

public class Geometry
{
    public Location Location { get; set; } = new();
    public Viewport? Viewport { get; set; }
}

public class Location
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class Viewport
{
    public Location Northeast { get; set; } = new();
    public Location Southwest { get; set; } = new();
}

// Models cho Google Places Nearby Search API Response
public class PlacesResponse
{
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? NextPageToken { get; set; }
    public List<PlaceResult> Results { get; set; } = new();
}

public class PlaceResult
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public int UserRatingsTotal { get; set; }
    public string? Vicinity { get; set; }
    public string? FormattedAddress { get; set; }
    public List<string> Types { get; set; } = new();
    public Geometry Geometry { get; set; } = new();
}

