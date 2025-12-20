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
        _logger.LogInformation("Chat request. sessionId={SessionId} autoRunApi={AutoRunApi} messageLen={Len}",
            session.SessionId, request.AutoRunApi, request.Message.Trim().Length);

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
            ["max_tokens"] = 650,
        };

        if (request.AutoRunApi)
        {
            firstPayload["tools"] = tools;
            firstPayload["tool_choice"] = "auto";
        }

        AzureChatCompletion first;
        try
        {
            first = await CreateWithRetryOnceOn429Async(firstPayload, session.SessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI first completion failed. sessionId={SessionId}", session.SessionId);
            var assistantText = ex is AzureOpenAIRequestException { StatusCode: 429 } rateLimitEx
                ? BuildUserFacingRateLimitMessage(rateLimitEx)
                : BuildLocalFallbackReply(request.Message, ex);
            AppendToHistory(session, request.Message, assistantText);

            return new ChatResponse
            {
                SessionId = session.SessionId,
                AssistantMessage = assistantText,
                MemorySummary = session.MemorySummary,
                ToolCalls = new List<ChatToolCall>
                {
                    new()
                    {
                        Name = "azure_openai",
                        Status = "error",
                        Detail = ex is AzureOpenAIRequestException aoaiEx && aoaiEx.StatusCode == 429
                            ? BuildRateLimitDetail(aoaiEx)
                            : "Azure OpenAI request failed"
                    }
                }
            };
        }

        var toolCalls = new List<ChatToolCall>();
        var latestSearchSummary = (SearchToolResult?)null;

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

                _logger.LogInformation("Tool call search_places. sessionId={SessionId} area={Area} keyword={Keyword}",
                    session.SessionId, area, keyword ?? "");

                toolCalls.Add(new ChatToolCall
                {
                    Name = "search_places",
                    Status = "running",
                    Detail = $"area={area}{(string.IsNullOrWhiteSpace(keyword) ? "" : $", keyword={keyword}")}"
                });

                var result = await _searchService.SearchPlacesAsync(new SearchRequest { Area = area, Keyword = keyword });
                var summary = SummarizeSearchResult(result);
                latestSearchSummary = summary;

                toolCalls.Add(new ChatToolCall
                {
                    Name = "search_places",
                    Status = "done",
                    Detail = $"{summary.TotalCount} results"
                });

                var toolContentJson = JsonSerializer.Serialize(summary);
                _logger.LogInformation("Tool result summary size={Size} chars. sessionId={SessionId}",
                    toolContentJson.Length, session.SessionId);

                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = call.Id,
                    ["content"] = toolContentJson,
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
            ["max_tokens"] = 700,
        };

        string assistantFinal;
        try
        {
            var second = await CreateWithRetryOnceOn429Async(secondPayload, session.SessionId, cancellationToken);
            assistantFinal = second.AssistantContent?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI second completion failed. sessionId={SessionId}", session.SessionId);
            assistantFinal = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(assistantFinal))
        {
            // assistantFinal empty -> fallback
            assistantFinal = latestSearchSummary != null
                ? BuildFallbackAnswerFromSearch(latestSearchSummary)
                : "Mình đã gọi API nhưng chưa nhận được phản hồi hoàn chỉnh. Bạn thử lại giúp mình nhé.";
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

    private static string BuildLocalFallbackReply(string userMessage, Exception? ex)
    {
        var text = (userMessage ?? string.Empty).Trim();

        if (LooksLikeGreetingOrSmalltalk(text))
        {
            return "Chào bạn! Mình có thể giúp bạn tìm địa điểm theo khu vực và từ khoá.\n\nVí dụ: \"Tìm cafe làm việc ở Quận 3\" hoặc \"Tìm bệnh viện gần Quận 1\".";
        }

        return "Hiện mình đang gặp lỗi khi kết nối dịch vụ A.I. Bạn thử lại sau ít phút giúp mình nhé.";
    }

    private static string BuildRateLimitDetail(AzureOpenAIRequestException ex)
    {
        var retryAfter = ex.RetryAfterSeconds;
        return retryAfter.HasValue && retryAfter.Value > 0
            ? $"Rate limited. retryAfterSec={retryAfter.Value}"
            : "Rate limited.";
    }

    private static string BuildUserFacingRateLimitMessage(AzureOpenAIRequestException ex)
    {
        var retryAfter = ex.RetryAfterSeconds;
        return retryAfter.HasValue && retryAfter.Value > 0
            ? $"Hệ thống đang bận (rate limit). Bạn thử lại sau {retryAfter.Value} giây nhé."
            : "Hệ thống đang bận (rate limit). Bạn thử lại sau ít giây nhé.";
    }

    private static bool LooksLikeGreetingOrSmalltalk(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;

        var t = text.Trim().ToLowerInvariant();
        if (t.Length <= 32)
        {
            if (t is "hi" or "hello" or "hey" or "alo" or "chao" or "chào" or "xin chao" or "xin chào")
            {
                return true;
            }
        }

        var smalltalkMarkers = new[]
        {
            "chao", "chào", "xin chao", "xin chào",
            "toi ten", "tôi tên", "minh ten", "mình tên",
            "ban ten", "bạn tên",
            "ban khoe", "bạn khỏe", "khoe khong", "khỏe không",
            "cam on", "cảm ơn",
            "co gi vay", "có gì vậy", "gi vay", "gì vậy"
        };

        return smalltalkMarkers.Any(m => t.Contains(m, StringComparison.Ordinal));
    }

    private async Task<AzureChatCompletion> CreateWithRetryOnceOn429Async(object payload, string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            return await _openAi.CreateChatCompletionAsync(payload, cancellationToken);
        }
        catch (AzureOpenAIRequestException ex) when (ex.StatusCode == 429 &&
                                                    ex.RetryAfterSeconds.HasValue &&
                                                    ex.RetryAfterSeconds.Value > 0 &&
                                                    ex.RetryAfterSeconds.Value <= 15)
        {
            _logger.LogWarning("Azure OpenAI rate limited. sessionId={SessionId} retryAfterSec={RetryAfterSec} -> waiting then retry once",
                sessionId, ex.RetryAfterSeconds.Value);
            await Task.Delay(TimeSpan.FromSeconds(ex.RetryAfterSeconds.Value), cancellationToken);
            return await _openAi.CreateChatCompletionAsync(payload, cancellationToken);
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
            .Take(5)
            .Select(p => new SearchToolPlace
            {
                Name = p.Name,
                Rating = p.Rating,
                UserRatingsTotal = p.UserRatingsTotal,
                Address = Truncate(p.Address, 140),
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

    private static string Truncate(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLen ? value : $"{value[..maxLen]}…";
    }

    private static string BuildFallbackAnswerFromSearch(SearchToolResult summary)
    {
        if (summary.TotalCount == 0)
        {
            return $"Mình không tìm thấy địa điểm nào cho khu vực “{summary.Area}”. Bạn muốn thử khu vực khác không?";
        }

        var lines = new List<string>
        {
            $"Mình đã tìm thấy {summary.TotalCount} địa điểm tại “{summary.Area}”{(string.IsNullOrWhiteSpace(summary.Keyword) ? "" : $" cho từ khoá “{summary.Keyword}”")}.",
            "",
            "Top gợi ý:",
        };

        foreach (var p in summary.Places)
        {
            var rating = p.Rating.HasValue ? $"{p.Rating:0.0}⭐" : "chưa có rating";
            lines.Add($"- {p.Name} ({rating}, {p.UserRatingsTotal} đánh giá) — {p.Address}");
        }

        lines.Add("");
        lines.Add("Nếu bạn muốn, mình có thể lọc theo “đánh giá > 4.2”, “mở cửa”, hoặc “gần trung tâm”.");

        return string.Join('\n', lines);
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
