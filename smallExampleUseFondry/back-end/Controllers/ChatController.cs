using Microsoft.AspNetCore.Mvc;
using ChatApi.Models;
using ChatApi.Services;

namespace ChatApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OpenAIService _openAIService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(OpenAIService openAIService, ILogger<ChatController> logger)
    {
        _openAIService = openAIService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ChatResponse { Error = "Message không được để trống" });
        }

        _logger.LogInformation("Received chat request: {Message}", request.Message);

        var response = await _openAIService.GetChatCompletionAsync(request, cancellationToken);

        if (!string.IsNullOrEmpty(response.Error))
        {
            return StatusCode(500, response);
        }

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

