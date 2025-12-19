using System.Collections.Concurrent;

namespace GoogleSearching.Api.Services;

public class ChatSessionStore
{
    private readonly ConcurrentDictionary<string, ChatSessionState> _sessions = new();

    public ChatSessionState GetOrCreate(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return CreateNew();
        }

        return _sessions.GetOrAdd(sessionId, _ => new ChatSessionState(sessionId));
    }

    public ChatSessionState CreateNew()
    {
        var id = $"s_{Guid.NewGuid():N}";
        var state = new ChatSessionState(id);
        _sessions[id] = state;
        return state;
    }
}

public class ChatSessionState
{
    public ChatSessionState(string sessionId)
    {
        SessionId = sessionId;
    }

    public string SessionId { get; }

    public string? LastArea { get; set; }
    public string? LastKeyword { get; set; }

    public List<StoredChatMessage> History { get; } = new();

    public string MemorySummary =>
        string.Join('\n', new[]
        {
            "Trích nhớ (session):",
            $"- Khu vực gần đây: {(string.IsNullOrWhiteSpace(LastArea) ? "chưa có" : LastArea)}",
            $"- Từ khoá gần đây: {(string.IsNullOrWhiteSpace(LastKeyword) ? "chưa có" : LastKeyword)}",
        });
}

public class StoredChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

