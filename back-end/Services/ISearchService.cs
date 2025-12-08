using GoogleSearching.Api.Models;

namespace GoogleSearching.Api.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchPlacesAsync(SearchRequest request);
}

