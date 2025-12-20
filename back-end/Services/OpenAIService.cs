using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;

namespace GoogleSearching.Api.Services;

public class AzureOpenAIRequestException : Exception
{
    public AzureOpenAIRequestException(string message, int statusCode, int? retryAfterSeconds, string? bodyPreview, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
        BodyPreview = bodyPreview;
    }

    public int StatusCode { get; }
    public int? RetryAfterSeconds { get; }
    public string? BodyPreview { get; }
}

public class OpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _logger = logger;
        
        var endpoint = configuration["AzureOpenAI:Endpoint"]?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured or is empty");
        
        var apiKey = configuration["AzureOpenAI:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured or is empty");
        
        _deploymentName = configuration["AzureOpenAI:DeploymentName"]?.Trim() ?? "gpt-4o-mini";

        var credential = new AzureKeyCredential(apiKey);
        _client = new OpenAIClient(new Uri(endpoint), credential);
    }

    public async Task<AzureChatCompletion> CreateChatCompletionAsync(object payload, CancellationToken cancellationToken)
    {
        try
        {
            var payloadDict = payload as Dictionary<string, object?> 
                ?? throw new ArgumentException("Payload must be a Dictionary<string, object?>", nameof(payload));

            var messages = ExtractMessages(payloadDict);
            var tools = ExtractTools(payloadDict);
            var toolChoice = ExtractToolChoice(payloadDict);
            var temperature = ExtractTemperature(payloadDict);
            var maxTokens = ExtractMaxTokens(payloadDict);

            var chatCompletionsOptions = new ChatCompletionsOptions(_deploymentName, messages)
            {
                Temperature = temperature,
                MaxTokens = maxTokens
            };

            // Add tools if present
            if (tools != null && tools.Count > 0)
            {
                _logger.LogInformation("Adding {Count} tools to OpenAI request", tools.Count);
                foreach (var tool in tools)
                {
                    chatCompletionsOptions.Tools.Add(tool);
                }

                if (toolChoice != null)
                {
                    chatCompletionsOptions.ToolChoice = toolChoice;
                    _logger.LogInformation("Tool choice set to: {ToolChoice}", toolChoice);
                }
            }

            var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            var choice = response.Value.Choices[0];
            var message = choice.Message;

            var result = new AzureChatCompletion
            {
                AssistantContent = message.Content
            };

            // Extract tool calls
            if (message.ToolCalls != null && message.ToolCalls.Count > 0)
            {
                _logger.LogInformation("OpenAI returned {Count} tool calls", message.ToolCalls.Count);
                foreach (var toolCall in message.ToolCalls)
                {
                    if (toolCall is ChatCompletionsFunctionToolCall functionToolCall)
                    {
                        _logger.LogInformation("Tool call: {Name} with ID {Id}", functionToolCall.Name, functionToolCall.Id);
                        result.ToolCalls.Add(new AzureToolCall
                        {
                            Id = functionToolCall.Id,
                            Name = functionToolCall.Name ?? string.Empty,
                            ArgumentsJson = functionToolCall.Arguments?.ToString() ?? "{}"
                        });
                    }
                }
            }
            else
            {
                var contentPreview = message.Content != null 
                    ? message.Content.Substring(0, Math.Min(200, message.Content.Length)) 
                    : "null";
                _logger.LogWarning("OpenAI response has no tool calls. Content: {Content}", contentPreview);
            }

            return result;
        }
        catch (RequestFailedException ex)
        {
            var statusCode = ex.Status;
            var retryAfterSeconds = TryGetRetryAfterSeconds(ex);
            var bodyPreview = ex.Message;

            _logger.LogWarning("Azure OpenAI error {Status}. retryAfterSec={RetryAfterSec} body={Body}",
                statusCode, retryAfterSeconds, bodyPreview);

            throw new AzureOpenAIRequestException(
                $"Azure OpenAI request failed: {statusCode}.",
                statusCode,
                retryAfterSeconds,
                bodyPreview,
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Azure OpenAI");
            throw new AzureOpenAIRequestException(
                "Azure OpenAI request failed with unexpected error.",
                500,
                null,
                ex.Message,
                ex);
        }
    }

    private List<ChatRequestMessage> ExtractMessages(Dictionary<string, object?> payload)
    {
        if (!payload.TryGetValue("messages", out var messagesObj) || messagesObj == null)
        {
            throw new ArgumentException("Payload must contain 'messages' array");
        }

        var messagesList = messagesObj as List<Dictionary<string, object?>> 
            ?? throw new ArgumentException("'messages' must be a List<Dictionary<string, object?>>");

        var result = new List<ChatRequestMessage>();

        foreach (var msg in messagesList)
        {
            if (!msg.TryGetValue("role", out var roleObj) || roleObj == null)
                continue;

            var role = roleObj.ToString()?.ToLowerInvariant() ?? "user";
            var content = msg.TryGetValue("content", out var contentObj) ? contentObj?.ToString() : null;

            ChatRequestMessage? message = role switch
            {
                "system" => new ChatRequestSystemMessage(content ?? string.Empty),
                "user" => new ChatRequestUserMessage(content ?? string.Empty),
                "assistant" => ExtractAssistantMessage(msg, content),
                "tool" => ExtractToolMessage(msg),
                _ => null
            };

            if (message != null)
            {
                result.Add(message);
            }
        }

        return result;
    }

    private ChatRequestMessage ExtractAssistantMessage(Dictionary<string, object?> msg, string? content)
    {
        // QUAN TRỌNG: Theo Azure OpenAI docs, khi có tool messages, PHẢI có assistant message với tool_calls đi trước
        // Format: role="assistant", content=null (khi có tool_calls), tool_calls=[{id, type, function: {name, arguments}}]
        var assistant = new ChatRequestAssistantMessage(content ?? string.Empty);

        if (msg.TryGetValue("tool_calls", out var toolCallsObj) && toolCallsObj is IEnumerable<object?> toolCallsEnumerable)
        {
            foreach (var tc in toolCallsEnumerable)
            {
                if (tc is not Dictionary<string, object?> tcDict)
                    continue;

                // Theo docs: tool_calls phải có id, type="function", và function: {name, arguments}
                var id = tcDict.TryGetValue("id", out var idObj) ? idObj?.ToString() : null;
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Skipping tool call with missing id");
                    continue;
                }

                if (!tcDict.TryGetValue("function", out var fnObj) || fnObj is not Dictionary<string, object?> fnDict)
                {
                    _logger.LogWarning("Skipping tool call with missing function object. id={Id}", id);
                    continue;
                }

                var name = fnDict.TryGetValue("name", out var nameObj) ? nameObj?.ToString() : null;
                var arguments = fnDict.TryGetValue("arguments", out var argsObj) ? argsObj?.ToString() : null;

                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Skipping tool call with missing function name. id={Id}", id);
                    continue;
                }

                // ChatCompletionsFunctionToolCall(id, name, arguments)
                assistant.ToolCalls.Add(new ChatCompletionsFunctionToolCall(id, name, arguments ?? "{}"));
                _logger.LogInformation("Added tool call to assistant message: id={Id} name={Name}", id, name);
            }
        }

        return assistant;
    }

    private ChatRequestMessage? ExtractToolMessage(Dictionary<string, object?> msg)
    {
        // Theo Azure OpenAI docs: tool message format là role="tool", tool_call_id=<id>, content=<kết quả>
        // QUAN TRỌNG: tool_call_id PHẢI khớp với id trong tool_calls của assistant message trước đó
        if (!msg.TryGetValue("tool_call_id", out var toolCallIdObj) || toolCallIdObj == null)
        {
            _logger.LogWarning("Tool message missing tool_call_id");
            return null;
        }

        var toolCallId = toolCallIdObj.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(toolCallId))
        {
            _logger.LogWarning("Tool message has empty tool_call_id");
            return null;
        }

        var content = msg.TryGetValue("content", out var contentObj) ? contentObj?.ToString() : string.Empty;
        
        // Azure.AI.OpenAI SDK: ChatRequestToolMessage(content, toolCallId)
        // LƯU Ý: Thứ tự là (content, toolCallId) - không được đảo!
        var toolMessage = new ChatRequestToolMessage(content ?? string.Empty, toolCallId);
        
        _logger.LogInformation("Created tool message: toolCallId={ToolCallId} contentLength={ContentLength}", 
            toolCallId, content?.Length ?? 0);
        
        return toolMessage;
    }

    private List<ChatCompletionsToolDefinition>? ExtractTools(Dictionary<string, object?> payload)
    {
        if (!payload.TryGetValue("tools", out var toolsObj) || toolsObj == null)
            return null;

        // ChatService hiện build tools dạng object[] (mỗi item là Dictionary<string, object?>)
        // nên ở đây cần hỗ trợ cả array/list/enumerable.
        IEnumerable<object?> toolItems = toolsObj switch
        {
            object?[] arr => arr,
            IEnumerable<object?> enumerable => enumerable,
            _ => Array.Empty<object?>()
        };

        var result = new List<ChatCompletionsToolDefinition>();

        foreach (var item in toolItems)
        {
            if (item is not Dictionary<string, object?> tool)
                continue;

            if (!tool.TryGetValue("type", out var typeObj) || typeObj?.ToString() != "function")
                continue;

            if (!tool.TryGetValue("function", out var functionObj) || functionObj is not Dictionary<string, object?> function)
                continue;

            var name = function.TryGetValue("name", out var nameObj) ? nameObj?.ToString() : null;
            var description = function.TryGetValue("description", out var descObj) ? descObj?.ToString() : null;
            var parameters = function.TryGetValue("parameters", out var paramsObj) ? paramsObj : null;

            if (string.IsNullOrWhiteSpace(name))
                continue;

            var functionDefinition = new ChatCompletionsFunctionToolDefinition
            {
                Name = name,
                Description = description
            };

            if (parameters != null)
            {
                try
                {
                    var parametersJson = JsonSerializer.Serialize(parameters);
                    functionDefinition.Parameters = BinaryData.FromString(parametersJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to serialize tool parameters for {ToolName}", name);
                }
            }

            // ChatCompletionsFunctionToolDefinition kế thừa từ ChatCompletionsToolDefinition
            result.Add(functionDefinition);
        }

        return result.Count > 0 ? result : null;
    }

    private ChatCompletionsToolChoice? ExtractToolChoice(Dictionary<string, object?> payload)
    {
        if (!payload.TryGetValue("tool_choice", out var toolChoiceObj) || toolChoiceObj == null)
            return null;

        var toolChoiceStr = toolChoiceObj.ToString()?.ToLowerInvariant();
        return toolChoiceStr switch
        {
            "auto" => ChatCompletionsToolChoice.Auto,
            "none" => ChatCompletionsToolChoice.None,
            // "required" không có trong SDK version này, dùng "auto" thay thế
            "required" => ChatCompletionsToolChoice.Auto,
            _ => null
        };
    }

    private float ExtractTemperature(Dictionary<string, object?> payload)
    {
        if (payload.TryGetValue("temperature", out var tempObj) && tempObj != null)
        {
            if (tempObj is double d)
                return (float)d;
            if (tempObj is int i)
                return (float)i;
            if (float.TryParse(tempObj.ToString(), out var f))
                return f;
        }
        return 0.7f;
    }

    private int? ExtractMaxTokens(Dictionary<string, object?> payload)
    {
        if (payload.TryGetValue("max_tokens", out var maxTokensObj) && maxTokensObj != null)
        {
            if (maxTokensObj is int i)
                return i;
            if (int.TryParse(maxTokensObj.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static int? TryGetRetryAfterSeconds(RequestFailedException ex)
    {
        // Azure SDK may include retry-after in headers, but RequestFailedException doesn't expose it directly
        // We'll return null and let the caller handle it
        return null;
    }
}

public class AzureChatCompletion
{
    public string? AssistantContent { get; set; }
    public List<AzureToolCall> ToolCalls { get; set; } = new();
}

public class AzureToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";
}

