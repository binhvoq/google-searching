using System.Text.Json.Serialization;

namespace GoogleSearching.Api.Models;

public class PlacesV1SearchResponse
{
    [JsonPropertyName("places")]
    public List<PlacesV1Place> Places { get; set; } = new();

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}

public class PlacesV1Place
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("displayName")]
    public PlacesV1LocalizedText? DisplayName { get; set; }

    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("userRatingCount")]
    public int? UserRatingCount { get; set; }

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = new();

    [JsonPropertyName("primaryType")]
    public string? PrimaryType { get; set; }

    [JsonPropertyName("location")]
    public PlacesV1LatLng? Location { get; set; }
}

public class PlacesV1LocalizedText
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; set; }
}

public class PlacesV1LatLng
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

