namespace GoogleSearching.Api.Models;

public class PlaceResponse
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public int UserRatingsTotal { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Vicinity { get; set; } = string.Empty;
    public List<string> Types { get; set; } = new();
    public LocationResponse? Location { get; set; }
}

