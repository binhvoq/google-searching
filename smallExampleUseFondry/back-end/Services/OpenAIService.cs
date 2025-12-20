using Azure;
using Azure.AI.OpenAI;
using ChatApi.Models;

namespace ChatApi.Services;

public class OpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _logger = logger;
        
        var endpoint = configuration["AzureOpenAI:Endpoint"] 
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
        var apiKey = configuration["AzureOpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured");
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";
        var apiVersion = configuration["AzureOpenAI:ApiVersion"] ?? "2024-02-15-preview";

        var credential = new AzureKeyCredential(apiKey);
        
        _client = new OpenAIClient(new Uri(endpoint), credential);
    }

    public async Task<ChatResponse> GetChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatMessages = new List<ChatRequestMessage>();

            // Thêm lịch sử chat nếu có
            if (request.History != null && request.History.Any())
            {
                foreach (var msg in request.History)
                {
                    if (msg.Role.ToLower() == "user")
                        chatMessages.Add(new ChatRequestUserMessage(msg.Content));
                    else if (msg.Role.ToLower() == "assistant")
                        chatMessages.Add(new ChatRequestAssistantMessage(msg.Content));
                    else if (msg.Role.ToLower() == "system")
                        chatMessages.Add(new ChatRequestSystemMessage(msg.Content));
                }
            }

            // Thêm message hiện tại
            chatMessages.Add(new ChatRequestUserMessage(request.Message));

            var chatCompletionsOptions = new ChatCompletionsOptions(_deploymentName, chatMessages)
            {
                Temperature = (float)(request.Temperature ?? 0.7),
                MaxTokens = request.MaxTokens ?? 500
            };

            var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            var choice = response.Value.Choices[0];
            var usage = response.Value.Usage;

            return new ChatResponse
            {
                Message = choice.Message.Content ?? string.Empty,
                TokensUsed = usage.TotalTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            return new ChatResponse
            {
                Error = ex.Message,
                Message = "Có lỗi xảy ra khi xử lý yêu cầu."
            };
        }
    }
}

