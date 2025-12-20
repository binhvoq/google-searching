namespace ChatApi.Models;

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string? Error { get; set; }
}

