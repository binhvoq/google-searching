using GoogleSearching.Api.Models;
using GoogleSearching.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoogleSearching.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chat;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chat, ILogger<ChatController> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    /// <summary>
    /// Chat với A.I (Azure OpenAI). Có thể tự động gọi API tìm kiếm địa điểm qua tool `search_places`.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Send([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message là bắt buộc" });
        }

        try
        {
            var response = await _chat.SendAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat error");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi chat với A.I" });
        }
    }
}

