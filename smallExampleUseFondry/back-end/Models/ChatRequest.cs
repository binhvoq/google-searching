namespace ChatApi.Models;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage>? History { get; set; }
    public double? Temperature { get; set; } = 0.7;
    public int? MaxTokens { get; set; } = 500;
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

