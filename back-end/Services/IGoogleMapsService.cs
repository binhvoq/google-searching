using GoogleSearching.Api.Models;

namespace GoogleSearching.Api.Services;

public interface IGoogleMapsService
{
    Task<LocationResponse?> GetLocationAndRadiusAsync(string area);
    Task<List<PlaceResult>> SearchPlacesAsync(string area, string? keyword, LocationResponse location);
}

