namespace GoogleSearching.Api.Models;

public class ChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string AssistantMessage { get; set; } = string.Empty;
    public string MemorySummary { get; set; } = string.Empty;
    public List<ChatToolCall> ToolCalls { get; set; } = new();
}

public class ChatToolCall
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "done";
    public string? Detail { get; set; }
}

