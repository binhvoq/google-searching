using System.Text.Json;
using GoogleSearching.Api.Models;

namespace GoogleSearching.Api.Services;

public class ChatService : IChatService
{
    private readonly AzureOpenAIChatClient _openAi;
    private readonly ISearchService _searchService;
    private readonly ChatSessionStore _sessions;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        AzureOpenAIChatClient openAi,
        ISearchService searchService,
        ChatSessionStore sessions,
        ILogger<ChatService> logger)
    {
        _openAi = openAi;
        _searchService = searchService;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        var session = _sessions.GetOrCreate(request.SessionId);

        var systemPrompt = ChatPrompts.BuildSystemPrompt(request.AutoRunApi);
        var memory = session.MemorySummary;

        var history = session.History
            .OrderByDescending(m => m.CreatedAt)
            .Take(12)
            .Reverse()
            .Select(m => new Dictionary<string, object?>
            {
                ["role"] = m.Role,
                ["content"] = m.Content
            })
            .ToList();

        var messages = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["role"] = "system",
                ["content"] = $"{systemPrompt}\n\n{memory}"
            },
        };
        messages.AddRange(history);
        messages.Add(new Dictionary<string, object?>
        {
            ["role"] = "user",
            ["content"] = request.Message.Trim()
        });

        var tools = BuildTools();

        var firstPayload = new Dictionary<string, object?>
        {
            ["messages"] = messages,
            ["temperature"] = 0.2,
            ["max_tokens"] = 800,
        };

        if (request.AutoRunApi)
        {
            firstPayload["tools"] = tools;
            firstPayload["tool_choice"] = "auto";
        }

        var first = await _openAi.CreateChatCompletionAsync(firstPayload, cancellationToken);

        var toolCalls = new List<ChatToolCall>();

        // No tool calls -> return assistant message as-is
        if (first.ToolCalls.Count == 0 || !request.AutoRunApi)
        {
            var assistantText = first.AssistantContent?.Trim();
            if (string.IsNullOrWhiteSpace(assistantText))
            {
                assistantText = "Mình chưa có phản hồi. Bạn có thể thử diễn đạt lại yêu cầu cụ thể hơn không?";
            }

            AppendToHistory(session, request.Message, assistantText);

            return new ChatResponse
            {
                SessionId = session.SessionId,
                AssistantMessage = assistantText,
                MemorySummary = session.MemorySummary,
                ToolCalls = toolCalls
            };
        }

        // Add assistant tool call message
        messages.Add(new Dictionary<string, object?>
        {
            ["role"] = "assistant",
            ["content"] = null,
            ["tool_calls"] = first.ToolCalls.Select(tc => new Dictionary<string, object?>
            {
                ["id"] = tc.Id,
                ["type"] = "function",
                ["function"] = new Dictionary<string, object?>
                {
                    ["name"] = tc.Name,
                    ["arguments"] = tc.ArgumentsJson,
                }
            }).ToList()
        });

        foreach (var call in first.ToolCalls)
        {
            if (!string.Equals(call.Name, "search_places", StringComparison.OrdinalIgnoreCase))
            {
                toolCalls.Add(new ChatToolCall { Name = call.Name, Status = "error", Detail = "Unsupported tool" });
                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = call.Id,
                    ["content"] = JsonSerializer.Serialize(new { error = "Unsupported tool" }),
                });
                continue;
            }

            try
            {
                var (area, keyword) = ParseSearchArgs(call.ArgumentsJson);
                session.LastArea = area;
                session.LastKeyword = keyword;

                toolCalls.Add(new ChatToolCall
                {
                    Name = "search_places",
                    Status = "running",
                    Detail = $"area={area}{(string.IsNullOrWhiteSpace(keyword) ? "" : $", keyword={keyword}")}"
                });

                var result = await _searchService.SearchPlacesAsync(new SearchRequest { Area = area, Keyword = keyword });
                var summary = SummarizeSearchResult(result);

                toolCalls.Add(new ChatToolCall
                {
                    Name = "search_places",
                    Status = "done",
                    Detail = $"{summary.TotalCount} results"
                });

                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = call.Id,
                    ["content"] = JsonSerializer.Serialize(summary),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool execution failed: {ToolName}", call.Name);
                toolCalls.Add(new ChatToolCall { Name = call.Name, Status = "error", Detail = ex.Message });
                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = call.Id,
                    ["content"] = JsonSerializer.Serialize(new { error = "Tool execution failed", detail = ex.Message }),
                });
            }
        }

        var secondPayload = new Dictionary<string, object?>
        {
            ["messages"] = messages,
            ["temperature"] = 0.2,
            ["max_tokens"] = 900,
        };

        var second = await _openAi.CreateChatCompletionAsync(secondPayload, cancellationToken);
        var assistantFinal = second.AssistantContent?.Trim();
        if (string.IsNullOrWhiteSpace(assistantFinal))
        {
            assistantFinal = "Mình đã gọi API nhưng chưa nhận được phản hồi hoàn chỉnh. Bạn thử lại giúp mình nhé.";
        }

        AppendToHistory(session, request.Message, assistantFinal);

        return new ChatResponse
        {
            SessionId = session.SessionId,
            AssistantMessage = assistantFinal,
            MemorySummary = session.MemorySummary,
            ToolCalls = toolCalls
        };
    }

    private static void AppendToHistory(ChatSessionState session, string userMessage, string assistantMessage)
    {
        session.History.Add(new StoredChatMessage { Role = "user", Content = userMessage.Trim() });
        session.History.Add(new StoredChatMessage { Role = "assistant", Content = assistantMessage.Trim() });

        // Keep history bounded
        while (session.History.Count > 40)
        {
            session.History.RemoveAt(0);
        }
    }

    private static object BuildTools()
    {
        return new object[]
        {
            new Dictionary<string, object?>
            {
                ["type"] = "function",
                ["function"] = new Dictionary<string, object?>
                {
                    ["name"] = "search_places",
                    ["description"] = "Tìm địa điểm theo khu vực (area) và từ khoá (keyword).",
                    ["parameters"] = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object?>
                        {
                            ["area"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["description"] = "Khu vực tìm kiếm, ví dụ: “Quận 1”, “Đà Lạt”, “Thủ Đức”."
                            },
                            ["keyword"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["description"] = "Từ khoá tuỳ chọn, ví dụ: “bệnh viện”, “cafe làm việc”, “khách sạn 4 sao”."
                            },
                        },
                        ["required"] = new[] { "area" }
                    }
                }
            }
        };
    }

    private static (string area, string? keyword) ParseSearchArgs(string argumentsJson)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
        var root = doc.RootElement;

        var area = root.TryGetProperty("area", out var areaEl) ? areaEl.GetString() : null;
        var keyword = root.TryGetProperty("keyword", out var keywordEl) ? keywordEl.GetString() : null;

        if (string.IsNullOrWhiteSpace(area))
        {
            throw new InvalidOperationException("Tool search_places requires 'area'.");
        }

        return (area.Trim(), string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim());
    }

    private static SearchToolResult SummarizeSearchResult(SearchResponse result)
    {
        var places = result.Places
            .Take(10)
            .Select(p => new SearchToolPlace
            {
                Name = p.Name,
                Rating = p.Rating,
                UserRatingsTotal = p.UserRatingsTotal,
                Address = p.Address,
                GoogleMapsUrl = string.IsNullOrWhiteSpace(p.PlaceId) ? null : $"https://www.google.com/maps/place/?q=place_id:{p.PlaceId}"
            })
            .ToList();

        return new SearchToolResult
        {
            Area = result.Area,
            Keyword = result.Keyword,
            TotalCount = result.TotalCount,
            Places = places
        };
    }
}

public class SearchToolResult
{
    public string Area { get; set; } = string.Empty;
    public string? Keyword { get; set; }
    public int TotalCount { get; set; }
    public List<SearchToolPlace> Places { get; set; } = new();
}

public class SearchToolPlace
{
    public string Name { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public int UserRatingsTotal { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? GoogleMapsUrl { get; set; }
}

