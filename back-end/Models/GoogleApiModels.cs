using System.Text.Json.Serialization;

namespace GoogleSearching.Api.Models;

// Models cho Google Geocoding API Response
public class GeocodingResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("results")]
    public List<GeocodingResult> Results { get; set; } = new();
}

public class GeocodingResult
{
    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; } = new();
    
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; } = string.Empty;
}

public class Geometry
{
    [JsonPropertyName("location")]
    public Location Location { get; set; } = new();
    
    [JsonPropertyName("viewport")]
    public Viewport? Viewport { get; set; }
}

public class Location
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }
    
    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

public class Viewport
{
    [JsonPropertyName("northeast")]
    public Location Northeast { get; set; } = new();
    
    [JsonPropertyName("southwest")]
    public Location Southwest { get; set; } = new();
}

// Models cho Google Places Nearby Search API Response
public class PlacesResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }
    
    [JsonPropertyName("results")]
    public List<PlaceResult> Results { get; set; } = new();
}

public class PlaceResult
{
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("rating")]
    public double? Rating { get; set; }
    
    [JsonPropertyName("user_ratings_total")]
    public int UserRatingsTotal { get; set; }
    
    [JsonPropertyName("vicinity")]
    public string? Vicinity { get; set; }
    
    [JsonPropertyName("formatted_address")]
    public string? FormattedAddress { get; set; }
    
    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = new();

    [JsonPropertyName("primary_type")]
    public string? PrimaryType { get; set; }

    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; } = new();
}

