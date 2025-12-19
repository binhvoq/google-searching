using GoogleSearching.Api.Models;
using GoogleSearching.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoogleSearching.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Tìm kiếm địa điểm theo vùng và từ khoá
    /// </summary>
    /// <param name="request">Request chứa vùng (Area) và từ khoá (Keyword) tuỳ chọn</param>
    [HttpPost]
    public async Task<ActionResult<SearchResponse>> SearchPlaces([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Area))
        {
            return BadRequest(new { error = "Vùng tìm kiếm (Area) là bắt buộc" });
        }

        try
        {
            var result = await _searchService.SearchPlacesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching places");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi tìm kiếm địa điểm" });
        }
    }

    /// <summary>
    /// Tìm kiếm địa điểm theo vùng và từ khoá (GET method)
    /// </summary>
    /// <param name="area">Vùng tìm kiếm (ví dụ: "Đà Lạt", "Quận 8, HCM")</param>
    /// <param name="keyword">Từ khoá tìm kiếm (ví dụ: "khách sạn", "cafe làm việc")</param>
    [HttpGet]
    public async Task<ActionResult<SearchResponse>> SearchPlacesGet([FromQuery] string area, [FromQuery] string? keyword = null)
    {
        if (string.IsNullOrWhiteSpace(area))
        {
            return BadRequest(new { error = "Vùng tìm kiếm (area) là bắt buộc" });
        }

        var request = new SearchRequest
        {
            Area = area,
            Keyword = keyword
        };

        try
        {
            var result = await _searchService.SearchPlacesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching places");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi tìm kiếm địa điểm" });
        }
    }
}

