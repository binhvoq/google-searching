namespace GoogleSearching.Api.Models;

public class ChatRequest
{
    public string? SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool AutoRunApi { get; set; } = true;
}

