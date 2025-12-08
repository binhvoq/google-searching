namespace GoogleSearching.Api.Models;

public class SearchResponse
{
    public List<PlaceResponse> Places { get; set; } = new();
    public int TotalCount { get; set; }
    public string Area { get; set; } = string.Empty;
    public string? Keyword { get; set; }
    public LocationResponse? CenterLocation { get; set; }
}

